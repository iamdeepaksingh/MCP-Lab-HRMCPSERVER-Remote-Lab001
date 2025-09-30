# HR MCP Server

A Model Context Protocol (MCP) server built with .NET 8.0 that provides Human Resources management tools for candidate operations. This server features enterprise-grade Azure Blob Storage persistence and can be deployed locally for development or remotely to Azure for production use.

## 🚀 Features

- **🔍 Candidate Management**: Full CRUD operations for HR candidate data
- **📡 MCP Protocol Compliance**: Implements the Model Context Protocol specification
- **🌐 Remote Access Support**: CORS-enabled for remote MCP client connections (Copilot Studio ready)
- **☁️ Azure Blob Storage**: Enterprise-grade persistent data storage with automatic failover
- **🛡️ Enterprise Security**: User secrets, dynamic connection strings, and secure deployment
- **🔄 Smart Service Selection**: Automatic fallback between Blob Storage and file-based storage
- **📊 Enhanced Logging**: Visual indicators for storage mode (🔵 Blob / 📁 File)
- **🔀 Dual MCP Endpoints**: Support for both SSE and Streamable HTTP protocols

## 🛠️ Available Tools

The HR MCP Server provides the following tools:

| Tool | Description | Parameters |
|------|-------------|------------|
| `list_candidates` | Get all candidates | None |
| `add_candidate` | Add a new candidate | `firstName`, `lastName`, `email`, `currentRole`, `skills`, `spokenLanguages` |
| `update_candidate` | Update existing candidate | `email`, `firstName`, `lastName`, `currentRole`, `skills`, `spokenLanguages` |
| `remove_candidate` | Remove a candidate by email | `email` |
| `search_candidates` | Search candidates by criteria | `searchTerm` (searches name, email, skills, role) |

## 🏗️ Architecture

### Storage Architecture

| Environment | Storage Mode | Authentication | Persistence |
|-------------|--------------|----------------|-------------|
| **Local Development** | 🔵 Blob Storage / 📁 File | User Secrets | ✅ Permanent |
| **Azure Production** | 🔵 Blob Storage | Dynamic Keys | ✅ Permanent |
| **Fallback Mode** | 📁 File Storage | None | ⚠️ Temporary |

### Service Selection Logic

The server automatically selects the appropriate storage service:

