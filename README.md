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
1. Create your AAD Application by navigating to the **App registrations** tab within your Azure Active Directory and clicking **New registration**. Fill in 
the necessary information, and click **Register**. 
2. Click on the newly-created application. Take note of the Application (client) Id. 
3. Navigate to the **Certificates & secrets** tab and click **New client secret**. Fill in the necessary information, and click **Add**. 
Take note of the ClientKey value. 
4. Navigate to the **API permissions** tab and click **Add a permission**. Under **Microsoft APIs**, select **Microsoft Graph**. 
5. Click **Delegated Permissions**, expand the **User** category, and select **User.Read**, **User.Read.All**, and **User.ReadBasic.All**. 
6. Now click **Application Permissions**, expand the **User** category, and select **User.Read.All**. 
7. Now click **Add Permissions**. Note that depending on your associated Tenant, your access requests for these API permissions may require manual approval 
and could result in a multi-day process. 

For more information on setting up your AAD Application, [click here.](https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app)

### Granting Access to the AAD Application
Your AAD Application must be given permissions to access the KeyVaults you want to retrieve.

To do so, follow [these steps](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal) and grant your AAD Application permissions with 
the **Contributor** role in each KeyVault you want to access.

For more information on RBAC in Azure, [click here.](https://docs.microsoft.com/en-us/azure/key-vault/general/overview-security)

## Storing the Secrets in a KeyVault 
Follow [these steps](https://docs.microsoft.com/en-us/azure/key-vault/secrets/quick-create-portal) to create an Azure KeyVault and add three secrets 
for the AAD Application ClientId, ClientKey, and your AAD tenantId. The tenantId can be found in the **Overview** tab within Azure Active Directory. 

## Creating the MasterConfig.json File 
This project requires a custom MasterConfig.json file upload. 
Refer to the [MasterConfigExample.json file](Config/MasterConfigExample.json) for formatting and inputs.

Note that **all** of the fields within **AadAppKeyDetails** are required, but not all fields are required within **Resources** for each Resource object.

There are 3 ways to obtain a list of KeyVaults: 
- **EXAMPLE 1** - Provide only the SubscriptionId, which gets all of the KeyVaults in the subscription.
- **EXAMPLE 2** - Provide a SubscriptionId and a ResourceGroupName, which gets all of the KeyVaults within the specified ResourceGroup. 
Note that you can add multiple ResourceGroup names per SubscriptionId and can specify specific KeyVaults if you wish. 
- **EXAMPLE 3** - Provide a SubscriptionId, ResourceGroupName, and a list of KeyVault names, which gets all of the KeyVaults specified in the list.

## Defining your File Paths
To define the location of your input MasterConfig.json file and the output YamlOutput.yml file, edit the **Project Properties**. 
Click on the **Debug** tab and within **Application arguments**, add your file path to the json file, enter a space, and add your file path to the yaml file.

## Security Principals in AAD
A security principal is an object that is requesting access to Azure resources. It can be represented as a...
- **User** - an individual 
- **Group** - a set of users for which all users within that group share a role
- **Application** - an identity that is automatically managed by Azure
- **Service Principal** - a security identity that is used by applications or services to access specific Azure resources

In order to add, update, or remove access policies for a Security Principal, the Security Principal must first exist in the associated Azure Active Directory.

# Editing the Access Policies
The generated YamlOutput.yml file will be used to propagate any changes to the KeyVault access policies. 
You can do so by editing the YamlOutput.yml directly to...
- Add or remove specific permissions for a Security Principal with an existing Access Policy
- Remove an existing Access Policy for a Security Principal
- Add a new Access Policy for a Security Principal
  - Note that Security Principals of Type **User** or **Group** must define **Type**, **DisplayName**, **Alias**, **PermissionsToKeys**, **PermissionsToSecrets**, and **PermissionsToCertificates**, while types **Application** and **Service Principal** require **Type**, **DisplayName**, **PermissionsToKeys**, **PermissionsToSecrets**, and **PermissionsToCertificates** only
  
Refer to the [YamlSample.yml file](Config/YamlSample.yml) for formatting.
  
## Use of Shorthands
- We have also made shorthands available for each type of Permission:
#### Keys:
  - **All**: all Key permissions
  - **Read**: Get and List Access
  - **Write**: Update, Create, and Delete Access
  - **Storage**: Import, Recover, Backup, and Restore Access
  - **Crypto**: All cryptographic operations i.e. Decrypt, Encrypt, UnwrapKey, WrapKey, Veryfy, and Sign
  #### Secrets
  - **All**: all Secret permissions
  - **Read**: Get and List Access
  - **Write**: Set and Delete Access
  - **Storage**: Recover, Backup, and Restore Access
  #### Certificates
  - **All**: all Certificate permissions
  - **Read**: Get and List Access
  - **Write**: Update, Create, and Delete Access
  - **Storage**: Import, Recover, Backup, and Restore Access
  - **Management**: ManageContact and all Certificate Authorities Accesses

- **<Shorthand> - <permission(s)>** commands are also available to remove a list of permissions, separated by commas, from the shorthand i.e. **Read - list**
   - Ensure you add a space after the shorthand!
- The **All** shorthand can be used in conjunction with other shorthands i.e. **All - read**
- All of the shorthands are defined in the **Constants.cs** file and can be modified

## Global Constants and Design Considerations
In the **Constants.cs** file, we have defined:
- various URL addresses utilized to create the KeyVaultManagement and Graph clients
- **MIN_NUM_USERS** to ensure that all KeyVaults contain access policies for at least this number of User
  - This number is currently set to 2, meaning that each KeyVault must define access policies for at least 2 users. 
- **MAX_NUM_CHANGES** to limit the amount of changes someone can make at once
  - One change refers to changes in one Security Principal's access policies i.e. you can grant/delete any number of their permissions and it will equate to one change
  - This number is currently set to 5, meaning that someone cannot make more than 5 changes per program run
- all of the shorthand keywords as well as all valid permissions for each permission block

All of these constants can be modified should they need to change.

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

