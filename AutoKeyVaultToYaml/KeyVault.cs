using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.Network.Fluent.Topology.Definition;
using Microsoft.Graph;
using Microsoft.PowerShell.Commands;

namespace AutoKeyVaultToYaml
{
    class KeyVault
    {
        public KeyVault(Vault vault, GraphServiceClient graphClient)
        {
            this.VaultName = vault.Name;
            this.ResourceGroupName = getResourceGroup(vault.Id);
            this.Location = vault.Location;
            this.ResourceId = vault.Id;
            this.VaultUri = vault.Properties.VaultUri;
            this.TenantId = vault.Properties.TenantId.ToString();
            this.Sku = vault.Properties.Sku.Name.ToString();
            this.EnabledForDeployment = getValue(vault.Properties.EnabledForDeployment);
            this.EnabledForTemplateDeployment = getValue(vault.Properties.EnabledForTemplateDeployment);
            this.EnabledForDiskEncryption = getValue(vault.Properties.EnabledForDiskEncryption);
            this.EnableSoftDelete = getValue(vault.Properties.EnableSoftDelete);
            this.AccessPolicies = getAccessPolicies(vault.Properties.AccessPolicies, graphClient);
        }

        /**
         * Returns the ResourceGroupName from the ResourceId
         */
        private string getResourceGroup(string vaultId)
        {
            string subStr = vaultId.Substring(vaultId.IndexOf("resourceGroups/") + 15);
            return (subStr.Substring(0, subStr.IndexOf('/')));
        }

        /**
         * If the enabledProp was assigned a value, returns that value
         * Otherwise, returns null
         */
        private Nullable<bool> getValue(Nullable<bool> enabledProp)
        {
            if (enabledProp.HasValue)
            {
                return (enabledProp.Value);
            }
            return null;
        }

        /*
         * Parses through each AccessPolicyEntry and stores the data in a ServicePrincipal object
         * Returns a list of ServicePrincipal objects
         */
        private List<ServicePrincipal> getAccessPolicies(IList<AccessPolicyEntry> accessPolicies, GraphServiceClient graphClient)
        {
            List<ServicePrincipal> policies = new List<ServicePrincipal>();

            var policiesEnum = accessPolicies.GetEnumerator();
            while (policiesEnum.MoveNext())
            {
                policies.Add(new ServicePrincipal(policiesEnum.Current, graphClient));
            }

            return policies;
        }

        public string VaultName;
        public string ResourceGroupName;
        public string Location;
        public string ResourceId;
        public string VaultUri;
        public string TenantId;
        public string Sku;
        public Nullable<bool> EnabledForDeployment;
        public Nullable<bool> EnabledForTemplateDeployment;
        public Nullable<bool> EnabledForDiskEncryption;
        public Nullable<bool> EnableSoftDelete;
        public List<ServicePrincipal> AccessPolicies;
    }
}