```csharp
// Smart service selection based on connection string availability
var usesBlobStorage = !string.IsNullOrEmpty(connectionString);
if (usesBlobStorage) {
    Console.WriteLine("🔵 BLOB STORAGE MODE: Starting with empty candidate list");
} else {
    Console.WriteLine("📁 FILE MODE: Loading candidates from JSON file");
}
```

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) (for Azure deployment)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for Azure Storage management)
- [Node.js](https://nodejs.org/) (for MCP Inspector testing)

## 🏃‍♂️ Quick Start

### Local Development

1. **Clone and Navigate to the Project**
   ```powershell
   cd hr-mcp-server
   ```

2. **Configure User Secrets (for Azure Blob Storage)**
   ```powershell
   # Initialize user secrets (if not already done)
   dotnet user-secrets init
   
   # Set your Azure Storage connection string for local development
   dotnet user-secrets set "Azure:StorageConnectionString" "YourAzureStorageConnectionString"
   ```

3. **Restore Dependencies**
   ```powershell
   dotnet restore
   ```

4. **Build the Project**
   ```powershell
   dotnet build
   ```

5. **Run the Server**
5. **Run the Server**
   ```powershell
   dotnet run
   ```

   The server will start and display output indicating storage mode:
   
   **🔵 Blob Storage Mode** (with Azure connection string):
   ```
   🔵 BLOB STORAGE MODE: Starting with empty candidate list - BlobCandidateService will load from Azure Blob Storage
   🔵 BlobCandidateService constructor called - BLOB STORAGE MODE ACTIVE
   🔵 Loaded 2 candidates from AZURE BLOB STORAGE: candidates.json
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://[::]:47002
   ```
   
   **📁 File Mode** (fallback without connection string):
   ```
   📁 FILE MODE: Loading candidates from JSON file
   📁 Loaded 5 candidates from JSON FILE: .\Data\candidates.json
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://[::]:47002
   ```

   📝 **Note**: The server automatically binds to port `47002` for local development.

### Testing Locally with MCP Inspector

1. **Start the MCP Server** (if not already running)
   ```powershell
   dotnet run
   ```

2. **Open MCP Inspector**
   ```powershell
   # For SSE endpoint (traditional)
   npx @modelcontextprotocol/inspector http://localhost:47002/sse
   
   # For Streamable HTTP endpoint (modern/Copilot Studio)
   npx @modelcontextprotocol/inspector http://localhost:47002/mcp
   ```

   This will:
   - Start the MCP Inspector web interface
   - Automatically open your browser
   - Connect to your local MCP server
   - Display all available HR tools

3. **Test the Tools**
   - Use the MCP Inspector web interface to test each tool
   - Try `list_candidates` to see existing candidates
   - Add, update, search, and remove candidates
   - **🔵 Blob Mode**: Data persists in Azure Blob Storage (`candidates.json`)
   - **📁 File Mode**: Data updates in `./Data/candidates.json`

## ☁️ Azure Deployment

### Prerequisites for Azure Deployment

1. **Azure Subscription**: Ensure you have an active Azure subscription
2. **Azure CLI Authentication**: 
   ```powershell
   az login
   ```
3. **Azure Developer CLI**: Install and authenticate azd

### Deployment Steps

1. **Initialize Azure Environment** (first time only)
   ```powershell
   azd auth login
   azd init
   ```

2. **Deploy to Azure**
   ```powershell
   azd up
   ```

   This command will:
   - Create Azure resources (App Service, App Service Plan, **Storage Account**, **Blob Container**)
   - Configure Azure Storage connection strings automatically
   - Build and deploy your .NET application
   - Configure the runtime environment with **🔵 Blob Storage mode**
   - Provide the deployment URL

3. **Get Deployment Information**
   ```powershell
   azd show
   ```

   Example output:
   ```
   hr-mcp-server
     Services:
       web  https://app-h2lyfuaa73a76.azurewebsites.net/
     Environments:
       HRMCPSERVERDKS01 [Current]
   ```

### Azure Infrastructure

The Bicep template automatically creates:

| Resource | Purpose | Configuration |
|----------|---------|---------------|
| **Storage Account** | Persistent data storage | Standard_LRS, Hot tier, Private access |
| **Blob Container** | Candidate data storage | `candidates-data` container |
| **App Service Plan** | Hosting environment | B1 Basic tier (Windows) |
| **App Service** | MCP Server hosting | .NET 8.0, CORS enabled, HTTPS only |

### Testing Remote Azure Deployment

1. **Test with MCP Inspector**
   ```powershell
   # For Copilot Studio compatibility (recommended)
   npx @modelcontextprotocol/inspector https://your-app-url.azurewebsites.net/mcp
   
   # For traditional SSE endpoint
   npx @modelcontextprotocol/inspector https://your-app-url.azurewebsites.net/sse
   ```

   Replace `your-app-url` with your actual Azure App Service URL.

2. **Verify Remote Connection**
   - The MCP Inspector should successfully connect to your Azure-deployed server
   - Look for **🔵 BLOB STORAGE MODE** in the application logs
   - All HR tools should be available and functional
   - Test CRUD operations to ensure **persistent data storage** in Azure Blob Storage
   - **Data survives app restarts** - this is the key advantage over file-based storage

3. **Check Azure Storage**
   ```powershell
   # List blobs in the candidates container
   az storage blob list --container-name candidates-data --account-name your-storage-account --auth-mode key
   ```

## 🗂️ Project Structure

```
hr-mcp-server/
├── Data/
│   └── candidates.json          # Sample candidate data (📁 File mode)
├── Configuration/
│   └── HRMCPServerConfiguration.cs  # Server configuration
├── Services/
│   ├── BlobCandidateService.cs  # 🔵 Azure Blob Storage service
│   ├── CandidateService.cs      # 📁 File-based service (legacy)
│   └── ICandidateService.cs     # Service interface
├── Tools/
│   ├── HRTools.cs              # MCP tool implementations  
│   └── Models.cs               # Data models and structures
├── Properties/
│   └── launchSettings.json     # Development launch settings
├── infra/
│   ├── resources.bicep         # 🏗️ Azure infrastructure as code
│   ├── main.bicep             # Main Bicep template
│   ├── main.parameters.json   # Bicep parameters
│   └── abbreviations.json     # Azure resource naming
├── Program.cs                 # 🚀 Main application entry point
├── appsettings.json          # Application configuration
├── appsettings.Development.json # 🛡️ Local config (secrets in User Secrets)
├── azure.yaml                # Azure deployment configuration
└── hr-mcp-server.csproj     # .NET project file with Azure dependencies
```

## ⚙️ Configuration

### Application Settings

#### appsettings.json (Production)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "HRMCPServer": {
    "CandidatesPath": "./Data/candidates.json"
  },
  "Azure": {
    "StorageConnectionString": "",  // Set via Azure App Service environment
    "BlobContainerName": "candidates-data"
  }
}
```

#### appsettings.Development.json (Local)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "HRMCPServer": {
    "CandidatesPath": ".\\Data\\candidates.json"
  },
  "Azure": {
    "StorageConnectionString": "",  // 🛡️ Stored in User Secrets for security
    "BlobContainerName": "candidates-data"
  }
}
```

