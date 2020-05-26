using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoKeyVaultToYaml
{
    class Vault
    {
        public Vault(IAzure azure, IVault vault)
        {
            this.VaultName = vault.Name;
            this.ResourceGroupName = vault.ResourceGroupName;
            this.Location = vault.RegionName;
            this.ResourceId = vault.Id;
            this.VaultUri = vault.VaultUri;
            this.TenantId = vault.TenantId;
            this.Sku = vault.Sku.Name.ToString();
            this.EnabledForDeployment = vault.EnabledForDeployment;
            this.EnabledForTemplateDeployment = vault.EnabledForTemplateDeployment;
            this.EnabledForDiskEncryption = vault.EnabledForDiskEncryption;
            this.EnableSoftDelete = false; //FIX
            this.AccessPolicies = getAccessPolicies(azure, vault);
        }
        
        private List<ServicePrincipal> getAccessPolicies(IAzure azure, IVault vault)
        {
            List<ServicePrincipal> policies = new List<ServicePrincipal>();

            var accessPoliciesEnum = vault.AccessPolicies.GetEnumerator();
            while (accessPoliciesEnum.MoveNext())
            {
                ServicePrincipal servicePrincipal = new ServicePrincipal(azure, accessPoliciesEnum.Current);
                policies.Add(servicePrincipal);
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
        public bool EnabledForDeployment;
        public bool EnabledForTemplateDeployment;
        public bool EnabledForDiskEncryption;
        public bool EnableSoftDelete;
        public List<ServicePrincipal> AccessPolicies;
    }
}
