using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using YamlDotNet.Serialization;

namespace RBAC
{
    /// <summary>
    /// This class gets and sets the properties of an Azure KeyVault.
    /// </summary>
    class KeyVaultProperties
    {
        public KeyVaultProperties() { }
        public KeyVaultProperties(Vault vault, GraphServiceClient graphClient, StreamWriter log)
        {
            this.VaultName = vault.Name;
            this.ResourceGroupName = getResourceGroup(vault.Id);
            this.SubscriptionId = getSubscription(vault.Id);
            this.Location = vault.Location;
            this.TenantId = vault.Properties.TenantId.ToString();
            this.AccessPolicies = getAccessPolicies(vault.Properties.AccessPolicies, graphClient, log);
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
        /// This method parses through each AccessPolicyEntry and stores the data from each policy entry in a ServicePrincipal object.
        /// </summary>
        /// <param name="accessPolicies">The list of AccessPolicyEntrys</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient with permissions to obtain the DisplayName</param>
        /// <returns>The list of ServicePrincipal objects</returns>
        private List<PrincipalPermissions> getAccessPolicies(IList<AccessPolicyEntry> accessPolicies, GraphServiceClient graphClient, StreamWriter log)
        {
            List<PrincipalPermissions> policies = new List<PrincipalPermissions>();

            var policiesEnum = accessPolicies.GetEnumerator();
            while (policiesEnum.MoveNext())
            {
                policies.Add(new PrincipalPermissions(policiesEnum.Current, graphClient, log));
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

        /// <summary>
        /// This method counts the amount of users contained in the KeyVault.
        /// </summary>
        /// <returns>The number of users with access policies</returns>
        public int usersContained()
        {
            int count = 0;
            foreach(PrincipalPermissions sp in AccessPolicies)
            {
                if (sp.Type.Trim().ToLower() == "user")
                {
                    count++;
                }
            }
            return count;
        }

        public string VaultName { get; set; }
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string Location { get; set; }
        public string TenantId { get; set; }
        public List<PrincipalPermissions> AccessPolicies { get; set; }
    }
}