### Security Configuration

#### User Secrets (Local Development)
```powershell
# Store connection string securely for local development
dotnet user-secrets set "Azure:StorageConnectionString" "DefaultEndpointsProtocol=https;..."
```

#### Environment Variables (Azure Production)

Azure deployment automatically configures these via Bicep:

- `Azure__StorageConnectionString`: 🔐 Dynamically generated connection string
- `Azure__BlobContainerName`: "candidates-data"
- `ASPNETCORE_ENVIRONMENT`: "Production"  
- `ASPNETCORE_URLS`: HTTP binding configuration
- `HRMCPServer__CandidatesPath`: Fallback file path

## 🔧 Development Notes

### Storage Mode Detection

The application automatically detects which storage mode to use:

```csharp
var usesBlobStorage = !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Azure:StorageConnectionString"));
```

### Local Development Port

The application automatically binds to port `47002` for local development.

### CORS Configuration

The server includes comprehensive CORS support for remote access including Copilot Studio:

```csharp
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
```

### Dual MCP Endpoints

The server provides two MCP endpoints for maximum compatibility:

- `/sse` - Server-Sent Events (traditional MCP clients)
- `/mcp` - Streamable HTTP (modern clients like Copilot Studio)

### Data Persistence Strategy

| Mode | Storage Location | Persistence Level | Use Case |
|------|-----------------|-------------------|----------|
| 🔵 **Blob Storage** | Azure Storage Account | ✅ Permanent | Production, shared environments |
| 📁 **File Storage** | Local file system | ⚠️ Temporary | Development, fallback |

## 🛡️ Security Features

### Connection String Security

- **Local Development**: Stored in User Secrets (encrypted)
- **Azure Production**: Generated dynamically via Bicep templates  
- **GitHub Repository**: No sensitive data committed (.gitignore protection)

### Access Control

- **Blob Storage**: Private access only (no public blob access)
- **App Service**: HTTPS enforced, TLS 1.2 minimum
- **CORS**: Configured for legitimate MCP clients

### .gitignore Protection

The repository includes comprehensive security rules:

```gitignore
# SECURITY: Sensitive configuration files
**/appsettings.Development.json
**/appsettings.Local.json
**/*connectionstring*
**/*accountkey*
app-logs.zip
.env*
*.secrets.json
```

## 🐛 Troubleshooting

### Common Issues

1. **Port Conflicts**
   - The server uses port `47002` by default
   - If the port is busy, kill existing processes: `taskkill /f /im dotnet.exe`

2. **Storage Mode Issues**
   - **No connection string**: Server falls back to 📁 File mode
   - **Invalid connection string**: Check User Secrets or Azure configuration
   - **Blob access denied**: Verify Azure Storage account permissions

3. **Build Errors**
   - Ensure .NET 8.0 SDK is installed
   - Run `dotnet restore` to restore NuGet packages
   - Check for missing Azure Storage Blobs package

4. **Azure Deployment Issues**
   - Verify Azure CLI authentication: `az account show`
   - Check azd environment: `azd env list`
   - Review deployment logs: `azd show`
   - Ensure sufficient Azure subscription quotas

5. **MCP Inspector Connection Issues**
   - **Local**: Ensure the server is running and accessible
   - **Remote**: Use `/mcp` endpoint for Copilot Studio
   - **Authentication**: Use `$env:DANGEROUSLY_OMIT_AUTH="true"` for testing
   - Check firewall settings and CORS configuration

### Error Messages & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| **"Couldn't find a project to run"** | Wrong directory | Navigate to `hr-mcp-server/` directory |
| **"406 Not Acceptable"** | Normal HTTP request to MCP endpoint | Expected behavior for non-MCP requests |
| **"🔵 Azure Storage connection string is not configured"** | Missing connection string | Set User Secrets or check Azure config |
| **"Session not found"** | MCP session issues | Server restart usually resolves |
| **"Proxy authentication required"** | Corporate proxy | Use environment variables or VPN |

