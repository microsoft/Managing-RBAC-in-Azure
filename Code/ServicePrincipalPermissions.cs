using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace RBAC
{
    /// <summary>
    /// This class stores the AccessPolicies of a Service Principal.
    /// </summary>
    class ServicePrincipalPermissions
    {
        public ServicePrincipalPermissions() 
        {
            this.ObjectId = "";
            this.ApplicationId = "";
            this.Alias = "";
        }
        public ServicePrincipalPermissions(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            string[] typeAndName = getTypeAndName(accessPol, graphClient);

            this.ObjectId = accessPol.ObjectId;
            this.ApplicationId = accessPol.ApplicationId.ToString();
            this.Type = typeAndName[0];
            this.DisplayName = getDisplayName(typeAndName);
            this.Alias = getAlias(typeAndName);
            this.PermissionsToKeys = getPermissions(accessPol.Permissions.Keys);
            this.PermissionsToSecrets = getPermissions(accessPol.Permissions.Secrets);
            this.PermissionsToCertificates = getPermissions(accessPol.Permissions.Certificates);
        }


        /// <summary>
        /// This method gets the Type, DisplayName, and Alias of the ServicePrincipal using the GraphServiceClient.
        /// </summary>
        /// <param name="accessPol">The current AccessPolicyEntry</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient with permissions to obtain the DisplayName</param>
        /// <returns>A string array holding the Type, DisplayName, and Alias if applicable</returns>
        private string[] getTypeAndName(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            // User
            try
            {
                var user = (graphClient.Users.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                return new string[] { "User", user.DisplayName, user.UserPrincipalName };
            }
            catch { }

            // Group
            try
            {
                var group = (graphClient.Groups.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                return new string[] { "Group", group.DisplayName, group.Mail };
            }
            catch { }

            // Application
            try
            {
                var app = (graphClient.Applications.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                return new string[] { "Application", app.DisplayName };
            }
            catch { }

            // Service Principal
            try
            {
                var sp = (graphClient.ServicePrincipals.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                return new string[] { "Service Principal", sp.DisplayName };
            }
            // "Unknown Application
            catch
            {
                return new string[] { "Unknown" };
            }
        }

        /// <summary>
        /// This method gets the DisplayName of the ServicePrincipal.
        /// </summary>
        /// <param name="typeAndName">The string array holding the Type, DisplayName, and Alias</param>
        /// <returns>The DisplayName of the Service Principal if one exists. Otherwise, returns an empty string.</returns>
        private string getDisplayName(string[] typeAndName)
        {
            if (typeAndName.Count() > 1)
            {
                return typeAndName[1];
            }
            return "";
        }

        /// <summary>
        ///  This method gets the Alias of the ServicePrincipal.
        /// </summary>
        /// <param name="typeAndName">A string array holding the Type, DisplayName, and Alias if applicable</param>
        /// <returns>The Alias of the Service Principal if one exists. Otherwise, returns an empty string.</returns>
        private string getAlias(string[] typeAndName)
        {
            if (typeAndName.Count() > 2)
            {
                return typeAndName[2];
            }
            return "";
        }

        /// <summary>
        /// This method returns a string array of the permissions.Null if there were no granted permissions. Otherwise, returns the string array. 
        /// </summary>
        /// <param name="permissions">The list of Key, Secret, or Certificate permissions</param>
        /// <returns>The string array of permissions</returns>
        private string[] getPermissions(IList<string> permissions)
        {
            if (permissions != null && permissions.Count != 0)
            {
                return permissions.ToArray();
            }
            return new string[] { };
        }

        /// <summary>
        /// This method overrides the Equals operator to allow comparison between two ServicePrincipalPermissions objects.
        /// </summary>
        /// <param name="rhs">The object to compare against</param>
        /// <returns>True if rhs is of type ServicePrincipalPermissions and the Key, Secret, and Certificate permissions are all the same. Otherwise, returns false.</returns>
        public override bool Equals(Object rhs)
        {
            if ((rhs == null) || !this.GetType().Equals(rhs.GetType()))
            {
                return false;
            }
            else
            {
                var spp = (ServicePrincipalPermissions)rhs;
                if ((spp.PermissionsToKeys == null && this.PermissionsToKeys != null) || (this.PermissionsToKeys == null && spp.PermissionsToKeys != null))
                {
                    return false;
                }
                if ((spp.PermissionsToSecrets == null && this.PermissionsToSecrets != null) || (this.PermissionsToSecrets == null && spp.PermissionsToSecrets != null))
                {
                    return false;
                }
                if ((spp.PermissionsToCertificates == null && this.PermissionsToCertificates != null) || (this.PermissionsToCertificates == null && spp.PermissionsToCertificates != null))
                {
                    return false;
                }
                return (this.ObjectId == spp.ObjectId) && (this.PermissionsToKeys == null || this.PermissionsToKeys.SequenceEqual(spp.PermissionsToKeys)) && (this.PermissionsToSecrets == null || this.PermissionsToSecrets.SequenceEqual(spp.PermissionsToSecrets)) && (this.PermissionsToCertificates == null || this.PermissionsToCertificates.SequenceEqual(spp.PermissionsToCertificates));
            }
        }

        
        public string ObjectId { get; set; }
        public string ApplicationId { get; set; }
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Alias { get; set; }
        private string[] KeyPermissions;
        public string[] PermissionsToKeys
        {
            get => KeyPermissions;
            set
            {
                if (value != null)
                {
                    KeyPermissions = value;
                }
                else
                {
                    KeyPermissions = new string[] { };
                }
            }
        }

        private string[] SecretsPermissions;
        public string[] PermissionsToSecrets
        {
            get => SecretsPermissions;
            set
            {
                if (value != null)
                {
                    SecretsPermissions = value;
                }
                else
                {
                    SecretsPermissions = new string[] { };
                }
            }
        }

        private string[] CertificatePermissions;
        public string[] PermissionsToCertificates
        {
            get => CertificatePermissions;
            set
            {
                if (value != null) 
                { 
                    CertificatePermissions = value; 
                } 
                else 
                { 
                    CertificatePermissions = new string[] { }; 
                }
            }
        }

        public static string[] allKeyPermissions = { "get", "list", "update", "create", "import", "delete", "recover",
            "backup", "restore", "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign", "purge"};
        public static string[] allSecretPermissions = { "get", "list", "set", "delete", "recover", "backup", "restore", "purge" };
        public static string[] allCertificatePermissions = {"get", "list", "update", "create", "import", "delete", "recover",
            "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers", "purge"};
    }
}
