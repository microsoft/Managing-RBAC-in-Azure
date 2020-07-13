using Microsoft.Azure.Management.KeyVault.Models;
using System.Linq;
using System.Windows.Media;

namespace Managing_RBAC_in_AzureListOptions
{
    /// <summary>
    /// This class stores the global constants.
    /// </summary>
    public static class Constants
    {  
        // Defines shorthands for keys
        public static readonly string[] ALL_KEY_PERMISSIONS = typeof(KeyPermissions).GetFields().Select(prop => prop.Name.ToLower()).ToArray();
        public static readonly string[] READ_KEY_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_KEY_PERMISSIONS = { "update", "create", "delete" };
        public static readonly string[] STORAGE_KEY_PERMISSIONS = { "import", "recover", "backup", "restore" };
        public static readonly string[] CRYPTO_KEY_PERMISSIONS = { "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign" };

        // Defines shorthands for secrets
        public static readonly string[] ALL_SECRET_PERMISSIONS = typeof(SecretPermissions).GetFields().Select(prop => prop.Name.ToLower()).ToArray();
        public static readonly string[] READ_SECRET_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_SECRET_PERMISSIONS = { "set", "delete" };
        public static readonly string[] STORAGE_SECRET_PERMISSIONS = { "recover", "backup", "restore" };

        // Defines shorthands for certificates
        public static readonly string[] ALL_CERTIFICATE_PERMISSIONS = typeof(CertificatePermissions).GetFields().Select(prop => prop.Name.ToLower()).ToArray();
        public static readonly string[] READ_CERTIFICATE_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_CERTIFICATE_PERMISSIONS = { "update", "create", "delete" };
        public static readonly string[] STORAGE_CERTIFICATE_PERMISSIONS = { "import", "recover", "backup", "restore" };
        public static readonly string[] MANAGEMENT_CERTIFICATE_PERMISSIONS = { "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" };

        // Defines shorthand keywords
        public static readonly string[] SHORTHANDS_KEYS = { "all", "read", "write", "storage", "crypto" };
        public static readonly string[] SHORTHANDS_SECRETS = { "all", "read", "write", "storage"};
        public static readonly string[] SHORTHANDS_CERTIFICATES = { "all", "read", "write", "storage", "management" };
    }
}
