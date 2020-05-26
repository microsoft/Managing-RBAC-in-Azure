using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoKeyVaultToYaml
{
    class ServicePrincipal
    {
        public ServicePrincipal(IAzure azure, IAccessPolicy accessPolicies)
        {
            this.ObjectId = accessPolicies.ObjectId;
            this.ApplicationId = accessPolicies.ApplicationId;
            this.DisplayName = getDisplayName(azure);
            this.PermissionsToKeys = getKeyPermissions(accessPolicies);
            this.PermissionsToSecrets = getSecretPermissions(accessPolicies);
            this.PermissionsToCertificates = getCertificatePermissions(accessPolicies);
        }

        private string getDisplayName(IAzure azure)
        {
            //IActiveDirectoryUser user = azure.AccessManagement.ActiveDirectoryUsers.GetById(this.ObjectId);
            return "name";
        }
        private string[] getKeyPermissions(IAccessPolicy accessPolicies)
        {
            StringBuilder sb = new StringBuilder();

            var keyEnum = accessPolicies.Permissions.Keys.GetEnumerator();
            while (keyEnum.MoveNext())
            {
                sb.Append(keyEnum.Current).Append(" ");
            }
            return ((sb.ToString().Length == 0) ? null : (sb.ToString().Substring(0, sb.Length - 1).Split(" ")));
        }
        private string[] getSecretPermissions(IAccessPolicy accessPolicies)
        {
            StringBuilder sb = new StringBuilder();

            var secretEnum = accessPolicies.Permissions.Secrets.GetEnumerator();
            while (secretEnum.MoveNext())
            {
                sb.Append(secretEnum.Current).Append(" ");
            }

            return ((sb.ToString().Length == 0) ? null : (sb.ToString().Substring(0, sb.Length - 1).Split(" ")));
        }

        private string[] getCertificatePermissions(IAccessPolicy accessPolicies)
        {
            StringBuilder sb = new StringBuilder();

            var certifEnum = accessPolicies.Permissions.Certificates.GetEnumerator();
            while (certifEnum.MoveNext())
            {
                sb.Append(certifEnum.Current).Append(" ");
            }
            return ((sb.ToString().Length == 0) ? null : (sb.ToString().Substring(0, sb.Length - 1).Split(" ")));
        }

        public string ObjectId;
        public string ApplicationId;
        public string DisplayName;
        public string[] PermissionsToKeys;
        public string[] PermissionsToSecrets;
        public string[] PermissionsToCertificates;
    }
}
