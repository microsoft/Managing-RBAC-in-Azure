using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;

namespace RBAC
{
    class KeyVaultProperties
    {
        public KeyVaultProperties() { }
        public KeyVaultProperties(Vault vault, GraphServiceClient graphClient)
        {
            this.VaultName = vault.Name;
            this.ResourceGroupName = getResourceGroup(vault.Id);
            this.SubscriptionId = getSubscription(vault.Id);
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
        private string getResourceGroup(string resourceId)
        {
            const string resourceGroupsLabel = "resourceGroups/";
            int resourceGroupsLabelStart = resourceId.IndexOf(resourceGroupsLabel);
            if (resourceGroupsLabelStart < 0)
            {
                throw new ArgumentException("ResourceId is invalid.");
            }
            int resourceGroupsValueStart = resourceGroupsLabelStart + resourceGroupsLabel.Length;
            string resourceGroupsValue = resourceId.Substring(resourceGroupsValueStart);
           
            int resourceGroupsValueEnd = resourceGroupsValue.IndexOf('/');
            if (resourceGroupsValueEnd > 0)
            {
                resourceGroupsValue = resourceGroupsValue.Substring(0, resourceGroupsValueEnd);
            }

            return resourceGroupsValue;
        }

        private string getSubscription(string resourceId)
        {
            const string subscriptionLabel = "subscriptions/";
            int subscriptionLabelStart = resourceId.IndexOf(subscriptionLabel);
            if (subscriptionLabelStart < 0)
            {
                throw new ArgumentException("ResourceId is invalid.");
            }
            int subscriptionValueStart = subscriptionLabelStart + subscriptionLabel.Length;
            string subscriptionValue = resourceId.Substring(subscriptionValueStart);

            int subscriptionValueEnd = subscriptionValue.IndexOf('/');
            if (subscriptionValueEnd > 0)
            {
                subscriptionValue = subscriptionValue.Substring(0, subscriptionValueEnd);
            }

            return subscriptionValue;
        }

        /**
         * If the enabledProp was assigned a value, returns that value
         * Otherwise, returns null
         */
        private bool? getValue(bool? enabledProp)
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
        private List<ServicePrincipalPermissions> getAccessPolicies(IList<AccessPolicyEntry> accessPolicies, GraphServiceClient graphClient)
        {
            List<ServicePrincipalPermissions> policies = new List<ServicePrincipalPermissions>();

            var policiesEnum = accessPolicies.GetEnumerator();
            while (policiesEnum.MoveNext())
            {
                policies.Add(new ServicePrincipalPermissions(policiesEnum.Current, graphClient));
            }

            return policies;
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                var o = (KeyVaultProperties)obj;
                return this.AccessPolicies.SequenceEqual(o.AccessPolicies);
            }
        }

        public string VaultName { get; set; }
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string Location { get; set; }
        public string ResourceId { get; set; }
        public string VaultUri { get; set; }
        public string TenantId { get; set; }
        public string Sku { get; set; }
        public bool? EnabledForDeployment { get; set; }
        public bool? EnabledForTemplateDeployment { get; set; }
        public bool? EnabledForDiskEncryption { get; set; }
        public bool? EnableSoftDelete { get; set; }
        public List<ServicePrincipalPermissions> AccessPolicies { get; set; }
    }
}
