# Prerequisites 
This project uses the following NuGet packages: 
- Newtonsoft.Json
- YamlDotNet
- Microsoft.Graph*
- Microsoft.Azure.Management
- Azure.Security.KeyVault
- Azure.Identity 
- System.Collections 

# Getting Started 

## The AAD Application 
This project uses an Azure Active Directory Application to retrieve the KeyVaults. 
1. Create your AAD Application by navigating to the "App registrations" tab within your Azure Active Directory and clicking "New registration". Fill in 
the necessary information, and click "Register". 
2. Click on the newly-created application. Take note of the Application (client) Id. 
3. Navigate to the "Certificates & secrets" tab and click "New client secret". Fill in the necessary information, and click "Add". 
Take note of the ClientKey value. 
4. Navigate to the "API permissions" tab and click "Add a permission". Under "Microsoft APIs", select "Microsoft Graph". 
5. Click "Delegated Permissions", expand the "User" category, and select "User.Read", "User.Read.All", and "User.ReadBasic.All". 
6. Now click "Application Permissions", expand the "User" category, and select "User.Read.All". 
7. Now click "Add Permissions". Note that depending on your associated Tenant, your access requests for these API permissions may require manual approval 
and could result in a multi-day process. 

For more information on setting up your AAD Applciation, visit https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app. 

### Granting Access to the AAD Application
Your AAD Application must be given permissions to access the KeyVaults you want to retrieve.

To do so, follow [these steps](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal") and grant your AAD Application permissions with 
the "Contributor" role in each KeyVault you want to access.

For more information on RBAC in Azure, visit https://docs.microsoft.com/en-us/azure/key-vault/general/overview-security.

## Storing the Secrets in a KeyVault 
Follow [these steps](https://docs.microsoft.com/en-us/azure/key-vault/secrets/quick-create-portal) to create an Azure KeyVault and add three secrets 
for the AAD Application ClientId, ClientKey, and your AAD tenantId. The tenantId can be found in the "Overview" tab within Azure Active Directory. 

## Creating the MasterConfig.json File 
This project requires a custom MasterConfig.json file upload. 
Refer to the [MasterConfigExample.json file](Config\MasterConfigExample.json) for formatting and inputs.
``` Note: All of the fields within "AadAppKeyDetails" are required, but not all fields are required within "Resources" for each Resource object.```

There are 3 ways to obtain a list of KeyVaults: 
- EXAMPLE1: Provide only the SubscriptionId -> this gets all of the KeyVaults in the subscription.
- EXAMPLE2: Provide a SubscriptionId and a ResourceGroupName -> this gets all of the KeyVaults within the specified ResourceGroup. 
Note that you can add multiple ResourceGroup names per SubscriptionId and can specify specific KeyVaults if you wish. 
- EXAMPLE3: Provide a SubscriptionId, ResourceGroupName, and a list of KeyVault names -> this gets all of the KeyVaults specified in the list.

# Contributing 
This project welcomes contributions and suggestions. Most contributions require you to agree to a 
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us 
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com. 

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a 
CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions 
provided by the bot. You will only need to do this once across all repos using our CLA. 

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact 
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments. 

