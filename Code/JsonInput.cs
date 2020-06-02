using System.Collections.Generic;

namespace RBAC
{
    /// <summary>
    /// This class stores information obtained from the MasterConfig.json file.
    /// </summary>
    class JsonInput
    {
        public AadAppKey AadAppKeyDetails { get; set; }
        public List<Resource> Resources { get; set; }
    }

    /// <summary>
    /// This class stores the client information obtained from the MasterConfig.json file that is later needed to create the KeyVaultManagementClient and GraphServiceClient.
    /// </summary>
    class AadAppKey
    {
        public string AadAppName { get; set; }
        public string VaultName { get; set; }
        public string ClientIdSecretName { get; set; }
        public string ClientKeySecretName { get; set; }
        public string TenantIdSecretName { get; set; }
    }

    /// <summary>
    /// This class stores the Resources information specified in the MasterConfig.json file.
    /// </summary>
    /// <remarks>The MasterConfig.json file must include a SubscriptionId, but specific ResourceGroups are not required.</remarks>
    class Resource
    {
        Resource()
        {
            this.ResourceGroups = null;
        }
        public string SubscriptionId { get; set; }
        public List<ResourceGroup> ResourceGroups { get; set; }
    }

    /// <summary>
    /// This class stores the details on the ResourceGroups specified in the MasterConfig.json file.
    /// </summary>
    /// <remarks>If the ResourceGroups field is not null, the MasterConfig.json file must include a ResourceGroup Name, but specific KeyVault names are not required.</remarks>
    class ResourceGroup
    {
        ResourceGroup()
        {
            this.KeyVaults = null;
        }
        public string ResourceGroupName { get; set; }
        public List<string> KeyVaults { get; set; }
    }
}
