[assembly: log4net.Config.XmlConfigurator]

namespace RBAC
{
    /// <summary>
    /// This class stores the global constants for phase 1.
    /// </summary>
    public static class AccessPoliciesToYamlConstants
    {
        public const string HTTP = "https://";
        public const string AZURE_URL = ".vault.azure.net";
        public const string MICROSOFT_LOGIN = "https://login.microsoftonline.com/";
        public const string GRAPHCLIENT_URL = "https://graph.microsoft.com/.default";
        public const string SUBS_PATH = "/subscriptions/";
        public const string RESGROUP_PATH = "/resourceGroups/";
        public const string VAULT_PATH = "/providers/Microsoft.KeyVault/vaults/";
    }
}
