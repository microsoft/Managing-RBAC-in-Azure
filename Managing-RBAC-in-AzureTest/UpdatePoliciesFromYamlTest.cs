
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
        /// <summary>
        /// This method verifies that the yaml is deserialized properly.
        /// </summary>
        [TestMethod]
        public void TestYamlDeserialization()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml("../../../expected/ExpectedOutput.yml");

            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();
            Assert.IsTrue(expectedYamlVaults.SequenceEqual(yamlVaults));
        }

        /// <summary>
        /// This method verifies that the program handles if there are invalid fields 
        /// or changes made in the yaml other than those in the AccessPolicies.
        /// </summary>
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
            catch (Exception e)
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

        /// <summary>
        /// This method verifies how changes are counted and that the program handles the number of 
        /// changes exceeding the maximum value defined in Constants.cs or if an entire KeyVault is added/deleted from the yaml.
        /// </summary>
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

        /// <summary>
        /// This method verifies that the program handles invalid KeyVaultProperties fields.
        /// </summary>
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

        /// <summary>
        /// This method verifies that the program handles invalid PrincipalPermissions fields.
        /// </summary>
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

        /// <summary>
        /// This method verifies that the number of users in a KeyVault's AccessPolicies are being counted properly and 
        /// that the program handles if the KeyVault does not contain the minimum number of users defined in Constants.cs.
        /// </summary>
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

        /// <summary>
        /// This method verifies that the program handles if the PrincipalPermissions object does not have permissions defined, 
        /// if they already have an access policy defined, or if their shorthand permissions are invalid.
        /// </summary>
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
                        DisplayName = "Katie Helman",
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

            KeyVaultProperties invalid = new KeyVaultProperties()
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
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "RBACAutomationApp",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { }
                    },
                }
            };

            // Check access policy already defined for a type that is not a user
            try
            {
                up.updateVault(invalid, ap.createKVMClient(secrets), secrets, ap.createGraphClient(secrets));
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: An access policy has already been defined for RBACAutomationApp in {kv.VaultName}.", e.Message);
            }

            // Check invalid shorthand permissions
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

        /// <summary>
        /// This method verifies that the program handles invalid or repeated permissions.
        /// </summary>
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

        /// <summary>
        /// This method verifies that the program handles invalid or incorrect fields within each type of PrincipalPermissions.
        /// </summary>
        [TestMethod]
        public void TestVerifySP()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            JsonInput json = ap.readJsonFile("../../../input/MasterConfig.json");
            var secrets = ap.getSecrets(json);

            PrincipalPermissions user = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "Opeyemi Olaoluwa",
                PermissionsToKeys = new string[] { "get", "list" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            // Check User without defining Alias
            try
            {
                up.verifyServicePrincipal(user, "user", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Alias is required for {user.DisplayName}. User skipped.", e.Message);
            }

            // Check User with defining empty Alias
            user.Alias = "       ";
            try
            {
                up.verifyServicePrincipal(user, "user", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Alias is required for {user.DisplayName}. User skipped.", e.Message);
            }

            // Check User with wrong Alias
            user.Alias = "katie@microsoft.com";
            try
            {
                up.verifyServicePrincipal(user, "user", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Could not find User with Alias '{user.Alias}'. User skipped.", e.Message);
            }

            // Check User with wrong DisplayName
            user.Alias = "t-kahelm@microsoft.com";
            try
            {
                up.verifyServicePrincipal(user, "user", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The DisplayName '{user.DisplayName}' is misspelled and cannot be recognized. User skipped.", e.Message);
            }

            PrincipalPermissions group = new PrincipalPermissions()
            {
                Type = "Group",
                DisplayName = "RBACAutomationApp",
                PermissionsToKeys = new string[] { "get", "list" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            // Check Group without defining Alias
            try
            {
                up.verifyServicePrincipal(group, "group", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Alias is required for {group.DisplayName}. Group skipped.", e.Message);
            }

            // Check Group with defining empty Alias
            group.Alias = "   ";
            try
            {
                up.verifyServicePrincipal(group, "group", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The Alias '{group.Alias}' is incorrect for {group.DisplayName} and cannot be recognized. Group skipped.", e.Message);
            }

            // Check Group with defining wrong Alias
            group.Alias = "1es@microsoft.com";
            try
            {
                up.verifyServicePrincipal(group, "group", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The Alias '{group.Alias}' is incorrect for {group.DisplayName} and cannot be recognized. Group skipped.", e.Message);
            }

            // Check Group with wrong DisplayName
            group.Alias = "";
            group.DisplayName = "RBACAutomationApp1";
            try
            {
                up.verifyServicePrincipal(group, "group", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Could not find Group with DisplayName '{group.DisplayName}'. Group skipped.", e.Message);
            }

            PrincipalPermissions app = new PrincipalPermissions()
            {
                Type = "Application",
                DisplayName = "BingStrategy",
                PermissionsToKeys = new string[] { "get", "list" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            // Check App without defining Alias
            try
            {
                up.verifyServicePrincipal(app, "application", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.Fail();
            }

            // Check App with defining empty Alias
            app.Alias = "   ";
            try
            {
                up.verifyServicePrincipal(app, "application", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The Alias '{app.Alias}' is incorrect for {app.DisplayName} and cannot be recognized. Application skipped.", e.Message);
            }

            // Check App with defining wrong Alias
            app.Alias = "1es@microsoft.com";
            try
            {
                up.verifyServicePrincipal(app, "application", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The Alias '{app.Alias}' is incorrect for {app.DisplayName} and cannot be recognized. Application skipped.", e.Message);
            }

            // Check App with wrong DisplayName
            app.Alias = "";
            app.DisplayName = "RBACAutomationApp1";
            try
            {
                up.verifyServicePrincipal(app, "application", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Could not find Application with DisplayName '{app.DisplayName}'. Application skipped.", e.Message);
            }

            PrincipalPermissions sp = new PrincipalPermissions()
            {
                Type = "Group",
                DisplayName = "RBACAutomationApp",
                PermissionsToKeys = new string[] { "get", "list" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            // Check SP without defining Alias
            try
            {
                up.verifyServicePrincipal(sp, "service principal", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.Fail();
            }

            // Check SP with defining empty Alias
            sp.Alias = "   ";
            try
            {
                up.verifyServicePrincipal(sp, "service principal", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The Alias '{sp.Alias}' is incorrect for {sp.DisplayName} and cannot be recognized. ServicePrincipal skipped.", e.Message);
            }

            // Check SP with defining wrong Alias
            sp.Alias = "1es@microsoft.com";
            try
            {
                up.verifyServicePrincipal(sp, "service principal", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: The Alias '{sp.Alias}' is incorrect for {sp.DisplayName} and cannot be recognized. ServicePrincipal skipped.", e.Message);
            }

            // Check SP with wrong DisplayName
            sp.Alias = "";
            sp.DisplayName = "RBACAutomationApp1";
            try
            {
                up.verifyServicePrincipal(sp, "service principal", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Error: Could not find ServicePrincipal with DisplayName '{sp.DisplayName}'. ServicePrincipal skipped.", e.Message);
            }

            sp.Type = "unknown";
            try
            {
                up.verifyServicePrincipal(sp, "unknown", ap.createGraphClient(secrets));
            }
            catch (Exception e)
            {
                Assert.AreEqual($"'{sp.Type}' is not a valid type for {sp.DisplayName}. Valid types are 'User', 'Group', 'Application', or 'Service Principal'. Skipped.", e.Message);
            }
        }

        /// <summary>
        /// This method verifies that the shorthands are translated to their respective permissions properly.
        /// </summary>
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

        /// <summary>
        /// This method verifies that "all" shorthand cannot be repeated.
        /// </summary>
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
            catch (Exception e)
            {
                Assert.AreEqual("Key 'all' permission is duplicated", e.Message);
            }
        }

        /// <summary>
        /// This method verifies that the correct permissions are being identified by the shorthand keyword.
        /// </summary>
        [TestMethod]
        public void TestGetShorthandPermissions()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            try
            {
                var res = up.getShorthandPermissions("all", "key");
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Cannot remove 'all' from a permission", e.Message);
            }

            Assert.IsNull(up.getShorthandPermissions("none", "key"));
            Assert.IsNull(up.getShorthandPermissions("none", "secret"));
            Assert.IsNull(up.getShorthandPermissions("none", "certificate"));
            Assert.IsNull(up.getShorthandPermissions("read", "none"));

            Assert.IsTrue(Constants.READ_KEY_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("read", "key")));
            Assert.IsTrue(Constants.WRITE_KEY_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("write", "key")));
            Assert.IsTrue(Constants.CRYPTOGRAPHIC_KEY_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("crypto", "key")));
            Assert.IsTrue(Constants.STORAGE_KEY_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("storage", "key")));

            Assert.IsTrue(Constants.READ_SECRET_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("read", "secret")));
            Assert.IsTrue(Constants.WRITE_SECRET_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("write", "secret")));
            Assert.IsTrue(Constants.STORAGE_SECRET_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("storage", "secret")));

            Assert.IsTrue(Constants.READ_CERTIFICATE_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("read", "certificate")));
            Assert.IsTrue(Constants.WRITE_CERTIFICATE_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("write", "certificate")));
            Assert.IsTrue(Constants.MANAGEMENT_CERTIFICATE_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("management", "certificate")));
            Assert.IsTrue(Constants.STORAGE_CERTIFICATE_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("storage", "certificate")));
        }

        /// <summary>
        /// This method creates the expected yamlVaults list of KeyVaultProperties from the deserialized yaml.
        /// </summary>
        /// <returns>The list of KeyVaultProperties from the deserialized yaml</returns>
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
    }
}
