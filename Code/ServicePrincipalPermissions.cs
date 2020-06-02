using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RBAC
{
    class ServicePrincipalPermissions
    {
        public ServicePrincipalPermissions() { }
        public ServicePrincipalPermissions(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            this.ObjectId = accessPol.ObjectId;
            this.ApplicationId = accessPol.ApplicationId.ToString();
            this.DisplayName = getDisplayName(accessPol, graphClient);
            this.PermissionsToKeys = getPermissions(accessPol.Permissions.Keys);
            this.PermissionsToSecrets = getPermissions(accessPol.Permissions.Secrets);
            this.PermissionsToCertificates = getPermissions(accessPol.Permissions.Certificates);
        }

        /// <summary>
        /// This method gets the DisplayName of the ServicePrincipal using the GraphServiceClient.
        /// </summary>
        /// <param name="accessPol">The current AccessPolicyEntry</param>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        private string getDisplayName(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            try // User
            {
                var user = (graphClient.Users.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                return (user.DisplayName + " (" + user.UserPrincipalName + ")");
            } catch 
            {
                try // Group
                {
                    var group = (graphClient.Groups.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0];
                    return (group.DisplayName + " (" + group.Mail + ")");
                } catch
                {
                    try // Application
                    {
                        return (graphClient.Applications.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0].DisplayName;
                    } catch
                    {
                        try // Service Principal
                        {
                            return (graphClient.ServicePrincipals.Request().Filter($"Id eq '{accessPol.ObjectId}'").GetAsync().Result)[0].DisplayName;
                        } catch
                        {
                            return ""; // "Unknown" Application
                        } 
                    }
                }
            }
        }

        /// <summary>
        /// Converts permissions from an IList of strings to an array of strings
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns> Null if there were no granted permissions. Otherwise, returns the string array. </returns>
        private string[] getPermissions(IList<string> permissions)
        {
            if (permissions != null)
            {
                return permissions.ToArray();
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                var o = (ServicePrincipalPermissions)obj;
                if ((o.PermissionsToKeys == null && this.PermissionsToKeys != null) || (this.PermissionsToKeys == null && o.PermissionsToKeys != null))
                {
                    return false;
                }
                if ((o.PermissionsToSecrets == null && this.PermissionsToSecrets != null) || (this.PermissionsToSecrets == null && o.PermissionsToSecrets != null))
                {
                    return false;
                }
                if ((o.PermissionsToCertificates == null && this.PermissionsToCertificates != null) || (this.PermissionsToCertificates == null && o.PermissionsToCertificates != null))
                {
                    return false;
                }
                return (this.PermissionsToKeys == null || this.PermissionsToKeys.SequenceEqual(o.PermissionsToKeys)) && (this.PermissionsToSecrets == null || this.PermissionsToSecrets.SequenceEqual(o.PermissionsToSecrets)) && (this.PermissionsToCertificates == null || this.PermissionsToCertificates.SequenceEqual(o.PermissionsToCertificates));
            }
        }

        public string ObjectId { get; set; }
        public string ApplicationId { get; set; }
        public string DisplayName { get; set; }
        public string[] PermissionsToKeys { get; set; }
        public string[] PermissionsToSecrets { get; set; }
        public string[] PermissionsToCertificates { get; set; }
    }
}
