# Chat UI Configuration

The Chat UI is integrated into the main ASP.NET Razor Pages application.

## Accessing the Chat UI

Navigate to `/Chat` on the deployed application URL.

## Features

- Natural language queries for expense data
- Function calling to interact with the database
- Pretty formatted responses for lists and data
- Markdown-style formatting support

## Configuration

The Chat UI requires the following app settings to be configured:

```json
{
  "OpenAI": {
    "Endpoint": "https://your-openai-instance.openai.azure.com/",
    "DeploymentName": "gpt-4o"
  },
  "Search": {
    "Endpoint": "https://your-search-instance.search.windows.net"
  },
  "ManagedIdentityClientId": "your-managed-identity-client-id"
}
```

## Deployment

To deploy with Chat UI enabled, use `deploy-with-chat.sh` instead of `deploy.sh`.

If deploying without GenAI services, the Chat UI will display a message explaining that GenAI services are not configured and how to enable them.

## RAG Pattern (Future Enhancement)

The Azure AI Search resource is provisioned for future RAG (Retrieval-Augmented Generation) pattern implementation. This can be used to:

- Index company expense policies
- Index historical expense data
- Provide contextual information to the AI assistant

## Security

- Uses Managed Identity for authentication to Azure OpenAI
- No API keys stored in configuration
- AZURE_CLIENT_ID environment variable set for DefaultAzureCredential