### Storage Mode Debugging

Check the console output to determine storage mode:

**🔵 Blob Storage Mode (Preferred)**:
```
🔵 BLOB STORAGE MODE: Starting with empty candidate list - BlobCandidateService will load from Azure Blob Storage
🔵 BlobServiceClient created successfully, blob storage enabled
🔵 Loaded X candidates from AZURE BLOB STORAGE: candidates.json
```

**📁 File Mode (Fallback)**:
```
📁 FILE MODE: Loading candidates from JSON file
📁 Loaded X candidates from JSON FILE: .\Data\candidates.json
```

### Azure Storage Verification

```powershell
# Check if storage account exists
az storage account show --name your-storage-account --resource-group your-resource-group

# List containers
az storage container list --account-name your-storage-account --auth-mode key

# Check blob contents
az storage blob list --container-name candidates-data --account-name your-storage-account --auth-mode key
```

## 📝 Sample Data

### File Mode Sample (`./Data/candidates.json`)
```json
[
  {
    "id": "1",
    "firstName": "John",
    "lastName": "Doe", 
    "email": "john.doe@email.com",
    "currentRole": "Software Engineer",
    "skills": "C#, Azure, React",
    "spokenLanguages": "English, Spanish"
  },
  {
    "id": "2",
    "firstName": "Jane", 
    "lastName": "Smith",
    "email": "jane.smith@email.com",
    "currentRole": "Product Manager",
    "skills": "Product Strategy, Agile, SQL",
    "spokenLanguages": "English, French"
  }
]
```

### Blob Storage Data

When running in 🔵 Blob Storage mode, the same JSON structure is stored in Azure Blob Storage at:
- **Container**: `candidates-data`
- **Blob**: `candidates.json`
- **Storage Account**: Automatically created during Azure deployment

## 🎯 Copilot Studio Integration

### Connection Configuration

**For Copilot Studio Agent Configuration:**

1. **MCP Server URL**: `https://your-app-url.azurewebsites.net/mcp`
2. **Authentication**: None required (CORS pre-configured)
3. **Protocol**: Streamable HTTP (modern MCP standard)

### Available Actions

Copilot Studio agents can use all HR tools:

| Action in Copilot Studio | MCP Tool | Description |
|--------------------------|----------|-------------|
| "List all candidates" | `list_candidates` | Retrieve candidate database |
| "Add new candidate" | `add_candidate` | Create candidate record |
| "Update candidate info" | `update_candidate` | Modify existing candidate |
| "Search for candidates" | `search_candidates` | Find candidates by criteria |
| "Remove candidate" | `remove_candidate` | Delete candidate record |

### Demo Scenarios

1. **Candidate Intake**: "Add a new candidate: John Smith, john@email.com, Software Engineer"
2. **Talent Search**: "Find all candidates with Python and Azure skills"
3. **Database Management**: "List all current candidates in our system"
4. **Profile Updates**: "Update Jane Doe's role to Senior Developer"

## 🚀 Deployment Summary

### Azure Resources Created

- **🏗️ Resource Group**: `rg-HRMCPSERVERDKS01`
- **💾 Storage Account**: `sth2lyfuaa73a76` (with blob container)
- **🌐 App Service**: `app-h2lyfuaa73a76` (B1 Basic tier)
- **📋 App Service Plan**: Windows-based hosting

### Production URL

**MCP Endpoint**: `https://app-h2lyfuaa73a76.azurewebsites.net/mcp`

### Data Persistence

✅ **Enterprise-grade persistence**: Data survives app restarts, scaling events, and deployments thanks to Azure Blob Storage integration.

## 🔗 References

- [Model Context Protocol (MCP)](https://modelcontextprotocol.io/)
- [MCP Inspector Tool](https://github.com/modelcontextprotocol/inspector)
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [.NET 8.0 Documentation](https://docs.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Microsoft Copilot Studio](https://docs.microsoft.com/microsoft-copilot-studio/)

## 📄 License

This project is part of the AI MCP Lab training series and is intended for educational and demonstration purposes.

---

**🎉 Happy MCP Development with Azure! 🚀**

Your HR MCP Server is now enterprise-ready with:
- ✅ **Persistent Azure Blob Storage**
- ✅ **Secure credential management**  
- ✅ **Copilot Studio compatibility**
- ✅ **Production-grade deployment**
- ✅ **Comprehensive documentation**

For questions or issues, please refer to the troubleshooting section or check the Azure deployment logs using `azd show`.