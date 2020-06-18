using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Extensions.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;
using RBAC;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.ExceptionServices;
using YamlDotNet.Serialization;

namespace Managing_RBAC_in_AzureTest
{
    [TestClass]
    public class UpdatePoliciesFromYamlTest
    {
        [TestMethod]
        public void TestYamlDeserialization()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            string yaml = System.IO.File.ReadAllText("../../../expected/ExpectedOutput.yml");
            var deserializer = new DeserializerBuilder().Build();
            List<KeyVaultProperties> yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);

            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();
            Assert.IsTrue(expectedYamlVaults.Equals(yamlVaults));
        }

        private List<KeyVaultProperties> createExpectedYamlVaults()
        {
            var exp = new List<KeyVaultProperties>();

            exp.Add(new KeyVaultProperties
            {
                VaultName = "RG1Test1",
                ResourceGroupName = "RBAC-KeyVaultUnitTests",
                SubscriptionId = "82bf28a8-6374-4908-b89c-5d1ab5495c5e",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions
                    {
                        Type = "Group",
                        DisplayName = "1ES Site Reliability Engineering",
                        Alias = "1essre@microsoft.com",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Mazin Shaaeldin",
                        Alias = "t-mashaa@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Opeyemi Olaoluwa",
                        Alias = "t-opolao@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    }
                }
            });

            exp.Add(new KeyVaultProperties
            {
                VaultName = "RG1Test2",
                ResourceGroupName = "RBAC-KeyVaultUnitTests",
                SubscriptionId = "82bf28a8-6374-4908-b89c-5d1ab5495c5e",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions
                    {
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Opeyemi Olaoluwa",
                        Alias = "t-opolao@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    }
                }
            });

            exp.Add(new KeyVaultProperties
            {
                VaultName = "PremiumRBACTest",
                ResourceGroupName = "RBACTest",
                SubscriptionId = "6b94a915-57a9-4023-8fe8-3792e113ddff",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions
                    {
                        Type = "Group",
                        DisplayName = "RBACKeyVault",
                        Alias = "RBACKeyVault@service.microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign", "purge"},
                        PermissionsToSecrets = new string[] {  "get" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list" },
                        PermissionsToSecrets = new string[] { "purge" },
                        PermissionsToCertificates = new string[] { "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Opeyemi Olaoluwa",
                        Alias = "t-opolao@microsoft.com",
                        PermissionsToKeys = new string[] {  "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign" },
                        PermissionsToSecrets = new string[] {  "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list" }
                    }
                }
            });

            exp.Add(new KeyVaultProperties
            {
                VaultName = "RBACTestVault1",
                ResourceGroupName = "RBACTest",
                SubscriptionId = "6b94a915-57a9-4023-8fe8-3792e113ddff",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Mazin Shaaeldin",
                        Alias = "t-mashaa@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers", "purge" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore", "purge" },
                        PermissionsToCertificates = new string[] { "get", "list" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "Opeyemi Olaoluwa",
                        Alias = "t-opolao@microsoft.com",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list" }
                    }
                }
            });

            return exp;
        }

        [TestMethod]
        public void testCheckVaultInvalidFields()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);

            // Vault Name null
            KeyVaultProperties kv = new KeyVaultProperties();
            kv.VaultName = null;
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing VaultName for {kv.VaultName}", e.Message);
            }

            // Vault Name empty
            kv.VaultName = "";
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing VaultName for {kv.VaultName}", e.Message);
            }

            // Resource Group name null
            kv.VaultName = "Vault Name";
            kv.ResourceGroupName = null;
            try
            {    
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing ResourceGroupName for {kv.VaultName}", e.Message);
            }

            // Resource group name empty
            kv.ResourceGroupName = "";
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing ResourceGroupName for {kv.VaultName}", e.Message);
            }

            // Subscription id null
            kv.ResourceGroupName = "Resource Group Name";
            kv.SubscriptionId = null;
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing SubscriptionId for {kv.VaultName}", e.Message);
            }

            // Subscription id empty
            kv.SubscriptionId = "";
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing SubscriptionId for {kv.VaultName}", e.Message);
            }

            // Location null 
            kv.SubscriptionId = "SubscriptionId";
            kv.Location = null;
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing Location for {kv.VaultName}", e.Message);
            }

            // Location empty
            kv.Location = "";
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing Location for {kv.VaultName}", e.Message);
            }

            // Tenant id null
            kv.Location = "Location";
            kv.TenantId = null;
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing TenantId for {kv.VaultName}", e.Message);
            }

            // Tenant id empty
            kv.TenantId = null;
            try
            {
                up.checkVaultInvalidFields(kv);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing TenantId for {kv.VaultName}", e.Message);
            }
        }

    }
}
