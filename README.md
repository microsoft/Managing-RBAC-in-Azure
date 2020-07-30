# Introduction
Managing-RBAC-in-Azure automates the process of adding, removing, or updating access policies in Azure KeyVault.

# Prerequisites 
This project uses the following NuGet packages:

 **AccessPoliciesToYaml:**
- Newtonsoft.Json
- YamlDotNet
- Microsoft.Graph*
- Microsoft.Azure.Management.Fluent
- Microsoft.Azure.Management.KeyVault
- Azure.Security.KeyVault.Secrets
- Azure.Identity 
- System.Collections 
- log4net

**Managing-RBAC-in-Azure.ListingOptions:**
- LiveCharts
- LiveCharts.wpf

# Getting Started 

## Creating an AAD Application
This project uses an Azure Active Directory Application (AAD) to retrieve the KeyVaults. 
1. Create your AAD Application by navigating to the **App registrations** tab within your Azure Active Directory and clicking **New registration**. Fill in the necessary information, and click **Register**. 
2. Click on the newly-created application. Take note of the Application (client) Id. 
3. Navigate to the **Certificates & secrets** tab and click **New client secret**. Fill in the necessary information, and click **Add**. 
Take note of the ClientKey value. 
4. Navigate to the **API permissions** tab and click **Add a permission**. Under **Microsoft APIs**, select **Microsoft Graph**. 
5. Click **Delegated Permissions**, expand the **User** category, and select **User.Read**, **User.Read.All**, and **User.ReadBasic.All**. 
6. Now click **Application Permissions**, expand the **User** category, and select **User.Read.All**. 
7. Now click **Add Permissions**. Note that depending on your associated Tenant, you may require a tenant admin to grant the consent.

For more information on setting up your AAD Application, [click here.](https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app)

### Granting Access to the AAD Application
Your AAD Application must be given permissions to access the KeyVaults you want to retrieve.

To do so, follow [these steps](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal) and grant your AAD Application permissions with 
the **Contributor** role in each KeyVault you want to access.

