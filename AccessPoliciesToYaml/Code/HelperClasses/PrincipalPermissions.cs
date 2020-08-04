// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace RBAC
{
    /// <summary>
    /// This class stores the AccessPolicies of a security principal.
    /// </summary>
    public class PrincipalPermissions
    {
        public PrincipalPermissions() 
        {
            this.Alias = "";
        }
        public PrincipalPermissions(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            Dictionary<string,string> typeAndName = getTypeAndName(accessPol, graphClient);

            this.Type = typeAndName["Type"];
            this.DisplayName = getDisplayName(typeAndName);
            this.Alias = getAlias(typeAndName);
            this.PermissionsToKeys = getPermissions(accessPol.Permissions.Keys);
            this.PermissionsToSecrets = getPermissions(accessPol.Permissions.Secrets);
            this.PermissionsToCertificates = getPermissions(accessPol.Permissions.Certificates);
        }


        /// <summary>
        /// This method gets the Type, DisplayName, and Alias of the security principal using the GraphServiceClient.
        /// </summary>
        /// <param name="accessPol">The current AccessPolicyEntry</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient with permissions to obtain the DisplayName</param>
        /// <returns>A string array holding the Type, DisplayName, and Alias if applicable</returns>
        private Dictionary<string,string> getTypeAndName(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            // User
            try
            {
                User user = null;
                if (graphClient.GetType() == typeof(TestGraphClient))
                {
                    var client = (TestGraphClient)graphClient;
                    user = (client.Users.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                else
                {
                    user = (graphClient.Users.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                data["Type"] = "User";
                data["DisplayName"] = user.DisplayName;
                data["Alias"] = user.UserPrincipalName;
                return data;
            }
            catch { }

            // Group
            try
            {
                Group group = null;
                if(graphClient.GetType() == typeof(TestGraphClient))
                {
                    var client = (TestGraphClient)graphClient;
                    group = (client.Groups.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                else
                {
                    group = (graphClient.Groups.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                data["Type"] = "Group";
                data["DisplayName"] = group.DisplayName; 
                data["Alias"] = group.Mail;
                return data;
            }
            catch { }

            // Application
            try
            {
                Application app = null;
                if (graphClient.GetType() == typeof(TestGraphClient))
                {
                    var client = (TestGraphClient)graphClient;
                    app = (client.Applications.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                else
                {
                    app = (graphClient.Applications.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                data["Type"] = "App";
                data["DisplayName"] = app.DisplayName;
                return data;
            }
            catch { }

            // Service Principal
            try
            {
                ServicePrincipal sp = null;
                if (graphClient.GetType() == typeof(TestGraphClient))
                {
                    var client = (TestGraphClient)graphClient;
                    sp = (client.ServicePrincipals.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                else
                {
                    sp = (graphClient.ServicePrincipals.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                }
                data["Type"] = "Service Principal";
                data["DisplayName"] = sp.DisplayName;
                return data;
            }
            // "Unknown Application
            catch
            {
                data["Type"] = "Unknown";
                return data;
            }
        }

        /// <summary>
        /// This method gets the DisplayName of the security principal.
        /// </summary>
        /// <param name="typeAndName">The string array holding the Type, DisplayName, and Alias</param>
        /// <returns>The DisplayName of the security principal if one exists. Otherwise, returns an empty string.</returns>
        private string getDisplayName(Dictionary<string,string> typeAndName)
        {
            if (typeAndName.Count() > 1)
            {
                return typeAndName["DisplayName"];
            }
            return "";
        }

        /// <summary>
        ///  This method gets the Alias of the security principal.
        /// </summary>
        /// <param name="typeAndName">A string array holding the Type, DisplayName, and Alias if applicable</param>
        /// <returns>The Alias of the security principal if one exists. Otherwise, returns an empty string.</returns>
        private string getAlias(Dictionary<string,string> typeAndName)
        {
            if (typeAndName.Count() > 2)
            {
                return typeAndName["Alias"];
            }
            return "";
        }

        /// <summary>
        /// This method gets a string array of the permissions.
        /// </summary>
        /// <param name="permissions">The list of Key, Secret, or Certificate permissions</param>
        /// <returns>Null if there were no granted permissions. Otherwise, returns the string array of permissions</returns>
        private string[] getPermissions(IList<string> permissions)
        {
            if (permissions != null && permissions.Count != 0)
            {
                return permissions.Select(val => val.Trim().ToLowerInvariant()).ToArray();
            }
            return new string[] { };
        }

        /// <summary>
        /// This method overrides the Equals operator to allow comparison between two PrincipalPermissions objects.
        /// </summary>
        /// <param name="rhs">The object to compare against</param>
        /// <returns>True if rhs is of type PrincipalPermissions and the Key, Secret, and Certificate permissions are all the same. Otherwise, returns false.</returns>
        public override bool Equals(Object rhs)
        {
            if ((rhs == null) || !this.GetType().Equals(rhs.GetType()))
            {
                return false;
            }
            else
            {
                var spp = (PrincipalPermissions)rhs;

                string type = this.Type.Trim().ToLower();
                string rhsType = spp.Type.Trim().ToLower();
                bool aliasIsSame = false;
                if (rhsType == "user" || rhsType == "group")
                {
                    aliasIsSame = (this.Alias == spp.Alias);
                }
                else
                {
                    aliasIsSame = true;
                }

                return (this.DisplayName.Trim().ToLower() == spp.DisplayName.Trim().ToLower()) && aliasIsSame && this.PermissionsToKeys.Length == spp.PermissionsToKeys.Length
                    && (this.PermissionsToKeys.ToList().ConvertAll(p => p.ToLower()).All(spp.PermissionsToKeys.ToList().ConvertAll(p => p.ToLower()).Contains)) &&
                    (this.PermissionsToSecrets.ToList().ConvertAll(p => p.ToLower()).All(spp.PermissionsToSecrets.ToList().ConvertAll(p => p.ToLower()).Contains))
                    && this.PermissionsToSecrets.Length == spp.PermissionsToSecrets.Length
                    && (this.PermissionsToCertificates.ToList().ConvertAll(p => p.ToLower()).All(spp.PermissionsToCertificates.ToList().ConvertAll(p => p.ToLower()).Contains))
                    && this.PermissionsToCertificates.Length == spp.PermissionsToCertificates.Length;
            }
        }

        [YamlIgnore]
        public string ObjectId { get; set; }
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
                    KeyPermissions = value.Select(val => val.Trim().ToLowerInvariant()).ToArray();
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
                    SecretsPermissions = value.Select(val => val.Trim().ToLowerInvariant()).ToArray();
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
                    CertificatePermissions = value.Select(val => val.Trim().ToLowerInvariant()).ToArray();
                } 
                else 
                { 
                    CertificatePermissions = new string[] { }; 
                }
            }
        }
    }
}
