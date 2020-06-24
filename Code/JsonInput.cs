﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RBAC
{
    /// <summary>
    /// This class stores information obtained from the MasterConfig.json file.
    /// </summary>
    public class JsonInput
    {
        public AadAppKey AadAppKeyDetails { get; set; }
        public List<Resource> Resources { get; set; }

        public override bool Equals(object obj)
        {
            return obj is JsonInput input &&
                   EqualityComparer<AadAppKey>.Default.Equals(AadAppKeyDetails, input.AadAppKeyDetails) &&
                   Resources.SequenceEqual(input.Resources);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// This class stores the client information obtained from the MasterConfig.json file that is later needed to create the KeyVaultManagementClient and GraphServiceClient.
    /// </summary>
    public class AadAppKey
    {
        public string AadAppName { get; set; }
        public string VaultName { get; set; }
        public string ClientIdSecretName { get; set; }
        public string ClientKeySecretName { get; set; }
        public string TenantIdSecretName { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AadAppKey key &&
                   AadAppName == key.AadAppName &&
                   VaultName == key.VaultName &&
                   ClientIdSecretName == key.ClientIdSecretName &&
                   ClientKeySecretName == key.ClientKeySecretName &&
                   TenantIdSecretName == key.TenantIdSecretName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AadAppName, VaultName, ClientIdSecretName, ClientKeySecretName, TenantIdSecretName);
        }
    }

    /// <summary>
    /// This class stores the Resources information specified in the MasterConfig.json file.
    /// </summary>
    /// <remarks>The MasterConfig.json file must include a SubscriptionId, but specific ResourceGroups are not required.</remarks>
    public class Resource
    {
        public Resource()
        {
            this.ResourceGroups = new List<ResourceGroup>();
        }
        public string SubscriptionId { get; set; }
        public List<ResourceGroup> ResourceGroups { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Resource resource &&
                   SubscriptionId == resource.SubscriptionId &&
                   ResourceGroups.SequenceEqual(resource.ResourceGroups);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionId, ResourceGroups);
        }
    }

    /// <summary>
    /// This class stores the details on the ResourceGroups specified in the MasterConfig.json file.
    /// </summary>
    /// <remarks>If the ResourceGroups field is not null, the MasterConfig.json file must include a ResourceGroup Name, but specific KeyVault names are not required.</remarks>
    public class ResourceGroup
    {
        public ResourceGroup()
        {
            this.KeyVaults = new List<string>();
        }
        public string ResourceGroupName { get; set; }
        public List<string> KeyVaults { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ResourceGroup group &&
                   ResourceGroupName == group.ResourceGroupName &&
                   KeyVaults.SequenceEqual(group.KeyVaults);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceGroupName, KeyVaults);
        }
    }
}
