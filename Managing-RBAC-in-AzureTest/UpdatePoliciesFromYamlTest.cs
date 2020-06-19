
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
        public void TestCheckChanges()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();
            List<KeyVaultProperties> vaultsRetrieved1 = createExpectedYamlVaults();
            List<KeyVaultProperties> yamlVaults = vaultsRetrieved1;

            // Check making 6 changes (first two only count as one change) 
            yamlVaults[0].AccessPolicies[0].PermissionsToSecrets = new string[] { "get" };
            yamlVaults[0].AccessPolicies[0].PermissionsToCertificates = new string[] { "get" };
            yamlVaults[0].AccessPolicies[1].PermissionsToCertificates = new string[] { "get" };
            yamlVaults[0].AccessPolicies[2].PermissionsToCertificates = new string[] { "get" };
            yamlVaults[0].AccessPolicies[3].PermissionsToCertificates = new string[] { "get" };
            yamlVaults[1].AccessPolicies[0].PermissionsToKeys = new string[] { "get" };
            yamlVaults[1].AccessPolicies[1].PermissionsToCertificates = new string[] { "get" };

            try
            {
                up.checkChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed 6 policies.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            // Add an entire KV
            yamlVaults.Add(new KeyVaultProperties
            {
                VaultName = "TestAddKV",
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
            });

            try
            {
                up.checkChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: KeyVault, TestAddKV, in the YAML file was not found in the JSON file.", e.Message);
            }
            yamlVaults.RemoveAt(4);

            // Remove an entire KV
            yamlVaults.RemoveAt(0);
            try
            {
                up.checkChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: KeyVault, RG1Test1, specified in the JSON file was not found in the YAML file.", e.Message);
            }
        }

        [TestMethod]
        public void TestCheckSPFields()
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
            catch
            {
                Assert.Fail();
            }

            incomplete.PermissionsToKeys = sp.PermissionsToKeys;
            incomplete.PermissionsToSecrets = null;
            try

            {
                up.checkSPInvalidFields(name, incomplete);
            }
            catch
            {
                Assert.Fail();
            }

            incomplete.PermissionsToSecrets = sp.PermissionsToSecrets;
            incomplete.PermissionsToCertificates = null;
            try

            {
                up.checkSPInvalidFields(name, incomplete);
            }
            catch
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
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
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
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            JsonInput json = ap.readJsonFile("../../../input/MasterConfig.json");
            var secrets = ap.getSecrets(json);

            // Check UsersContained less than 2 error
            try
            {
                up.updateVaults(new List<KeyVaultProperties>() { kv }, new List<KeyVaultProperties> { }, ap.createKVMClient(secrets), secrets, ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: RG1Test2 does not contain at least two users. Vault Skipped.", e.Message);
            }
        }

        [TestMethod]
        public void TestUpdateVault()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            JsonInput json = ap.readJsonFile("../../../input/MasterConfig.json");
            var secrets = ap.getSecrets(json);

            KeyVaultProperties noPermiss = new KeyVaultProperties()
            {
                VaultName = "RBACTestVault2",
                ResourceGroupName = "RBACTest",
                SubscriptionId = "6b94a915-57a9-4023-8fe8-3792e113ddff",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Opeyemi Olaoluwa",
                        Alias = "t-opolao@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { }
                    }
                }
            };

            // Check no permissions defined
            try
            {
                up.updateVault(noPermiss, ap.createKVMClient(secrets), secrets, ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Skipped User, 'Opeyemi Olaoluwa'. Does not have any permissions specified.", e.Message);
            }

            KeyVaultProperties kv = new KeyVaultProperties()
            {
                VaultName = "RBACTestVault2",
                ResourceGroupName = "RBACTest",
                SubscriptionId = "6b94a915-57a9-4023-8fe8-3792e113ddff",
                Location = "eastus",
                TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Elizabeth Mary",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                }
            };

            // Check access policy already defined for user
            try
            {
                up.updateVault(kv, ap.createKVMClient(secrets), secrets, ap.createGraphClient(secrets));
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: An access policy has already been defined for Katie Helman in {kv.VaultName}.", e.Message);
            }

            KeyVaultProperties invalid = kv;
            invalid.AccessPolicies.Add(new PrincipalPermissions()
            {
                Type = "Service Principal",
                DisplayName = "RBACAutomationApp",
                Alias = "",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            });
            invalid.AccessPolicies.Add(new PrincipalPermissions()
            {
                Type = "Service Principal",
                DisplayName = "RBACAutomationApp",
                Alias = "",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            });
            invalid.AccessPolicies.RemoveAt(0);
            invalid.AccessPolicies.RemoveAt(1);

            //Check access policy already defined for a type that is not a user
            try
            {
                up.updateVault(invalid, ap.createKVMClient(secrets), secrets, ap.createGraphClient(secrets));
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: An access policy has already been defined for RBACAutomationApp in {kv.VaultName}.", e.Message);
            }

            try
            {
                var a = up.translateShorthand("read", "Key", new string[] { "read", "write", "read - list", "storage" }, Constants.READ_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Key 'read' permission is duplicated", e.Message);
            }

            try
            {
                var a = up.translateShorthand("all", "Key", new string[] { "all", "read" }, Constants.ALL_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("'All' permission removes need for other Key permissions", e.Message);
            }

            try
            {
                var a = up.translateShorthand("write", "Key", new string[] { "delete", "read", "write" }, Constants.WRITE_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("update, create, delete permissions are already included in Key 'write' permission", e.Message);
            }

            try
            {
                var a = up.translateShorthand("all", "Key", new string[] { "all - snap" }, Constants.ALL_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Invalid Key 'all - <snap>' permission", e.Message);
            }

            try
            {
                var a = up.translateShorthand("write", "Key", new string[] { "all - write", "write - list" }, Constants.WRITE_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Invalid Key 'write - <list>' permission", e.Message);
            }

            try
            {
                var a = up.translateShorthand("write", "Key", new string[] { "write - create", "update", "create" }, Constants.WRITE_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("create permissions are already included in Key 'write' permission", e.Message);
            }

            try
            {
                var a = up.translateShorthand("all", "Key", new string[] { "all - storage, write, crypto" }, Constants.ALL_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.IsTrue(a.SequenceEqual(new string[] { "get", "list", "purge" }));
            }
            catch
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestCheckValidPermissions()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            PrincipalPermissions sp = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "Opeyemi Olaoluwa",
                Alias = "t-opolao@microsoft.com",
                PermissionsToKeys = new string[] { "getS", "list" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            // Check invalid key permission
            try
            {
                up.checkValidPermissions(sp);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Invalid key permission 'getS'", e.Message);
            }

            PrincipalPermissions invalid = sp;
            // Check invalid secret permission
            invalid.PermissionsToKeys = new string[] { };
            invalid.PermissionsToSecrets = new string[] { "managecontacts", "list" };
            try
            {
                up.checkValidPermissions(invalid);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Invalid secret permission 'managecontacts'", e.Message);
            }

            // Check invalid certificate permission
            invalid.PermissionsToSecrets = new string[] { };
            invalid.PermissionsToCertificates = new string[] { "managecontactz", "managecontactz" };
            try
            {
                up.checkValidPermissions(invalid);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Invalid certificate permission 'managecontactz'", e.Message);
            }

            // Check repeated key permissions
            invalid.PermissionsToCertificates = new string[] { };
            invalid.PermissionsToKeys = new string[] { "read", "read", "purge", "purge" };
            try
            {
                up.checkValidPermissions(invalid);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Key permission(s) 'read, purge' repeated", e.Message);
            }

            // Check repeated secret permissions, with different spacing
            invalid.PermissionsToKeys = new string[] { };
            invalid.PermissionsToSecrets = new string[] { "get", "      get" };
            try
            {
                up.checkValidPermissions(invalid);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Secret permission(s) 'get' repeated", e.Message);
            }

            // Check repeated certificate permissions, disregarding case
            invalid.PermissionsToSecrets = new string[] { };
            invalid.PermissionsToCertificates = new string[] { "all - PURGE", "all - purge" };
            try
            {
                up.checkValidPermissions(invalid);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Certificate permission(s) 'all - purge' repeated", e.Message);
            }
        }

        [TestMethod]
        public void TestTranslateShorthands()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            var pp = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "Opeyemi Olaoluwa",
                Alias = "t-opolao@microsoft.com",
                PermissionsToKeys = new string[] { "all" },
                PermissionsToSecrets = new string[] { "all" },
                PermissionsToCertificates = new string[] { "all" }
            };
            up.translateShorthands(pp);
            Assert.IsTrue(pp.PermissionsToKeys.SequenceEqual(Constants.ALL_KEY_PERMISSIONS));
            Assert.IsTrue(pp.PermissionsToSecrets.SequenceEqual(Constants.ALL_SECRET_PERMISSIONS));
            Assert.IsTrue(pp.PermissionsToCertificates.SequenceEqual(Constants.ALL_CERTIFICATE_PERMISSIONS));

            pp = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "Opeyemi Olaoluwa",
                Alias = "t-opolao@microsoft.com",
                PermissionsToKeys = new string[] { "read", "write", "storage", "crypto", "purge" },
                PermissionsToSecrets = new string[] { "read", "write", "storage", "purge" },
                PermissionsToCertificates = new string[] { "read", "write", "storage", "management", "purge" }
            };
            up.translateShorthands(pp);
            Assert.IsTrue(pp.PermissionsToKeys.All(Constants.ALL_KEY_PERMISSIONS.Contains) && pp.PermissionsToKeys.Length == Constants.ALL_KEY_PERMISSIONS.Length);
            Assert.IsTrue(pp.PermissionsToSecrets.All(Constants.ALL_SECRET_PERMISSIONS.Contains) && pp.PermissionsToSecrets.Length == Constants.ALL_SECRET_PERMISSIONS.Length);
            Assert.IsTrue(pp.PermissionsToCertificates.All(Constants.ALL_CERTIFICATE_PERMISSIONS.Contains) && pp.PermissionsToCertificates.Length == Constants.ALL_CERTIFICATE_PERMISSIONS.Length);
        }

        [TestMethod]
        public void TestTranslateShorthand()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            try
            {
                var a = up.translateShorthand("all", "Key", new string[] { "all", "all - read" }, Constants.ALL_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Key 'all' permission is duplicated", e.Message);
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
                    new PrincipalPermissions()
                    {
                        Type = "Group",
                        DisplayName = "1ES Site Reliability Engineering",
                        Alias = "1essre@microsoft.com",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Mazin Shaaeldin",
                        Alias = "t-mashaa@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
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
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
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
                    new PrincipalPermissions()
                    {
                        Type = "Group",
                        DisplayName = "RBACKeyVault",
                        Alias = "RBACKeyVault@service.microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign", "purge"},
                        PermissionsToSecrets = new string[] {  "get" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] {  "get", "list" },
                        PermissionsToSecrets = new string[] { "purge" },
                        PermissionsToCertificates = new string[] { "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
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
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Mazin Shaaeldin",
                        Alias = "t-mashaa@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers", "purge" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "Katie Helman",
                        Alias = "t-kahelm@microsoft.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore", "purge" },
                        PermissionsToCertificates = new string[] { "get", "list" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
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
        public void TestCheckVaultInvalidFields()
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
            kv.TenantId = "";
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
