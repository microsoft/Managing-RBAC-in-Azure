using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.Network.Fluent.HasPublicIPAddress.Definition;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoKeyVaultToYaml
{
    class Config
    {
        public AadAppKey AadAppKeyDetails { get; set; }
        public List<Resource> Resources { get; set; }
    }

    class AadAppKey
    {
        public string AadAppName { get; set; }
        public string VaultName { get; set; }
        public string ClientIdSecretName { get; set; }
        public string ClientKeySecretName { get; set; }
        public string TenantIdSecretName { get; set; }
    }
    
    class Resource
    {
        /**
         * You must include a SubscriptionId, but specific ResourceGroups are not required
         */
        Resource()
        {
            this.ResourceGroups = null;
        }
        public string SubscriptionId { get; set; }
        public List<ResourceGroup> ResourceGroups { get; set; } 
    }

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
