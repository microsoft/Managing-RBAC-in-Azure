
﻿using Microsoft.Azure.Management.AppService.Fluent.Models;
﻿using Microsoft.Azure.Management.BatchAI.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Extensions.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute;
using RBAC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.ExceptionServices;
using YamlDotNet.Serialization;

namespace RBAC
{
    [TestClass]
    public class UpdatePoliciesFromYamlTest
    {
        [TestMethod]
        public void TestYamlDeserialization()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml("../../../expected/ExpectedOutput.yml");

            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();
            Assert.IsTrue(expectedYamlVaults.SequenceEqual(yamlVaults));
        }
        [TestMethod]
        public void TestCheckVaultChanges()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            var validVaults = createExpectedYamlVaults();
            try
            {
                up.checkVaultChanges(validVaults, validVaults[0]);
            }
            catch
            {
                Assert.Fail();
            }
            var badName = new KeyVaultProperties { VaultName = "NotExist" };
            try
            {
                up.checkVaultChanges(validVaults, badName);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("VaultName NotExist was changed or added.", e.Message);
            }

            var badRGName = new KeyVaultProperties
            {
                VaultName = "RG1Test1",
                ResourceGroupName = "RBACKeyVaultUnitTests",
                SubscriptionId = "82bf28a8-6374-4908-b89c-5d1ab5495c5e",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"
            };
            try
            {
                up.checkVaultChanges(validVaults, badRGName);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("ResourceGroupName for RG1Test1 was changed.", e.Message);
            }

            var badSubId = new KeyVaultProperties
            {
                VaultName = "RG1Test1",
                ResourceGroupName = "RBAC-KeyVaultUnitTests",
                SubscriptionId = "bleep-bloop",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"
            };
            try
            {
                up.checkVaultChanges(validVaults, badSubId);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("SubscriptionId for RG1Test1 was changed.", e.Message);
            }

            var badLoc = new KeyVaultProperties
            {
                VaultName = "RG1Test1",
                ResourceGroupName = "RBAC-KeyVaultUnitTests",
                SubscriptionId = "82bf28a8-6374-4908-b89c-5d1ab5495c5e",
                Location = "nigeria",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"
            };
            try
            {
                up.checkVaultChanges(validVaults, badLoc);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Location for RG1Test1 was changed.", e.Message);
            }
            var badTen = new KeyVaultProperties
            {
                VaultName = "RG1Test1",
                ResourceGroupName = "RBAC-KeyVaultUnitTests",
                SubscriptionId = "82bf28a8-6374-4908-b89c-5d1ab5495c5e",
                Location = "eastus",
                TenantId = "Landlord"
            };
            try
            {
                up.checkVaultChanges(validVaults, badTen);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("TenantId for RG1Test1 was changed.", e.Message);
            }
        }

       [TestMethod]
       public void CheckSPFields()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            PrincipalPermissions sp = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "Opeyemi Olaoluwa",
                Alias = "t-opolao@microsoft.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };
            PrincipalPermissions sp1 = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "Opeyemi Olaoluwa",
                Alias = "t-opolao@microsoft.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };
            string name = "vaultName";

            try
            {
                up.checkSPInvalidFields(name, sp);
            }
            catch
            {
                Assert.Fail();
            }

            PrincipalPermissions incomplete = sp1;

            incomplete.Type = null;
            try
            {
                up.checkSPInvalidFields(name, incomplete);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing Type for {name}", e.Message);
            }

            incomplete.Type = "  ";
            try
            {
                up.checkSPInvalidFields(name, incomplete);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing Type for {name}", e.Message);
            }

            incomplete.Type = sp.Type;
            incomplete.DisplayName = null;
            try
            {
                up.checkSPInvalidFields(name, incomplete);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing DisplayName for {name}", e.Message);
            }

            incomplete.DisplayName = " ";
            try
            {
                up.checkSPInvalidFields(name, incomplete);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing DisplayName for {name}", e.Message);
            }


            incomplete.DisplayName = sp.DisplayName;
            incomplete.PermissionsToKeys = null;
            try
            {
                up.checkSPInvalidFields(name, incomplete);
            }
            catch(Exception e)
            {
                Assert.Fail();
            }
            incomplete.PermissionsToKeys = sp.PermissionsToKeys;
            incomplete.PermissionsToSecrets = null;
            try
            {
                up.checkSPInvalidFields(name, incomplete);
            }
            catch (Exception e)
            {
                Assert.Fail();
            }

            incomplete.PermissionsToSecrets = sp.PermissionsToSecrets;
            incomplete.PermissionsToCertificates = null;
            try
            {
                up.checkSPInvalidFields(name, incomplete);
            }
            catch (Exception e)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestUsersContained()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);

            KeyVaultProperties kv = new KeyVaultProperties()
            {
                VaultName = "RG1Test2",
                ResourceGroupName = "RBAC",
                SubscriptionId = "subid",
                Location = "eastus",
                TenantId = "tenant",
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
                    }
                }
            };

            try
            {
                AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
                JsonInput json = ap.readJsonFile("../../../input/MasterConfig.json");
                var secrets = ap.getSecrets(json);
                up.updateVaults(new List<KeyVaultProperties>() { kv }, new List<KeyVaultProperties> { }, ap.createKVMClient(secrets), secrets, ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: RG1Test2 does not contain at least two users. Vault Skipped.", e.Message);
            }
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
                        PermissionsToKeys = new string[] {  "decrypt", "encrypt", "wrapkey", "unwrapkey", "verify", "sign" },
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
