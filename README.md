# Aspire Knowledge Base Chat

This sample application leverages .NET Aspire to demonstrate a Retrieval-Augmented Generation (RAG) pattern implementation. The application is designed to create a conversational AI experience that can answer questions based on knowledge extracted from Azure DevOps wiki pages.


## Overview

The KBChat application showcases how to:

- Use .NET Aspire for building cloud-native applications with distributed architecture
- Implement a RAG pattern to enhance AI responses with specific domain knowledge
- Connect to and extract information from Azure DevOps wiki pages as a knowledge source
- Provide a conversational interface for users to query the knowledge base
- Leverage Azure services like Azure OpenAI, Cosmos DB, and Vector Search for AI capabilities

## Architecture

The application consists of:

- A backend API built with .NET
- A frontend web interface with React
- Vector search capabilities for knowledge retrieval
- Integration with Azure DevOps for wiki content ingestion
- Chat history management using Cosmos DB

## Prerequisites

Before you begin developing or deploying the KBChat application, ensure you have the following:

### Development Environment
- **.NET 9 SDK** - Required for building and running the application
- **Node.js** and **npm** - For the React frontend development
- **Docker** - For containerization during development and deployment
- **Visual Studio 2022** or **Visual Studio Code** - Recommended IDEs

### Azure Resources
Even for local development, the following Azure resources are required as there are no local alternatives:

- **Azure AI Search** - For vector storage and semantic search capabilities
- **Azure Storage Account** - For storing knowlegde base data
- **Azure OpenAI Service** - For AI capabilities and embeddings generation

### Azure Authentication
- **Azure CLI** - Must be installed and you must be logged in
  ```bash
  # Verify login status
  az account show
  
  # Login if needed
  az login
  ```
- **Azure Permissions** - Your account must have:
  - Search Index Data Contributor role on the Azure AI Search service
  - Blob Contributor role on the Azure Storage Account
  - Cognitive Services User role on the Azure OpenAI service


### IDE Extensions
- **C# Dev Kit** extension for VS Code (if using VS Code)
- **.NET Aspire** extension for Visual Studio 2022 (if using Visual Studio)

## Configuration

All application configuration is managed centrally in the KBChat.AppHost project. This follows the .NET Aspire pattern for streamlined configuration management. The configuration includes:

- **Azure DevOps Settings**: Connection details for the Azure DevOps wiki source
- **Azure OpenAI Settings**: Configuration for the AI model used for chat responses
- **Cosmos DB Settings**: Database connection for storing chat threads and messages
- **Storage Account Settings**: For managing application storage needs
- **EntraID Settings**: Authentication configuration
- **Sample Prompts Settings**: Pre-configured prompts for demonstration

To configure the application, update the appropriate settings in the `appsettings.json` file of the AppHost project or use environment variables following the .NET configuration provider pattern. You could also leverage a user secrets file.

### User Secrets Setup

For development, it's recommended to use .NET User Secrets to store sensitive configuration. Here are sample commands to set up user secrets:

```bash
# Initialize user secrets for the AppHost project
dotnet user-secrets init --project KBChat.AppHost

# Add Azure OpenAI settings
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-openai-endpoint.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "your-openai-key"
dotnet user-secrets set "AzureOpenAI:Deployment" "your-deployment-name"

# Add Azure DevOps settings
dotnet user-secrets set "AzureDevOps:Organization" "your-organization"
dotnet user-secrets set "AzureDevOps:Project" "your-project"
dotnet user-secrets set "AzureDevOps:PersonalAccessToken" "your-pat"

# Add Cosmos DB connection string
dotnet user-secrets set "ConnectionStrings:Cosmos" "your-cosmos-connection-string"

# Add Storage Account connection string
dotnet user-secrets set "ConnectionStrings:Storage" "your-storage-connection-string"

# View all configured secrets
dotnet user-secrets list --project KBChat.AppHost
```

User secrets are stored in your user profile directory and are automatically loaded during development, overriding values in appsettings.json.

## Authentication Setup

To enable authentication in the application, you need to create two separate app registrations in Azure Active Directory (Entra ID):

### 1. API App Registration

This registration is for the backend API with a custom scope for chat access:

1. Navigate to the Azure Portal and go to **Microsoft Entra ID** > **App registrations**
2. Click **New registration**
3. Enter a name (e.g. `KBChat-API`)
4. Select supported account types (Single tenant or multitenant based on your requirements)
5. Leave the redirect URI blank as this is an API
6. Click **Register**
7. Once created, go to **Expose an API** in the left menu
8. Click on **Add a scope**
9. Set the scope name as `chat` 
10. Add admin and user consent display names and descriptions (e.g. "Access KBChat API")
11. Set state to **Enabled**
12. Click **Add scope**
13. Copy the **Application (client) ID** from the Overview page - you'll need it for configuration

### 2. Frontend App Registration

This registration is for the React frontend with appropriate redirect URIs:

1. Navigate to the Azure Portal and go to **Microsoft Entra ID** > **App registrations**
2. Click **New registration**
3. Enter a name (e.g. `KBChat-Frontend`) 
4. Select supported account types (must match the API registration)
5. Add the redirect URI as **Single-page application (SPA)** with value `http://localhost` for local development
6. Click **Register** 
7. Once created, go to **API permissions** in the left menu
8. Click **Add a permission** > **My APIs**
9. Select your API app registration (e.g. `KBChat-API`)
10. Check the `chat` scope you created earlier
11. Click **Add permissions**
12. Copy the **Application (client) ID** from the Overview page - you'll need it for configuration

### Updating Configuration

Add the app registration details to your user secrets or appsettings:

```bash
# API App Registration settings
dotnet user-secrets set "EntraID:ApiClientId" "your-api-client-id"
dotnet user-secrets set "EntraID:Instance" ""https://login.microsoftonline.com/<your tenant id>"

# Frontend App Registration settings
dotnet user-secrets set "EntraID:FrontendClientId" "your-frontend-client-id"
```

## Azure DevOps Personal Access Token

To allow the application to access your Azure DevOps wiki pages, you need to create a Personal Access Token (PAT) with Wiki.Read permissions:

1. Navigate to your Azure DevOps organization (e.g., https://dev.azure.com/your-organization/)
2. Click **User Settings** next to your profile picture in the top right corner
3. Select **Personal access tokens**
4. Click **+ New Token**
5. Enter a name for your token (e.g., "KBChat Wiki Access")
6. Under **Organization**, select the organization where your wiki is located
7. For expiration, choose an appropriate timeframe (consider security implications)
8. Under **Scopes**, select "Custom defined"
9. Scroll down to the **Wiki** section
10. Check the box for **Wiki** > **Read**
11. Click **Create** to generate the token
12. Copy the token immediately (you won't be able to see it again)
13. Add the token to your configuration:

```bash
# Add Azure DevOps PAT to user secrets
dotnet user-secrets set "Parameters:ADOPAT" "your-pat"
```

> **IMPORTANT:** The Personal Access Token is a sensitive secret and should never be stored in source code or configuration files that might be committed to a repository. Always use secrets.json (via user secrets) for local development. Treat this token with the same level of protection as a password, as it provides access to your Azure DevOps resources.

Make sure the organization and project settings in your configuration match the wiki you want to access. The PAT will be used to authenticate API calls to retrieve wiki content for your knowledge base.


## Deployment to Azure

The KBChat application can be deployed to Azure using the Azure Developer CLI (AZD). This process automates the provisioning of all required Azure resources and deploys your application.

### Prerequisites

1. **Azure Developer CLI (AZD)** - Install from [aka.ms/azd-install](https://aka.ms/azd-install)
2. **Azure CLI** - Make sure it's installed and you're logged in
3. **Docker** - Required for building container images
4. **PowerShell** - Required for post-provision scripts

### Deployment Steps

1. **Ensure you're logged into Azure CLI**
   ```bash
   # Verify you're logged in to Azure
   az account show
   
   # If not logged in, use this command
   az login
   ```

2. **Initialize your AZD environment**
   ```bash
   # Initialize the AZD environment with a name for your resources
   azd init
   ```

3. **Provision resources and deploy**
   ```bash
   # This single command will provision Azure resources and deploy your application
   azd up
   ```
   During this process:
   - Azure resources defined in Bicep files will be provisioned
   - Your application will be built and containerized
   - The application will be deployed to Azure Container Apps
   - Post-provision scripts will run to set up role assignments

4. **Monitor the deployment**
   You can monitor the deployment in the terminal output or in the Azure portal.

### What Gets Deployed

The deployment includes:
- Azure Container Apps for the API and frontend services
- Azure OpenAI service for AI capabilities
- Cosmos DB for thread storage
- Azure Storage for application data
- Azure AI Search for vector storage and search capabilities
- Necessary role assignments via custom scripts

### Custom Role Assignments

The deployment includes a post-provision hook that runs `customInfra/deploy-roleassignments.ps1` to set up necessary role assignments between services. This ensures your services have the proper permissions to communicate with each other.

### Accessing Your Deployed Application

After deployment completes, AZD will output URLs for your deployed services. You can access:
- The frontend web application
- The .NET Aspire Dashboard

> The API endpoints are not publicly exposed when deployed to Azure.

### Special Note for Deployed Applications

> **Important:** After deploying to Azure, remember to update your frontend app registration in Entra ID (Azure AD) with the new deployed URL:
>
> 1. Go to **Microsoft Entra ID** > **App registrations**
> 2. Select your frontend application (e.g., `KBChat-Frontend`)
> 3. Go to **Authentication** in the left menu
> 4. Under **Platform configurations** > **Single-page application**, click **Add URI**
> 5. Add the production URL of your deployed frontend app 
> 6. Click **Save**
>
> Failure to add the deployed URL to the redirect URIs will result in authentication errors when users attempt to log in from the deployed application.

### Troubleshooting

If you encounter issues during deployment:
1. Check the AZD logs in the terminal output
2. Review resources in the Azure portal
3. Examine Container Apps logs for application-specific errors
4. Verify all required role assignments are in place