For more information on RBAC in Azure, [click here.](https://docs.microsoft.com/en-us/azure/key-vault/general/overview-security)

## Creating the MasterConfig.json File 
This project requires a custom **MasterConfig.json** file upload. 
Refer to the [MasterConfigExample.json file](Config/MasterConfigExample.json) for formatting and inputs.

There are 3 ways to obtain a list of KeyVaults: 
1. Provide only the SubscriptionId, which gets all of the KeyVaults in the subscription.
2. Provide a SubscriptionId and a ResourceGroupName, which gets all of the KeyVaults within the specified ResourceGroup. 
Note that you can add multiple ResourceGroup names per SubscriptionId and can specify specific KeyVaults if you wish. 
3. Provide a SubscriptionId, ResourceGroupName, and a list of KeyVault names, which gets all of the KeyVaults specified in the list.

# Phase 1: AccessPoliciesToYaml
This phase reads in a **MasterConfig.json** file, retrieves the specified KeyVaults and their access policies, and writes the results to **YamlOutput.yml**.

## Running AccessPoliciesToYaml

### Visual Studio
1. Open the **Managing-RBAC-in-Azure.sln** file in Visual Studio.
2. Hit CTRL-ALT-L to open the Solution Explorer. Right-click on **AccessPoliciesToYaml** and select **Properties**. Now click on the **Debug** tab.
3. Within **Application arguments**, define the file path of your local MasterConfig.json, followed by a space, and the path of the directory of which you want to write YamlOutput.yml and Log.log.

***Example arguments:*** "../../../../Config/MasterConfig.json ../../../../Config". In this example, MasterConfig.json is located in the Config folder, and YamlOutput.yml and Log.log are written to the Config folder.

4. Within **Environment variables**, define 4 variables:
	1. APP_NAME - this is the DisplayName of your AAD Application
	2. AZURE_CLIENT_ID - this is the Application (client) Id of your AAD Application
	3. AZURE_CLIENT_SECRET - this is the ClientKey of your AAD Application
	4. AZURE_TENANT_ID - this is the tenantId of your associated Tenant
5. Navigate back to the Solution Explorer. Right-click on the **Managing-RBAC-in-Azure.sln** file, select **Properties**, select **Single Startup Object**, and choose **AccessPoliciesToYaml** from the dropdown. Click **OK**. Your project is now ready to run.

## Debugging
**Log.log** contains timestamps and full debugging information.

# Phase 2: UpdatePolicesFromYaml
This phase utilizes the generated **YamlOutput.yml** file to propagate any changes to the KeyVault access policies and see those changes reflected in the Azure portal.

## Security Principals in AAD
A security principal is an object that is requesting access to Azure resources. It can be represented as a...
- **User**: an individual 
- **Group**: a set of users for which all users within that group share a role
- **Application**: an identity that is automatically managed by Azure
- **Service Principal**: a security identity that is used by applications or services to access specific Azure resources

In order to add, update, or remove access policies for a Security Principal, the Security Principal must first exist in the associated Azure Active Directory.

## Editing the Access Policies
You can edit the **YamlOutput.yml** directly to...
- Add or remove specific permissions for a Security Principal with an existing Access Policy
- Remove an existing Access Policy for a Security Principal
- Add a new Access Policy for a Security Principal
  - Note that Security Principals of Type **User** or **Group** must define **Type**, **DisplayName**, **Alias**, **PermissionsToKeys**, **PermissionsToSecrets**, and **PermissionsToCertificates**, while types **Application** and **Service Principal** require **Type**, **DisplayName**, **PermissionsToKeys**, **PermissionsToSecrets**, and **PermissionsToCertificates** only
  
Refer to the [YamlSample.yml file](Config/YamlSample.yml) for formatting.
  
### Use of Shorthands

To ease the usability aspect of the automation, we have made shorthands, or common permission groupings, available for each type of permission:
- ***Keys:***
  - **All**: all Key permissions
  - **Read**: Get and List Access
  - **Write**: Update, Create, and Delete Access
  - **Storage**: Import, Recover, Backup, and Restore Access
  - **Crypto**: All cryptographic operations i.e. Decrypt, Encrypt, UnwrapKey, WrapKey, Verify, and Sign
- ***Secrets:***
  - **All**: all Secret permissions
  - **Read**: Get and List Access
  - **Write**: Set and Delete Access
  - **Storage**: Recover, Backup, and Restore Access
- ***Certificates:***
  - **All**: all Certificate permissions
  - **Read**: Get and List Access
  - **Write**: Update, Create, and Delete Access
  - **Storage**: Import, Recover, Backup, and Restore Access
  - **Management**: ManageContact and all Certificate Authorities Accesses

#### Additional Features
- **<Shorthand> - <permission(s)>** commands are also available to remove a list of permissions, separated by commas, from the shorthand i.e. **Read - list**
   - Note that a space must be added after the shorthand keyword.
- The **All** shorthand can be used in conjunction with other shorthands i.e. **All - read**
- All of the shorthands are defined in **UpdatePoliciesFromYaml/Constants.cs** and can be modified

## Design Considerations

### Global Constants
In **UpdatePoliciesFromYaml/Constants.cs**, we have defined:
- various URL addresses utilized to create the KeyVaultManagement and Graph clients
- **MIN_NUM_USERS** to ensure that all KeyVaults contain access policies for at least this number of User
  - This number is currently set to 2, meaning that each KeyVault must define access policies for at least 2 users 
- **MAX_NUM_CHANGES** to limit the amount of changes someone can make at once
  - One change refers to changes in one Security Principal's access policies i.e. you can grant/delete any number of their permissions and it will equate to one change
  - This number is currently set to 5, meaning that someone cannot make more than 5 changes per program run
- all of the shorthand keywords as well as all valid permissions for each permission block

All of these constants can be modified should they need to change.

### DeletedPolicies.yml
A **DeletedPolicies.yml** file will be generated to display the access policies that were deleted upon each run of **UpdatePoliciesFromYaml**. This removes the need to re-run **AccessPoliciesToYaml** with every run of **UpdatePoliciesFromYaml** as it reflects the changes made in the portal since the most recent **AccessPoliciesToYaml** run. 

## Running UpdatePoliciesFromYaml

### Visual Studio
1. Open the generated **YamlOutput.yml** file from Phase 1 and make any desired changes to the access policies. Once your changes are made, save the file.
2. Hit CTRL-ALT-L to open the Solution Explorer. Right-click on **UpdatePoliciesFromYaml** and select **Properties**. Now click on the **Debug** tab.
3. Within **Application arguments**, define the file path of your local MasterConfig.json, followed by a space, the file path of your local YamlOutput.yml, followed by a space, and the path of the directory of which you want to write the DeletedPolicies.yml and the Log.log files, followed by a space, and the file path of log4net.config.

***Example arguments:*** "../../../../Config/MasterConfig.json ../../../../Config/YamlOutput.yml ../../../../Config ../../../../AccessPoliciesToYaml/log4net.config". In this example, MasterConfig.json and YamlOutput.yml are located in the Config folder and DeletedPolicies.yml and Log.log are written to the Config folder.

4. Within **Environment variables**, define 4 variables:
	1. APP_NAME - this is the DisplayName of your AAD Application
	2. AZURE_CLIENT_ID - this is the Application (client) Id of your AAD Application
	3. AZURE_CLIENT_SECRET - this is the ClientKey of your AAD Application
	4. AZURE_TENANT_ID - this is the tenantId of your associated Tenant
5. Navigate back to the Solution Explorer. Right-click on the **Managing-RBAC-in-Azure.sln** file, select **Properties**, select **Single Startup Object**, and choose **UpdatePoliciesFromYaml** from the dropdown. Click **OK**. Your project is now ready to run.

## Debugging
**Log.log** contains timestamps and full debugging information.

# Phase 3: Managing-RBAC-in-Azure.ListingOptions
We have defined 6 listing options for your benefit:
1. **List Permissions by Shorthand**: translates a shorthand keyword into its respective permissions
2. **List Assigned Permissions by Security Principal**: lists all of the access policies in regards to a specified security principal
3. **List Security Principal by Assigned Permissions**: lists all of the security principals with a specified permission
4. **Breakdown of Assigned Permissions by Percentage**: displays the percentage breakdown of the permissions or shorthands used within a specified scope
5. **List Top 10 KeyVaults by Permission Access**: lists the 10 vaults with the highest number of granted permissions or the highest number of access policies in a specified scope
6. **List Top 10 Security Principals by Permission Access**: lists the 10 security principals with the highest number of granted permissions or the highest number of access policies in a specified scope

## Running Managing-RBAC-in-Azure.ListingOptions

### Visual Studio
1. Open the **Managing-RBAC-in-Azure.sln** file in Visual Studio.
2. Hit CTRL-ALT-L to open the Solution Explorer. Right-click on the **Managing-RBAC-in-Azure.sln** file, select **Properties**, select **Single Startup Object**, and choose **Managing-RBAC-in-Azure.ListingOptions** from the dropdown. Click **OK**. Your project is now ready to run.

# Managing-RBAC-in-Azure.Tests
We have provided a series of automated test cases to verify your inputs.

## Running Managing-RBAC-in-Azure.Tests

### Visual Studio
1. Open the **Managing-RBAC-in-Azure.sln** file in Visual Studio.
2. Hit CTRL-ALT-L to open the Solution Explorer. Right-click on the **Managing-RBAC-in-Azure.Test** project and select **Run Tests**.

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

