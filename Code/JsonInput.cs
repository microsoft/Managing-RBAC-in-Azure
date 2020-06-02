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
    /// This class stores information for accessing client information obtaind from the MasterConfig.json file.
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
    /// This class stores information on resources specified in the MasterConfig.json file.
    /// </summary>
    class Resource
    {
        Resource()
        {
            this.ResourceGroups = null;
        }
        public string SubscriptionId { get; set; }
        public List<ResourceGroup> ResourceGroups { get; set; }
        //You must include a SubscriptionId, but specific ResourceGroups are not required
    }
    /// <summary>
    /// This class stores details on the resource groups specified in the MasterConfig.json file.
    /// </summary>
    class ResourceGroup
    {
        /**
         * If ResourceGroups is not null, you must include a ResourceGroup Name, but specific KeyVault names are not required
         */
        ResourceGroup()
        {
            this.KeyVaults = null;
        }
        public string ResourceGroupName { get; set; }
        public List<string> KeyVaults { get; set; }
    }
}
