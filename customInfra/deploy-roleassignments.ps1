$azdenv = azd env get-values --output json | ConvertFrom-Json

$targetSubscription = $azdenv.AZURE_SUBSCRIPTION_ID
az account show --query id -o tsv
if ($? -eq $false) {
    Write-Host "AZ CLI Login to the Entra ID tenant used by AZD"
    az login --scope https://graph.microsoft.com//.default
    az account set --subscription $targetSubscription
}

az account set --subscription $targetSubscription
if ($? -eq $false) {
    Write-Host "Failed to set the subscription.."
    Write-Host "Make sure you have access and are logged in with the right tenant"
    exit 1
}

$deploymentSuffix = $azdenv.MANAGED_IDENTITY_NAME -split "-" | Select-Object -Last 1
$searchName = "search-$deploymentSuffix"
$openAiName = "openai-$deploymentSuffix"
$storageName = "storage$deploymentSuffix"
$managedIdentityName = $azdenv.MANAGED_IDENTITY_NAME
$resourceGroupName = "rg-" + $azdenv.AZURE_ENV_NAME

Write-Host "Setting permissions for $searchName on $openAiName and $storageName in $resourceGroupName"
Write-Host "Setting permissions for $managedIdentityName on $storageName in $resourceGroupName"

az deployment group create --name "SearchRoleAssignment" `
        --resource-group $resourceGroupName `
        --template-file .\customInfra\RoleAssignments.bicep `
        --parameters searchName=$searchName openaiName=$openAiName storageName=$storageName containerAppManagedIdentityName=$managedIdentityName 
        