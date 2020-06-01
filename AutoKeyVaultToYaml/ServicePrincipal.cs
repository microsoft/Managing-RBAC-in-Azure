using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoKeyVaultToYaml
{
    class ServicePrincipal
    {
        public ServicePrincipal(AccessPolicyEntry accessPol, GraphServiceClient graphClient)
        {
            this.ObjectId = accessPol.ObjectId;
            this.ApplicationId = accessPol.ApplicationId.ToString();
            this.DisplayName = getDisplayName(accessPol, graphClient);
            this.PermissionsToKeys = getPermissions(accessPol.Permissions.Keys);
            this.PermissionsToSecrets = getPermissions(accessPol.Permissions.Secrets);
            this.PermissionsToCertificates = getPermissions(accessPol.Permissions.Certificates);
        }

        /**
         * Retrieves and returns the service principal's DisplayName using the GraphServiceClient
         */
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

        /**
         * Converts permissions from an IList of strings to an array of strings
         * Returns null if there were no granted permissions
         * Otherwise, returns the string array
         */
        private string[] getPermissions(IList<string> permissions)
        {
            StringBuilder sb = new StringBuilder();

            if (permissions != null)
            {
                var permissionsEnum = permissions.GetEnumerator();
                while (permissionsEnum.MoveNext())
                {
                    sb.Append(permissionsEnum.Current).Append(" ");
                   
                }
                return ((sb.ToString().Length == 0) ? null : (sb.ToString().Substring(0, sb.Length - 1).Split(" ")));
            }
            return null;
        }

        public string ObjectId;
        public string ApplicationId;
        public string DisplayName;
        public string[] PermissionsToKeys;
        public string[] PermissionsToSecrets;
        public string[] PermissionsToCertificates;
    }
}
