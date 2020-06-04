using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;

namespace RBAC
{
    /// <summary>
    /// This class gets and sets the properties of an Azure KeyVault.
    /// </summary>
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

        /// <summary>
        /// This method extracts the name of the ResourceGroup from the KeyVault ResourceId.
        /// </summary>
        /// <param name="resourceId">The resourceId of the KeyVault</param>
        /// <returns>The name of the ResourceGroup to which the KeyVault belongs</returns>
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

        /// <summary>
        /// This method extracts the SubscriptionId from the KeyVault ResourceId.
        /// </summary>
        /// <param name="resourceId">The resourceId of the KeyVault</param>
        /// <returns>The SubscriptionId of the Subscription to which the KeyVault belongs</returns>
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

        /// <summary>
        /// This method gets the value of the enabledProp if one was defined. 
        /// </summary>
        /// <param name="enabledProp">The EnabledForDeployment, EnabledForTemplateDeployment, EnabledForDiskEncryption, or EnableSoftDelete property</param>
        /// <returns>The boolean value of the enabledProp if one exists. Otherwise, returns null.</returns>
        private bool? getValue(bool? enabledProp)
        {
            if (enabledProp.HasValue)
            {
                return (enabledProp.Value);
            }
            return null;
        }

        /// <summary>
        /// This method parses through each AccessPolicyEntry and stores the data from each policy entry in a ServicePrincipal object.
        /// </summary>
        /// <param name="accessPolicies">The list of AccessPolicyEntrys</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient with permissions to obtain the DisplayName</param>
        /// <returns>The list of ServicePrincipal objects</returns>
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

        /// <summary>
        /// This method overrides the Equals operator to allow for comparison between two KeyVaultProperties objects.
        /// </summary>
        /// <param name="rhs">The object to compare against</param>
        /// <returns>True if rhs is of type KeyVaultProperties and the AccessPolcies are all the same. Otherwise, returns false.</returns>
        public override bool Equals(Object rhs)
        {
            if ((rhs == null) || !this.GetType().Equals(rhs.GetType()))
            {
                return false;
            }
            else
            {
                var kvp = (KeyVaultProperties)rhs;
                return (this.VaultName == kvp.VaultName) && (this.AccessPolicies.SequenceEqual(kvp.AccessPolicies));
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
