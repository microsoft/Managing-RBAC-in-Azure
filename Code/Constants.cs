using Microsoft.Azure.Management.EventHub.Fluent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RBAC
{
    public static class Constants
    {
        public const string HTTP = "https://";
        public const string AZURE_URL = ".vault.azure.net";
        public const string MICROSOFT_LOGIN = "https://login.microsoftonline.com/";
        public const string GRAPHCLIENT_URL = "https://graph.microsoft.com/.default";

        public const int MIN_NUM_USERS = 2;

        // All permissions offered by Azure per permission block
        public static readonly string[] ALL_KEY_PERMISSIONS = { "get", "list", "update", "create", "import", "delete", "recover",
            "backup", "restore", "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign", "purge" };
        public static readonly string[] ALL_SECRET_PERMISSIONS = { "get", "list", "set", "delete", "recover", "backup", "restore", "purge" };
        public static readonly string[] ALL_CERTIFICATE_PERMISSIONS = { "get", "list", "update", "create", "import", "delete", "recover",
            "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers", "purge" };

        // All permissions offered by Azure and short-hands per permission block
        public static readonly string[] VALID_KEY_PERMISSIONS = ALL_KEY_PERMISSIONS.Concat(new string[] { "all", "read", "write", "storage", "crypto" }).ToArray();
        public static readonly string[] VALID_SECRET_PERMISSIONS = ALL_SECRET_PERMISSIONS.Concat(new string[] { "all", "read", "write", "storage" }).ToArray();
        public static readonly string[] VALID_CERTIFICATE_PERMISSIONS = ALL_CERTIFICATE_PERMISSIONS.Concat(new string[] { "all", "read", "write", "storage", "manage" }).ToArray();

        // Defines short-hands for Keys
        public static readonly string[] READ_KEY_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_KEY_PERMISSIONS = { "update", "create", "delete" };
        public static readonly string[] CRYPTOGRAPHIC_KEY_PERMISSIONS = { "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign" };
        public static readonly string[] STORAGE_KEY_PERMISSIONS = { "import", "recover", "backup", "restore" };

        // Defines short-hands for Secrets
        public static readonly string[] READ_SECRET_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_SECRET_PERMISSIONS = { "set", "delete" };
        public static readonly string[] STORAGE_SECRET_PERMISSIONS = { "recover", "backup", "restore" };

        // Defines short-hands for Certificates
        public static readonly string[] READ_CERTIFICATE_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_CERTIFICATE_PERMISSIONS = { "update", "create", "delete" };
        public static readonly string[] STORAGE_CERTIFICATE_PERMISSIONS = { "import", "recover", "backup", "restore" };
        public static readonly string[] MANAGE_CERTIFICATE_PERMISSIONS = { "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" };
    }
}
