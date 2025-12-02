#!/bin/bash
# Deploy script for Expense Management System with GenAI Services
# Deploys infrastructure, GenAI resources, and application

set -e

echo "=========================================="
echo "Expense Management System Deployment"
echo "With GenAI Services (Azure OpenAI + AI Search)"
echo "=========================================="

# Configuration - Update these values
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo ""
echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Admin: $ADMIN_LOGIN"
echo ""

# Create resource group
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure with GenAI
echo "Deploying Azure infrastructure with GenAI services..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters adminLogin=$ADMIN_LOGIN adminObjectId=$ADMIN_OBJECT_ID deployGenAI=true \
    --query "properties.outputs" \
    --output json)

# Extract outputs
WEB_APP_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.webAppName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
WEB_APP_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.webAppUrl.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo ""
echo "Infrastructure deployed:"
echo "  Web App: $WEB_APP_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"
echo ""

# Wait for SQL Server to be ready
echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

# Install Python dependencies
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server name
echo "Updating Python scripts with deployment values..."
sed -i.bak "s/sql-expensemgmt-UNIQUESUFFIX.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-UNIQUESUFFIX.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-UNIQUESUFFIX.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo "Importing database schema..."
python3 run-sql.py

# Configure managed identity roles
echo "Configuring managed identity database roles..."
python3 run-sql-dbrole.py

# Deploy stored procedures
echo "Deploying stored procedures..."
python3 run-sql-stored-procs.py

# Configure App Service settings
echo "Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:$SQL_SERVER_FQDN,1433;Database=$DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $WEB_APP_NAME \
    --connection-string-type SQLAzure \
    --settings DefaultConnection="$CONNECTION_STRING" \
    --output none

az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $WEB_APP_NAME \
    --settings \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
        "Search__Endpoint=$SEARCH_ENDPOINT" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none

# Build and deploy application
echo "Building application..."
cd src/ExpenseManagement
dotnet publish -c Release -o ./publish

echo "Creating deployment package..."
cd publish
zip -r ../../../app.zip ./*
cd ../../..

echo "Deploying application to Azure..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $WEB_APP_NAME \
    --src-path ./app.zip \
    --type zip

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Application URL: $WEB_APP_URL/Index"
echo ""
echo "Note: The URL to view the app is $WEB_APP_URL/Index"
echo "      (not just the root URL)"
echo ""
echo "GenAI Features enabled:"
echo "  - AI Chat Assistant with natural language database queries"
echo "  - Function calling for expense operations"
echo ""
