﻿using Microsoft.Azure.Management.EventHub.Fluent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RBAC
{
    public static class Constants
    {
        public const string HTTP = "https://";
        public const string AZURE_URL = ".vault.azure.net";
        public const string MICROSOFT_LOGIN = "https://login.microsoftonline.com/";
        public const string GRAPHCLIENT_URL = "https://graph.microsoft.com/.default";
        public const string LOG_FILE_PATH = @"..\..\..\..\log\results.log";
        public const string LOG_FILE_PATH2 = @"..\..\..\..\log\results2.log";
        public static StreamWriter log =  new StreamWriter(new FileStream(LOG_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write));
        public static StreamWriter log2 = new StreamWriter(new FileStream(LOG_FILE_PATH2, FileMode.OpenOrCreate, FileAccess.Write));

        public static string toggle = "";
        public static StreamWriter getLog()
        {
            if (toggle == "phase1")
            {
                return log;
            }
            else 
            {
                return log2;
            }
           
        }

        public const int MIN_NUM_USERS = 2;
        public const int MAX_NUM_CHANGES = 5;

        // Defines shorthands for keys
        public static readonly string[] ALL_KEY_PERMISSIONS = { "get", "list", "update", "create", "import", "delete", "recover",
            "backup", "restore", "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign", "purge" };
        public static readonly string[] READ_KEY_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_KEY_PERMISSIONS = { "update", "create", "delete" };
        public static readonly string[] STORAGE_KEY_PERMISSIONS = { "import", "recover", "backup", "restore" };
        public static readonly string[] CRYPTOGRAPHIC_KEY_PERMISSIONS = { "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign" };

        // Defines shorthands for secrets
        public static readonly string[] ALL_SECRET_PERMISSIONS = { "get", "list", "set", "delete", "recover", "backup", "restore", "purge" };
        public static readonly string[] READ_SECRET_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_SECRET_PERMISSIONS = { "set", "delete" };
        public static readonly string[] STORAGE_SECRET_PERMISSIONS = { "recover", "backup", "restore" };

        // Defines shorthands for certificates
        public static readonly string[] ALL_CERTIFICATE_PERMISSIONS = { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts",
            "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers", "purge" };
        public static readonly string[] READ_CERTIFICATE_PERMISSIONS = { "get", "list" };
        public static readonly string[] WRITE_CERTIFICATE_PERMISSIONS = { "update", "create", "delete" };
        public static readonly string[] STORAGE_CERTIFICATE_PERMISSIONS = { "import", "recover", "backup", "restore" };
        public static readonly string[] MANAGEMENT_CERTIFICATE_PERMISSIONS = { "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" };

        // Defines shorthand keywords
        public static readonly string[] SHORTHANDS_KEYS = { "all", "read", "write", "storage", "crypto" };
        public static readonly string[] SHORTHANDS_SECRETS = { "all", "read", "write", "storage" };
        public static readonly string[] SHORTHANDS_CERTIFICATES = { "all", "read", "write", "storage", "management" };

        // Defines all valid permissions
        public static readonly string[] VALID_KEY_PERMISSIONS = ALL_KEY_PERMISSIONS.Concat(SHORTHANDS_KEYS).ToArray();
        public static readonly string[] VALID_SECRET_PERMISSIONS = ALL_SECRET_PERMISSIONS.Concat(SHORTHANDS_SECRETS).ToArray();
        public static readonly string[] VALID_CERTIFICATE_PERMISSIONS = ALL_CERTIFICATE_PERMISSIONS.Concat(SHORTHANDS_CERTIFICATES).ToArray();
    }
}
