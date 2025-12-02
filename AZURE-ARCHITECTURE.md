# Azure Services Architecture

This diagram shows the Azure services deployed by this application and how they connect.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              Azure Resource Group                                     │
│                              (rg-expensemgmt-demo)                                    │
│                                                                                       │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │                           App Service (UKSOUTH)                                 │  │
│  │                                                                                 │  │
│  │  ┌──────────────────┐     ┌──────────────────────────────────────────────────┐ │  │
│  │  │   Web App        │     │  User Assigned Managed Identity                  │ │  │
│  │  │   (ASP.NET 8)    │────▶│  (mid-expensemgmt-xxxxx)                          │ │  │
│  │  │                  │     │                                                   │ │  │
│  │  │  • Razor Pages   │     │  Used for:                                        │ │  │
│  │  │  • REST APIs     │     │  • SQL Database authentication                    │ │  │
│  │  │  • Swagger Docs  │     │  • Azure OpenAI authentication                    │ │  │
│  │  │  • Chat UI       │     │  • AI Search authentication                       │ │  │
│  │  └────────┬─────────┘     └──────────────────────────────────────────────────┘ │  │
│  │           │                                                                     │  │
│  └───────────┼─────────────────────────────────────────────────────────────────────┘  │
│              │                                                                        │
│              │ Managed Identity Auth                                                  │
│              │                                                                        │
│              ▼                                                                        │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │                        Azure SQL Database (UKSOUTH)                             │  │
│  │                                                                                 │  │
│  │  ┌──────────────────┐     ┌──────────────────────────────────────────────────┐ │  │
│  │  │  SQL Server      │     │  Northwind Database                               │ │  │
│  │  │  (Entra ID Auth) │────▶│  • Expenses table                                 │ │  │
│  │  │                  │     │  • Users table                                    │ │  │
│  │  │  No SQL Auth     │     │  • Categories table                               │ │  │
│  │  │  (MCAPS Policy)  │     │  • Status table                                   │ │  │
│  │  └──────────────────┘     │  • Stored Procedures                              │ │  │
│  │                           └──────────────────────────────────────────────────┘ │  │
│  └────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                       │
│  ┌────────────────────────────────────────────────────────────────────────────────┐  │
│  │                      GenAI Services (SWEDEN CENTRAL)                            │  │
│  │                      (Only deployed with deploy-with-chat.sh)                   │  │
│  │                                                                                 │  │
│  │  ┌──────────────────────────┐     ┌────────────────────────────────────────┐  │  │
│  │  │  Azure OpenAI            │     │  Azure AI Search                       │  │  │
│  │  │  (S0 SKU)                │     │  (Basic SKU)                           │  │  │
│  │  │                          │     │                                        │  │  │
│  │  │  Model: GPT-4o           │     │  For RAG pattern                       │  │  │
│  │  │  Capacity: 8             │     │  (future enhancement)                  │  │  │
│  │  │                          │     │                                        │  │  │
│  │  │  Used for:               │     │  Used for:                             │  │  │
│  │  │  • Natural language chat │     │  • Document indexing                   │  │  │
│  │  │  • Function calling      │     │  • Semantic search                     │  │  │
│  │  │  • Expense queries       │     │                                        │  │  │
│  │  └──────────────────────────┘     └────────────────────────────────────────┘  │  │
│  │                                                                                 │  │
│  └────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                       │
└─────────────────────────────────────────────────────────────────────────────────────┘

                              External Connections

┌─────────────────────┐                                   ┌─────────────────────┐
│                     │                                   │                     │
│   Web Browser       │─────────── HTTPS ────────────────▶│   App Service       │
│   (User)            │                                   │   /Index            │
│                     │                                   │                     │
└─────────────────────┘                                   └─────────────────────┘

┌─────────────────────┐                                   ┌─────────────────────┐
│                     │                                   │                     │
│   Azure CLI         │─────────── az login ─────────────▶│   Deploy Scripts    │
│   (Developer)       │                                   │   (deploy.sh)       │
│                     │                                   │                     │
└─────────────────────┘                                   └─────────────────────┘
```

## Data Flow

1. **User Request** → Web Browser sends HTTPS request to App Service
2. **App Service** → Authenticates with SQL using Managed Identity
3. **API Calls** → App calls stored procedures in Azure SQL Database
4. **Chat Requests** → App calls Azure OpenAI with function definitions
5. **Function Calling** → OpenAI returns function calls, App executes them
6. **Response** → Results returned to user

## Security

- **Entra ID Only Authentication** for SQL Server (MCAPS compliant)
- **Managed Identity** for all Azure service authentication
- **No passwords or secrets** stored in code or configuration
- **HTTPS only** with TLS 1.2 minimum

## Deployment Options

1. **deploy.sh** - Deploys App Service + SQL Database (no AI features)
2. **deploy-with-chat.sh** - Full deployment including Azure OpenAI and AI Search
