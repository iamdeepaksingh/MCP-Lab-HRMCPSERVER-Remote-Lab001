using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HRMCPServer.Services;

/// <summary>
/// Service for managing candidate data using Azure Blob Storage
/// </summary>
public class BlobCandidateService : ICandidateService
{
    private readonly List<Candidate> _candidates;
    private readonly object _candidatesLock = new();
    private readonly ILogger<BlobCandidateService> _logger;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;
    private readonly string _blobName = "candidates.json";
    private readonly bool _useBlobStorage;

    public BlobCandidateService(
        List<Candidate> candidates,
        ILogger<BlobCandidateService> logger,
        IConfiguration configuration)
    {
        _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get Azure configuration
        var connectionString = configuration.GetValue<string>("Azure:StorageConnectionString");
        _containerName = configuration.GetValue<string>("Azure:BlobContainerName") ?? "candidates-data";
        
        _logger.LogInformation("🔵 BlobCandidateService constructor called - BLOB STORAGE MODE ACTIVE");
        _logger.LogInformation("Connection string configured: {HasConnectionString}", !string.IsNullOrEmpty(connectionString));
        _logger.LogInformation("Container name: {ContainerName}", _containerName);
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Azure Storage connection string is not configured, falling back to in-memory storage");
            _useBlobStorage = false;
        }
        else
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
                _useBlobStorage = true;
                _logger.LogInformation("BlobServiceClient created successfully, blob storage enabled");
                
                // Initialize blob storage synchronously but don't wait for data loading
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await InitializeBlobStorageAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize blob storage, continuing with in-memory data");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create blob service client, falling back to in-memory storage");
                _useBlobStorage = false;
            }
        }
    }

    /// <summary>
    /// Initializes the blob container and loads existing data
    /// </summary>
    private async Task InitializeBlobStorageAsync()
    {
        if (!_useBlobStorage || _blobServiceClient == null)
            return;

        try
        {
            // Ensure container exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            // Try to load existing data from blob
            await LoadFromBlobAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize blob storage");
        }
    }

    public Task<List<Candidate>> GetAllCandidatesAsync()
    {
        lock (_candidatesLock)
        {
            return Task.FromResult(new List<Candidate>(_candidates));
        }
    }

    public Task<bool> AddCandidateAsync(Candidate candidate)
    {
        if (candidate == null)
            throw new ArgumentNullException(nameof(candidate));

        lock (_candidatesLock)
        {
            // Check if candidate with same email already exists
            if (_candidates.Any(c => string.Equals(c.Email, candidate.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _candidates.Add(candidate);
            _logger.LogInformation("Added new candidate: {FullName} ({Email})", candidate.FullName, candidate.Email);
        }
        
        // Save to blob if configured
        if (_useBlobStorage)
        {
            _logger.LogInformation("🔵 Triggering blob save for new candidate to AZURE BLOB STORAGE");
            _ = Task.Run(SaveToBlobAsync);
        }
        else
        {
            _logger.LogWarning("⚠️ Blob storage not enabled, candidate only saved in memory");
        }
        
        return Task.FromResult(true);
    }

    public Task<bool> UpdateCandidateAsync(string email, Action<Candidate> updateAction)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        
        if (updateAction == null)
            throw new ArgumentNullException(nameof(updateAction));

        bool updated = false;
        lock (_candidatesLock)
        {
            var candidate = _candidates.FirstOrDefault(c => 
                string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));

            if (candidate != null)
            {
                updateAction(candidate);
                _logger.LogInformation("Updated candidate with email: {Email}", email);
                updated = true;
            }
        }
        
        if (updated && _useBlobStorage)
        {
            _ = Task.Run(SaveToBlobAsync);
        }
        
        return Task.FromResult(updated);
    }

    public Task<bool> RemoveCandidateAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        bool removed = false;
        lock (_candidatesLock)
        {
            var candidate = _candidates.FirstOrDefault(c => 
                string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));

            if (candidate != null)
            {
                _candidates.Remove(candidate);
                _logger.LogInformation("Removed candidate with email: {Email}", email);
                removed = true;
            }
        }
        
        if (removed && _useBlobStorage)
        {
            _ = Task.Run(SaveToBlobAsync);
        }
        
        return Task.FromResult(removed);
    }

    public Task<List<Candidate>> SearchCandidatesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GetAllCandidatesAsync();
        }

        var searchTermLower = searchTerm.Trim().ToLowerInvariant();

        lock (_candidatesLock)
        {
            var matchingCandidates = _candidates.Where(c =>
                c.FirstName.ToLowerInvariant().Contains(searchTermLower) ||
                c.LastName.ToLowerInvariant().Contains(searchTermLower) ||
                c.Email.ToLowerInvariant().Contains(searchTermLower) ||
                c.CurrentRole.ToLowerInvariant().Contains(searchTermLower) ||
                c.Skills.Any(skill => skill.ToLowerInvariant().Contains(searchTermLower)) ||
                c.SpokenLanguages.Any(lang => lang.ToLowerInvariant().Contains(searchTermLower))
            ).ToList();

            return Task.FromResult(matchingCandidates);
        }
    }
    
    /// <summary>
    /// Loads candidates from Azure Blob Storage
    /// </summary>
    private async Task LoadFromBlobAsync()
    {
        if (!_useBlobStorage || _blobServiceClient == null)
            return;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(_blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                _logger.LogInformation("Blob {BlobName} does not exist, starting with empty candidate list", _blobName);
                return;
            }
            
            var response = await blobClient.DownloadContentAsync();
            var jsonContent = response.Value.Content.ToString();
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var candidatesFromBlob = JsonSerializer.Deserialize<List<Candidate>>(jsonContent, jsonOptions) ?? new List<Candidate>();
            
            lock (_candidatesLock)
            {
                _candidates.Clear();
                _candidates.AddRange(candidatesFromBlob);
            }
            
            _logger.LogInformation("🔵 Successfully loaded {Count} candidates from AZURE BLOB STORAGE", candidatesFromBlob.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load candidates from blob storage");
        }
    }
    
    /// <summary>
    /// Saves the current candidate list to Azure Blob Storage
    /// </summary>
    private async Task SaveToBlobAsync()
    {
        if (!_useBlobStorage || _blobServiceClient == null)
            return;

        try
        {
            List<Candidate> candidatesToSave;
            
            // Create a copy of the candidates list to avoid holding the lock too long
            lock (_candidatesLock)
            {
                candidatesToSave = new List<Candidate>(_candidates);
            }
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var jsonContent = JsonSerializer.Serialize(candidatesToSave, jsonOptions);
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(_blobName);
            
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
            await blobClient.UploadAsync(stream, overwrite: true);
            
            _logger.LogInformation("🔵 Successfully saved {Count} candidates to AZURE BLOB STORAGE", candidatesToSave.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save candidates to blob storage");
        }
    }
}