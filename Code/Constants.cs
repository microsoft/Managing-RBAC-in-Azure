using Microsoft.Azure.Management.KeyVault.Models;
using System.Linq;
using System;
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]

namespace RBAC
{
    /// <summary>
    /// This class stores the global constants.
    /// </summary>
    public static class Constants
    {
        public const string HTTP = "https://";
        public const string AZURE_URL = ".vault.azure.net";
        public const string MICROSOFT_LOGIN = "https://login.microsoftonline.com/";
        public const string GRAPHCLIENT_URL = "https://graph.microsoft.com/.default";

        public const int MIN_NUM_USERS = 2;
        public const int MAX_NUM_CHANGES = 5;

        //Defines the user types
        public enum USER_TYPES { User, Group, ServicePrincipal, Application }
        // Defines shorthands for keys
        public static readonly string[] ALL_KEY_PERMISSIONS = typeof(KeyPermissions).GetFields().Select(prop => prop.Name.ToLower()).ToArray();
        public enum READ_KEY_PERMISSIONS { get, list }
        public enum WRITE_KEY_PERMISSIONS { update, create, delete }
        public enum STORAGE_KEY_PERMISSIONS { import, recover, backup, restore }
        public enum CRYPTOGRAPHIC_KEY_PERMISSIONS { decrypt, encrypt, unwrapkey, wrapkey, verify, sign }

        // Defines shorthands for secrets
        public static readonly string[] ALL_SECRET_PERMISSIONS = typeof(SecretPermissions).GetFields().Select(prop => prop.Name.ToLower()).ToArray();
        public enum READ_SECRET_PERMISSIONS { get, list }
        public enum WRITE_SECRET_PERMISSIONS { set, delete }
        public enum STORAGE_SECRET_PERMISSIONS { recover, backup, restore }

        // Defines shorthands for certificates
        public static readonly string[] ALL_CERTIFICATE_PERMISSIONS = typeof(CertificatePermissions).GetFields().Select(prop => prop.Name.ToLower()).ToArray();
        public enum READ_CERTIFICATE_PERMISSIONS { get, list }
        public enum WRITE_CERTIFICATE_PERMISSIONS { update, create, delete }
        public enum STORAGE_CERTIFICATE_PERMISSIONS { import, recover, backup, restore }
        public enum MANAGEMENT_CERTIFICATE_PERMISSIONS { managecontacts, manageissuers, getissuers, listissuers, setissuers, deleteissuers }

        // Defines shorthand keywords
        public enum SHORTHANDS_KEYS { all, read, write, storage, crypto }
        public enum SHORTHANDS_SECRETS { all, read, write, storage }
        public enum SHORTHANDS_CERTIFICATES { all, read, write, storage, management }

        // Defines all valid permissions
        public static readonly string[] VALID_KEY_PERMISSIONS = ALL_KEY_PERMISSIONS.Concat(Enum.GetNames(typeof(SHORTHANDS_KEYS))).ToArray();
        public static readonly string[] VALID_SECRET_PERMISSIONS = ALL_SECRET_PERMISSIONS.Concat(Enum.GetNames(typeof(SHORTHANDS_SECRETS))).ToArray();
        public static readonly string[] VALID_CERTIFICATE_PERMISSIONS = ALL_CERTIFICATE_PERMISSIONS.Concat(Enum.GetNames(typeof(SHORTHANDS_CERTIFICATES))).ToArray();
    }
}
