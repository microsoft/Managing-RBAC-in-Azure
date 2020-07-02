
using Managing_RBAC_in_AzureTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var yamlVaults = createExpectedYamlVaults();
            List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();
            try
            {
                up.checkVaultChanges(yamlVaults, yamlVaults);
            }
            catch
            {
                Assert.Fail();
            }

            yamlVaults[0].VaultName = "NotExist";
            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("KeyVault 'RG1Test1' specified in the JSON file was not found in the YAML file.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].ResourceGroupName = "BadRG";
            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("ResourceGroupName for KeyVault 'RG1Test1' was changed.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].SubscriptionId = "BadSi";
            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("SubscriptionId for KeyVault 'RG1Test1' was changed.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].Location = "BadLoc";
            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Location for KeyVault 'RG1Test1' was changed.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].TenantId = "BadTen";
            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("TenantId for KeyVault 'RG1Test1' was changed.", e.Message);
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
                        DisplayName = "SP1",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions
                    {
                        Type = "User",
                        DisplayName = "User A",
                        Alias = "ua@valid.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    }
                }
            });

            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"KeyVault 'TestAddKV' in the YAML file was not found in the JSON file.", e.Message);
            }
            yamlVaults.RemoveAt(4);

            // Remove an entire KV
            yamlVaults.RemoveAt(0);
            try
            {
                up.checkVaultChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"KeyVault 'RG1Test1' specified in the JSON file was not found in the YAML file.", e.Message);
            }
        }

        /// <summary>
        /// This method verifies how changes are counted and that the program handles the number of 
        /// changes exceeding the maximum value defined in Constants.cs or if an entire KeyVault is added/deleted from the yaml.
        /// </summary>
        [TestMethod]
        public void TestGetChanges()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();
            List<KeyVaultProperties> yamlVaults = createExpectedYamlVaults();

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
                //Call getChanges in beginning
                up.updateVaults(yamlVaults, vaultsRetrieved, null, null, null);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed 6 policies.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].AccessPolicies.Add(new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get" },
                PermissionsToSecrets = new string[] { "get" },
                PermissionsToCertificates = new string[] { "get" }
            });
            try
            {
                up.getChanges(yamlVaults, vaultsRetrieved);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("An access policy has already been defined for User A in KeyVault 'RG1Test1'.", e.Message);
            }

            yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].AccessPolicies[0].PermissionsToKeys = new string[] { "get" };
            try
            {
                var del = up.getChanges(yamlVaults, vaultsRetrieved);
                Assert.AreEqual(1, del.Item2);
                Assert.AreEqual(1, del.Item1.Count);
                Assert.AreEqual(8, del.Item1[0].AccessPolicies[0].PermissionsToKeys.Length);
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
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
                Assert.AreEqual($"Missing 'VaultName' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'VaultName' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'ResourceGroupName' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'ResourceGroupName' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'SubscriptionId' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'SubscriptionId' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'Location' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'Location' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'TenantId' for KeyVault '{kv.VaultName}'", e.Message);
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
                Assert.AreEqual($"Missing 'TenantId' for KeyVault '{kv.VaultName}'", e.Message);
            }
        }

        /// <summary>
        /// This method verifies that the program handles invalid PrincipalPermissions fields.
        /// </summary>
        [TestMethod]
        public void TestCheckPPFields()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            PrincipalPermissions sp = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions sp1 = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            string name = "vaultName";

            try
            {
                up.checkPPInvalidFields(name, sp);
            }
            catch
            {
                Assert.Fail();
            }

            PrincipalPermissions incomplete = sp1;
            incomplete.Type = null;
            try
            {
                up.checkPPInvalidFields(name, incomplete);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing Type for {name}", e.Message);
            }

            incomplete.Type = "  ";
            try
            {
                up.checkPPInvalidFields(name, incomplete);
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
                up.checkPPInvalidFields(name, incomplete);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual($"Missing DisplayName for {name}", e.Message);
            }

            incomplete.DisplayName = " ";
            try
            {
                up.checkPPInvalidFields(name, incomplete);
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
                up.checkPPInvalidFields(name, incomplete);
            }
            catch
            {
                Assert.Fail();
            }

            incomplete.PermissionsToKeys = sp.PermissionsToKeys;
            incomplete.PermissionsToSecrets = null;
            try

            {
                up.checkPPInvalidFields(name, incomplete);
            }
            catch
            {
                Assert.Fail();
            }

            incomplete.PermissionsToSecrets = sp.PermissionsToSecrets;
            incomplete.PermissionsToCertificates = null;
            try

            {
                up.checkPPInvalidFields(name, incomplete);
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
            var vaultsRetrieved = createExpectedYamlVaults();
            var yamlVaults = createExpectedYamlVaults();
            yamlVaults[0] = new KeyVaultProperties()
            {
                VaultName = "RG1Test1",
                ResourceGroupName = "RG1",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                Location = "eastus",
                TenantId = "00000000-0000-0000-0000-000000000000",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "SP1",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User A",
                        Alias = "ua@valid.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    }
                }
            };

            // Check UsersContained less than 2 error
            try
            {
                up.updateVaults(yamlVaults, vaultsRetrieved, null, null, null);
            }
            catch (Exception e)
            {
                Assert.AreEqual($"KeyVault 'RG1Test1' does not contain at least two users. Skipped.", e.Message);
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
                DisplayName = "User A",
                Alias = "ua@valid.com",
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
                Assert.AreEqual("Invalid key permission 'gets'", e.Message);
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
        /// This method verifies that the shorthands are translated to their respective permissions properly.
        /// </summary>
        [TestMethod]
        public void TestTranslateShorthands()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            var pp = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "all" },
                PermissionsToSecrets = new string[] { "all" },
                PermissionsToCertificates = new string[] { "all" }
            };
            up.translateShorthands(pp);
            Assert.IsTrue(pp.PermissionsToKeys.All(Constants.ALL_KEY_PERMISSIONS.Contains));
            Assert.IsTrue(pp.PermissionsToSecrets.All(Constants.ALL_SECRET_PERMISSIONS.Contains));
            Assert.IsTrue(pp.PermissionsToCertificates.All(Constants.ALL_CERTIFICATE_PERMISSIONS.Contains));

            pp = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
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
            Assert.IsTrue(Constants.CRYPTO_KEY_PERMISSIONS.SequenceEqual(up.getShorthandPermissions("crypto", "key")));
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
                ResourceGroupName = "RG1",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                Location = "eastus",
                TenantId = "00000000-0000-0000-0000-000000000000",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "Group",
                        DisplayName = "g1",
                        Alias = "g1@valid.com",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User A",
                        Alias = "ua@valid.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User B",
                        Alias = "ub@valid.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User C",
                        Alias = "uc@valid.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    }
                }
            });

            exp.Add(new KeyVaultProperties
            {
                VaultName = "RG1Test2",
                ResourceGroupName = "RG1",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                Location = "eastus",
                TenantId = "00000000-0000-0000-0000-000000000000",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "SP1",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User A",
                        Alias = "ua@valid.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User B",
                        Alias = "ub@valid.com",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    }
                }
            });

            exp.Add(new KeyVaultProperties
            {
                VaultName = "RG2Test1",
                ResourceGroupName = "RG2",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                Location = "eastus",
                TenantId = "00000000-0000-0000-0000-000000000000",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "Application",
                        DisplayName = "a1",
                        Alias = "",
                        PermissionsToKeys = new string[] {  "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "decrypt", "encrypt", "unwrapkey", "wrapkey", "verify", "sign", "purge"},
                        PermissionsToSecrets = new string[] {  "get" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User A",
                        Alias = "ua@valid.com",
                        PermissionsToKeys = new string[] {  "get", "list" },
                        PermissionsToSecrets = new string[] { "purge" },
                        PermissionsToCertificates = new string[] { "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User B",
                        Alias = "ub@valid.com",
                        PermissionsToKeys = new string[] {  "decrypt", "encrypt", "wrapkey", "unwrapkey", "verify", "sign" },
                        PermissionsToSecrets = new string[] {  "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list" }
                    }
                }
            });

            exp.Add(new KeyVaultProperties
            {
                VaultName = "RG2Test2",
                ResourceGroupName = "RG2",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                Location = "eastus",
                TenantId = "00000000-0000-0000-0000-000000000000",
                AccessPolicies = new List<PrincipalPermissions>()
                {
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User A",
                        Alias = "ua@valid.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers", "purge" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User B",
                        Alias = "ub@valid.com",
                        PermissionsToKeys = new string[] { },
                        PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore", "purge" },
                        PermissionsToCertificates = new string[] { "get", "list" }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "Service Principal",
                        DisplayName = "SP1",
                        Alias = "",
                        PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                        PermissionsToSecrets = new string[] {  "get", "list", "set", "delete", "recover", "backup", "restore" },
                        PermissionsToCertificates = new string[] { }
                    },
                    new PrincipalPermissions()
                    {
                        Type = "User",
                        DisplayName = "User C",
                        Alias = "uc@valid.com",
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

