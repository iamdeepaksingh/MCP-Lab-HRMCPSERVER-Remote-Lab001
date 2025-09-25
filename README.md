# HR MCP Server

A Model Context Protocol (MCP) server built with .NET 8.0 that provides Human Resources management tools for candidate operations. This server can be deployed locally for development or remotely to Azure for production use.

## 🚀 Features

- **Candidate Management**: Full CRUD operations for HR candidate data
- **MCP Protocol Compliance**: Implements the Model Context Protocol specification
- **Remote Access Support**: CORS-enabled for remote MCP client connections
- **Azure Ready**: Configured for seamless Azure App Service deployment
- **JSON Data Storage**: File-based candidate data with JSON serialization

## 🛠️ Available Tools

The HR MCP Server provides the following tools:

| Tool | Description | Parameters |
|------|-------------|------------|
| `ListCandidates` | Get all candidates | None |
| `AddCandidate` | Add a new candidate | `name`, `email`, `position`, `experience_years` |
| `UpdateCandidate` | Update existing candidate | `candidate_id`, `name`, `email`, `position`, `experience_years` |
| `RemoveCandidate` | Remove a candidate | `candidate_id` |
| `SearchCandidates` | Search candidates by criteria | `query` (searches name, email, position) |

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) (for Azure deployment)
- [Node.js](https://nodejs.org/) (for MCP Inspector testing)

## 🏃‍♂️ Quick Start

### Local Development

1. **Clone and Navigate to the Project**
   ```powershell
   cd "C:\Users\{YourUsername}\Documents\Projects\Copilot Studio BElux\Lab\Lab1\hr-mcp-server"
   ```

2. **Restore Dependencies**
   ```powershell
   dotnet restore
   ```

3. **Build the Project**
   ```powershell
   dotnet build
   ```

4. **Run the Server**
   ```powershell
   dotnet run
   ```

   The server will start and display output similar to:
   ```
   Loaded 5 candidates from file: .\Data\candidates.json
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://[::]:47002
   info: Microsoft.Hosting.Lifetime[0]
         Application started. Press Ctrl+C to shut down.
   ```

   📝 **Note**: The server automatically binds to port `47002` for local development.

### Testing Locally with MCP Inspector

1. **Start the MCP Server** (if not already running)
   ```powershell
   dotnet run
   ```

2. **Open MCP Inspector**
   ```powershell
   npx @modelcontextprotocol/inspector http://localhost:47002/sse
   ```

   This will:
   - Start the MCP Inspector web interface
   - Automatically open your browser
   - Connect to your local MCP server
   - Display all available HR tools

3. **Test the Tools**
   - Use the MCP Inspector web interface to test each tool
   - Try `ListCandidates` to see existing candidates
   - Add, update, search, and remove candidates
   - Verify the JSON data file updates in `./Data/candidates.json`

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
   - Create Azure resources (App Service, App Service Plan)
   - Build and deploy your .NET application
   - Configure the runtime environment
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

### Testing Remote Azure Deployment

1. **Test with MCP Inspector**
   ```powershell
   npx @modelcontextprotocol/inspector https://your-app-url.azurewebsites.net/sse
   ```

   Replace `your-app-url` with your actual Azure App Service URL.

2. **Verify Remote Connection**
   - The MCP Inspector should successfully connect to your Azure-deployed server
   - All HR tools should be available and functional
   - Test CRUD operations to ensure data persistence works in the cloud

## 🗂️ Project Structure

```
hr-mcp-server/
├── Data/
│   └── candidates.json          # Sample candidate data
├── Configuration/
│   └── HRMCPServerConfiguration.cs  # Server configuration
├── Services/
│   ├── CandidateService.cs      # Business logic for candidates
│   └── ICandidateService.cs     # Service interface
├── Tools/
│   └── HRTools.cs              # MCP tool implementations
├── Properties/
│   └── launchSettings.json     # Development launch settings
├── infra/
│   ├── resources.bicep         # Azure infrastructure as code
│   └── abbreviations.json      # Azure resource naming
├── Program.cs                  # Main application entry point
├── appsettings.json           # Application configuration
├── azure.yaml                 # Azure deployment configuration
└── hr-mcp-server.csproj      # .NET project file
```

## ⚙️ Configuration

### Application Settings

The server can be configured via `appsettings.json`:

```json
{
  "HRMCPServer": {
    "CandidatesPath": "./Data/candidates.json"
  }
}
```

### Environment Variables (Azure)

For Azure deployment, the following environment variables are automatically configured:

- `ASPNETCORE_ENVIRONMENT`: Set to "Production"
- `ASPNETCORE_URLS`: HTTP binding configuration
- `HRMCPServer__CandidatesPath`: Path to candidates data file
- `WEBSITE_DOTNET_DEFAULT_VERSION`: .NET 8.0 runtime version

## 🔧 Development Notes

### Local Development Port

The application automatically binds to port `47002` for local development, regardless of the `--urls` parameter. This is controlled by the MCP server configuration.

### CORS Configuration

The server includes CORS support for remote access:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Data Persistence

- **Local**: Data is stored in `./Data/candidates.json`
- **Azure**: Data persists during the app's runtime but may reset on app restarts (consider using Azure Storage or database for production)

## 🐛 Troubleshooting

### Common Issues

1. **Port Conflicts**
   - The server uses port `47002` by default
   - If the port is busy, kill existing processes: `Stop-Process -Name "dotnet" -Force`

2. **Build Errors**
   - Ensure .NET 8.0 SDK is installed
   - Run `dotnet restore` to restore NuGet packages

3. **Azure Deployment Issues**
   - Verify Azure CLI authentication: `az account show`
   - Check azd environment: `azd env list`
   - Review deployment logs: `azd show`

4. **MCP Inspector Connection Issues**
   - Ensure the server is running and accessible
   - Check firewall settings for local connections
   - Verify CORS configuration for remote connections

### Error Messages

- **"Couldn't find a project to run"**: Ensure you're in the project directory
- **"406 Not Acceptable"**: Normal response for non-MCP HTTP requests to MCP endpoints
- **SSE Connection Errors**: Check the `/sse` endpoint path and CORS settings

## 📝 Sample Data

The server includes sample candidate data in `./Data/candidates.json`:

```json
[
  {
    "id": "1",
    "name": "John Doe",
    "email": "john.doe@email.com",
    "position": "Software Engineer",
    "experience_years": 5
  },
  // ... more candidates
]
```

## 🔗 References

- [Model Context Protocol (MCP)](https://modelcontextprotocol.io/)
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [.NET 8.0 Documentation](https://docs.microsoft.com/dotnet/core/whats-new/dotnet-8)

## 📄 License

This project is part of the Copilot Studio BElux Lab series and is intended for educational and demonstration purposes.

---

**Happy MCP Development! 🚀**

For questions or issues, please refer to the troubleshooting section or check the Azure deployment logs using `azd show`.