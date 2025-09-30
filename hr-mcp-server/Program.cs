using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using HRMCPServer;
using HRMCPServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// Add session support for MCP
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add CORS support for remote MCP client connections
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure the HR MCP Server settings
builder.Services.Configure<HRMCPServerConfiguration>(
    builder.Configuration.GetSection(HRMCPServerConfiguration.SectionName));

// Load candidates data and register as singleton
// When using blob storage, start with empty list and let BlobCandidateService load from blob
var usesBlobStorage = !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Azure:StorageConnectionString"));
if (usesBlobStorage)
{
    Console.WriteLine("🔵 BLOB STORAGE MODE: Starting with empty candidate list - BlobCandidateService will load from Azure Blob Storage");
}
else
{
    Console.WriteLine("📁 FILE MODE: Loading candidates from JSON file");
}
var candidatesData = usesBlobStorage ? new List<Candidate>() : await LoadCandidatesAsync(builder.Configuration);
builder.Services.AddSingleton(candidatesData);

// Register the candidate service (using Azure Blob Storage)
builder.Services.AddScoped<ICandidateService>(serviceProvider =>
{
    var candidates = serviceProvider.GetRequiredService<List<Candidate>>();
    var logger = serviceProvider.GetRequiredService<ILogger<BlobCandidateService>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    return new BlobCandidateService(candidates, logger, configuration);
});

// Add the MCP services: the transport to use (HTTP) and the tools to register.
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
    
var app = builder.Build();

// Enable CORS for remote access
app.UseCors();

// Enable session middleware
app.UseSession();

// Add a basic health check endpoint
app.MapGet("/", () => "HR MCP Server is running");
app.MapGet("/health", () => "OK");

// Configure the application to use the MCP server
// Support both SSE (legacy) and Streamable HTTP (for Copilot Studio)
app.MapMcp();           // Default SSE endpoint at /sse
app.MapMcp("/mcp");     // Streamable HTTP endpoint at /mcp

// Run the application
// This will start the MCP server and listen for incoming requests.
app.Run();

// Helper method to load candidates from JSON file
static async Task<List<Candidate>> LoadCandidatesAsync(IConfiguration configuration)
{
    try
    {
        var hrConfig = configuration.GetSection(HRMCPServerConfiguration.SectionName).Get<HRMCPServerConfiguration>();
        
        if (hrConfig == null || string.IsNullOrEmpty(hrConfig.CandidatesPath))
        {
            Console.WriteLine("HR configuration or CandidatesPath not found. Using empty candidate list.");
            return new List<Candidate>();
        }

        // Try multiple possible paths for the candidates file
        var possiblePaths = new[]
        {
            hrConfig.CandidatesPath,
            Path.Combine(AppContext.BaseDirectory, "Data", "candidates.json"),
            Path.Combine(Environment.CurrentDirectory, "Data", "candidates.json"),
            "Data/candidates.json",
            "./Data/candidates.json"
        };

        string? existingPath = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                existingPath = path;
                break;
            }
        }

        if (existingPath == null)
        {
            Console.WriteLine($"Candidates file not found in any of the following paths:");
            foreach (var path in possiblePaths)
            {
                Console.WriteLine($"  - {path} (Full path: {Path.GetFullPath(path)})");
            }
            Console.WriteLine("Using empty candidate list.");
            return new List<Candidate>();
        }

        var jsonContent = await File.ReadAllTextAsync(existingPath);
        var candidates = JsonSerializer.Deserialize<List<Candidate>>(jsonContent, GetJsonOptions());

        Console.WriteLine($"📁 Loaded {candidates?.Count ?? 0} candidates from JSON FILE: {existingPath}");
        return candidates ?? new List<Candidate>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading candidates from file: {ex.Message}. Using empty candidate list.");
        return new List<Candidate>();
    }
}

// Helper method for JSON serialization options
static JsonSerializerOptions GetJsonOptions()
{
    return new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}