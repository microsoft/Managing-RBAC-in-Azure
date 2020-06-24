using System.Linq;
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]
namespace RBAC
{
    /// <summary>
    /// This class stores the global constants.
    /// </summary>
    public static class Constants
    {
        public const string READ_ME = "https://github.com/microsoft/Managing-RBAC-in-Azure/blob/Katie/README.md";
        public const string JSON_SAMPLE = "https://github.com/microsoft/Managing-RBAC-in-Azure/blob/Katie/Config/MasterConfigExample.json";
        public const string KVM_CLIENT = "https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.keyvault.keyvaultmanagementclient?view=azure-dotnet";
        public const string GRAPH_CLIENT_CREATE = "https://docs.microsoft.com/en-us/graph/sdks/create-client?tabs=CS";

        public const string HTTP = "https://";
        public const string AZURE_URL = ".vault.azure.net";
        public const string MICROSOFT_LOGIN = "https://login.microsoftonline.com/";
        public const string GRAPHCLIENT_URL = "https://graph.microsoft.com/.default";

        public const int MIN_NUM_USERS = 2;
        public const int MAX_NUM_CHANGES = 5;

        public enum USER_TYPES { User, Group, ServicePrincipal, Application }
        // Defines shorthands for keys
        public enum ALL_KEY_PERMISSIONS { get, list, update, create, import, delete, recover,
            backup, restore, decrypt, encrypt, unwrapkey, wrapkey, verify, sign, purge }
        public enum READ_KEY_PERMISSIONS { get, list };
        public enum WRITE_KEY_PERMISSIONS { update, create, delete }
        public enum STORAGE_KEY_PERMISSIONS { import, recover, backup, restore }
        public enum CRYPTOGRAPHIC_KEY_PERMISSIONS { decrypt, encrypt, unwrapkey, wrapkey, verify, sign }

        // Defines shorthands for secrets
        public enum ALL_SECRET_PERMISSIONS { get, list, set, delete, recover, backup, restore, purge }
        public enum READ_SECRET_PERMISSIONS { get, list }
        public enum WRITE_SECRET_PERMISSIONS { set, delete }
        public enum STORAGE_SECRET_PERMISSIONS { recover, backup, restore }

        // Defines shorthands for certificates
        public enum ALL_CERTIFICATE_PERMISSIONS { get, list, update, create, import, delete, recover, backup, restore, managecontacts,
            manageissuers, getissuers, listissuers, setissuers, deleteissuers, purge }
        public enum READ_CERTIFICATE_PERMISSIONS { get, list }
        public enum WRITE_CERTIFICATE_PERMISSIONS { update, create, delete }
        public enum STORAGE_CERTIFICATE_PERMISSIONS { import, recover, backup, restore }
        public enum MANAGEMENT_CERTIFICATE_PERMISSIONS { managecontacts, manageissuers, getissuers, listissuers, setissuers, deleteissuers }

        // Defines shorthand keywords
        public enum SHORTHANDS_KEYS { all, read, write, storage, crypto }
        public enum SHORTHANDS_SECRETS { all, read, write, storage }
        public enum SHORTHANDS_CERTIFICATES { all, read, write, storage, management }

        // Defines all valid permissions
        public enum VALID_KEY_PERMISSIONS
        {
            get, list, update, create, import, delete, recover, backup, restore, decrypt, 
            encrypt, unwrapkey, wrapkey, verify, sign, purge, all, read, write, storage, crypto
        }
        public enum VALID_SECRET_PERMISSIONS { get, list, set, delete, recover, backup, restore, purge, all, read, write, storage }
        public enum VALID_CERTIFICATE_PERMISSIONS
        {
            get, list, update, create, import, delete, recover, backup, restore, managecontacts,
            manageissuers, getissuers, listissuers, setissuers, deleteissuers, purge,
            all, read, write, storage, management
        }
    }
}
