
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
        public class Testing<T>
        {
            public T testObject { get; set; }
            public string error { get; set; }

            public Testing(T testObject, string error = null)
            {
                this.testObject = testObject;
                this.error = error;
            }

        }
        [TestMethod]
        /// <summary>
        /// Verifies that a valid yaml is able to deserialize properly
        /// </summary>
        public void TestYamlDeserializationValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();

            List<Testing<List<KeyVaultProperties>>> testCasesValid = new List<Testing<List<KeyVaultProperties>>>()
            {
                new Testing<List<KeyVaultProperties>> (up.deserializeYaml("../../../expected/ExpectedOutput.yml"))
            };

            foreach (Testing<List<KeyVaultProperties>> testCase in testCasesValid)
            {
                try
                {
                    Assert.IsTrue(expectedYamlVaults.SequenceEqual(testCase.testObject));
                }
                catch
                {
                    Assert.Fail();
                }
            }

            // UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            // List<KeyVaultProperties> yamlVaults = up.deserializeYaml("../../../expected/ExpectedOutput.yml");
            // List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();
            // Assert.IsTrue(expectedYamlVaults.SequenceEqual(yamlVaults));
        }

        [TestMethod]
        /// <summary>
        /// Verifies that the program handles if there are invalid fields 
        /// or changes made in the yaml other than those in the AccessPolicies.
        /// </summary>
        public void TestCheckVaultChangesValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();

            List<Testing<List<KeyVaultProperties>>> testCasesValid = new List<Testing<List<KeyVaultProperties>>>()
            {
                new Testing<List<KeyVaultProperties>> (createExpectedYamlVaults())
            };

            foreach (Testing<List<KeyVaultProperties>> testCase in testCasesValid)
            {
                try
                {
                    up.checkVaultChanges(expectedYamlVaults, testCase.testObject);
                }
                catch
                {
                    Assert.Fail();
                }
            }
            // UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            // var yamlVaults = createExpectedYamlVaults();
            // List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();
            // try
            // {
            //       up.checkVaultChanges(yamlVaults, yamlVaults);
            //  }
            // catch
            // {
            //     Assert.Fail();
            // }
        }

        [TestMethod]
        /// <summary>
        /// Verifies that the program handles if there are invalid fields 
        /// or changes made in the yaml other than those in the AccessPolicies.
        /// </summary>
        public void TestCheckVaultChangesInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();

            List<KeyVaultProperties> changedVaultName = createExpectedYamlVaults();
            changedVaultName[0].VaultName = "vaultNameChanged";

            List<KeyVaultProperties> changedResourceGroupName = createExpectedYamlVaults();
            changedResourceGroupName[0].ResourceGroupName = "RgNameChanged";

            List<KeyVaultProperties> changedSubscriptionId = createExpectedYamlVaults();
            changedSubscriptionId[0].SubscriptionId = "SubIdChanged";

            List<KeyVaultProperties> changedLocation = createExpectedYamlVaults();
            changedLocation[0].Location = "LocChanged";

            List<KeyVaultProperties> changedTenantId = createExpectedYamlVaults();
            changedTenantId[0].TenantId = "TenIdChanged";

            List<KeyVaultProperties> addedKv = createExpectedYamlVaults();
            addedKv.Add(new KeyVaultProperties
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

            List<KeyVaultProperties> removedKv = createExpectedYamlVaults();
            removedKv.RemoveAt(0);

            List<Testing<List<KeyVaultProperties>>> testCasesInvalid = new List<Testing<List<KeyVaultProperties>>>()
            {
                new Testing<List<KeyVaultProperties>> (changedVaultName, "KeyVault 'RG1Test1' specified in the JSON file was not found in the YAML file."),
                new Testing<List<KeyVaultProperties>> (changedResourceGroupName, "ResourceGroupName for KeyVault 'RG1Test1' was changed."),
                new Testing<List<KeyVaultProperties>> (changedSubscriptionId, "SubscriptionId for KeyVault 'RG1Test1' was changed."),
                new Testing<List<KeyVaultProperties>> (changedLocation, "Location for KeyVault 'RG1Test1' was changed."),
                new Testing<List<KeyVaultProperties>> (changedTenantId, "TenantId for KeyVault 'RG1Test1' was changed."),
                new Testing<List<KeyVaultProperties>> (addedKv, "KeyVault 'TestAddKV' in the YAML file was not found in the JSON file."),
                new Testing<List<KeyVaultProperties>> (removedKv, "KeyVault 'RG1Test1' specified in the JSON file was not found in the YAML file.")
            };

            foreach (Testing<List<KeyVaultProperties>> testCase in testCasesInvalid)
            {
                try
                {
                    up.checkVaultChanges(testCase.testObject, expectedYamlVaults);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }


        }
        [TestMethod]
        /// <summary>
        /// Verifies how changes are counted and that the program handles the number of 
        /// changes exceeding the maximum value defined in Constants.cs or if an entire KeyVault is added/deleted from the yaml.
        /// </summary>
        public void TestGetChangesValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();

            List<KeyVaultProperties> yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].AccessPolicies[0].PermissionsToKeys = new string[] { "get" };

            try
            {
                var del = up.getChanges(yamlVaults, vaultsRetrieved);
                Assert.AreEqual(1, del.Item2);
                Assert.AreEqual(1, del.Item1.Count);
                Assert.AreEqual(8, del.Item1[0].AccessPolicies[0].PermissionsToKeys.Length);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }

        }

        [TestMethod]
        /// <summary>
        /// Verifies how changes are counted and that the program handles the number of 
        /// changes exceeding the maximum value defined in Constants.cs or if an entire KeyVault is added/deleted from the yaml.
        /// </summary>
        public void TestGetChangesInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();

            // Check making 6 changes (first two only count as one change)
            List<KeyVaultProperties> changedSixPermissions = createExpectedYamlVaults();
            changedSixPermissions[0].AccessPolicies[0].PermissionsToSecrets = new string[] { "get" };
            changedSixPermissions[0].AccessPolicies[0].PermissionsToCertificates = new string[] { "get" };
            changedSixPermissions[0].AccessPolicies[1].PermissionsToCertificates = new string[] { "get" };
            changedSixPermissions[0].AccessPolicies[2].PermissionsToCertificates = new string[] { "get" };
            changedSixPermissions[0].AccessPolicies[3].PermissionsToCertificates = new string[] { "get" };
            changedSixPermissions[1].AccessPolicies[0].PermissionsToKeys = new string[] { "get" };
            changedSixPermissions[1].AccessPolicies[1].PermissionsToCertificates = new string[] { "get" };

            List<Testing<List<KeyVaultProperties>>> changes = new List<Testing<List<KeyVaultProperties>>>()
            {
                new Testing<List<KeyVaultProperties>> (changedSixPermissions, $"You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed 6 policies.")
            };

            foreach (Testing<List<KeyVaultProperties>> testCase in changes)
            {
                try
                {
                    //Call getChanges in beginning
                    up.updateVaults(changedSixPermissions, vaultsRetrieved, null, null, null);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }

            List<KeyVaultProperties> alreadyDefinedAccessPolicy = createExpectedYamlVaults();
            alreadyDefinedAccessPolicy[0].AccessPolicies.Add(new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get" },
                PermissionsToSecrets = new string[] { "get" },
                PermissionsToCertificates = new string[] { "get" }
            });

            List<Testing<List<KeyVaultProperties>>> listAlreadyDefinedAccessPolicies = new List<Testing<List<KeyVaultProperties>>>()
            {
                new Testing<List<KeyVaultProperties>> (alreadyDefinedAccessPolicy,"An access policy has already been defined for User A in KeyVault 'RG1Test1'." )
            };

            foreach (Testing<List<KeyVaultProperties>> testCase in listAlreadyDefinedAccessPolicies)
            {
                try
                {
                    //Call getChanges in beginning
                    up.getChanges(testCase.testObject, vaultsRetrieved);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }


        }

        [TestMethod]
        /// <summary>
        /// Verifies that the program handles invalid KeyVaultProperties fields.
        /// </summary>
        public void TestCheckVaultInvalidFieldsInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);

            KeyVaultProperties vaultNameNull = new KeyVaultProperties() { VaultName = null };
            KeyVaultProperties vaultNameEmpty = new KeyVaultProperties() { VaultName = "" };
            KeyVaultProperties resourceGroupNameNull = new KeyVaultProperties() { ResourceGroupName = null, VaultName = "KeyVault" };
            KeyVaultProperties resourceGroupNameEmpty = new KeyVaultProperties() { ResourceGroupName = "", VaultName = "KeyVault" };
            KeyVaultProperties subscriptionIdNull = new KeyVaultProperties() { SubscriptionId = null, VaultName = "VaultName", ResourceGroupName = "RgName" };
            KeyVaultProperties subscriptionIdEmpty = new KeyVaultProperties() { SubscriptionId = "", VaultName = "VaultName", ResourceGroupName = "RgName" };
            KeyVaultProperties locationNull = new KeyVaultProperties() { Location = null, VaultName = "VaultName", ResourceGroupName = "RGName", SubscriptionId = "SubId" };
            KeyVaultProperties locationEmpty = new KeyVaultProperties() { Location = "", VaultName = "VaultName", ResourceGroupName = "RGName", SubscriptionId = "SubId" };
            KeyVaultProperties tenantIdNull = new KeyVaultProperties() { TenantId = null, VaultName = "VaultName", ResourceGroupName = "RGName", SubscriptionId = "SubId", Location = "Loc" };
            KeyVaultProperties tenantIdEmpty = new KeyVaultProperties() { TenantId = "", VaultName = "VaultName", ResourceGroupName = "RGName", SubscriptionId = "SubId", Location = "Loc" };

            List<Testing<KeyVaultProperties>> vaults = new List<Testing<KeyVaultProperties>>()
            {
                new Testing<KeyVaultProperties> (vaultNameNull, $"Missing 'VaultName' for KeyVault '{vaultNameNull.VaultName}'" ),
                new Testing<KeyVaultProperties> (vaultNameEmpty, $"Missing 'VaultName' for KeyVault '{vaultNameEmpty.VaultName}'" ),
                new Testing<KeyVaultProperties> (resourceGroupNameNull, $"Missing 'ResourceGroupName' for KeyVault '{resourceGroupNameNull.VaultName}'"),
                new Testing<KeyVaultProperties> (resourceGroupNameEmpty, $"Missing 'ResourceGroupName' for KeyVault '{resourceGroupNameEmpty.VaultName}'" ),
                new Testing<KeyVaultProperties> (subscriptionIdNull, $"Missing 'SubscriptionId' for KeyVault '{subscriptionIdEmpty.VaultName}'" ),
                new Testing<KeyVaultProperties> (subscriptionIdEmpty, $"Missing 'SubscriptionId' for KeyVault '{subscriptionIdEmpty.VaultName}'" ),
                new Testing<KeyVaultProperties> (locationNull, $"Missing 'Location' for KeyVault '{locationNull.VaultName}'" ),
                new Testing<KeyVaultProperties> (locationEmpty, $"Missing 'Location' for KeyVault '{locationEmpty.VaultName}'" ),
                new Testing<KeyVaultProperties> (tenantIdNull, $"Missing 'TenantId' for KeyVault '{tenantIdNull.VaultName}'" ),
                new Testing<KeyVaultProperties> (tenantIdEmpty, $"Missing 'TenantId' for KeyVault '{tenantIdEmpty.VaultName}'" ),

            };

            foreach (Testing<KeyVaultProperties> testCase in vaults)
            {
                try
                {
                    up.checkVaultInvalidFields(testCase.testObject);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }

        }
        [TestMethod]
        /// <summary>
        /// Verifies that the program handles invalid PrincipalPermissions fields.
        /// </summary>
        public void TestCheckPPFieldsValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            string vaultName = "vaultName";

            PrincipalPermissions regular = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions keysNull = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = null,
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions secretsNull = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = null,
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions certificatesNull = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = null
            };

            List<Testing<PrincipalPermissions>> vaults = new List<Testing<PrincipalPermissions>>()
            {
                new Testing<PrincipalPermissions> (regular),
                new Testing<PrincipalPermissions> (keysNull),
                new Testing<PrincipalPermissions> (secretsNull),
                new Testing<PrincipalPermissions> (certificatesNull)
            };

            foreach (Testing<PrincipalPermissions> testCase in vaults)
            {
                try
                {
                    up.checkPPInvalidFields(vaultName, testCase.testObject);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// Verifies that the program handles invalid PrincipalPermissions fields.
        /// </summary>
        public void TestCheckPPFieldsInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            string vaultName = "vaultName";

            PrincipalPermissions typeNull = new PrincipalPermissions() {
                Type = null,
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions typeEmpty = new PrincipalPermissions()
            {
                Type = "",
                DisplayName = null,
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions displayNameNull = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = null,
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            PrincipalPermissions displayNameEmpty = new PrincipalPermissions()
            {
                Type = "User",
                DisplayName = "",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "get", "list", "update", "create", "import", "delete", "recover", "backup", "restore" },
                PermissionsToSecrets = new string[] { "get", "list", "set", "delete", "recover", "backup", "restore" },
                PermissionsToCertificates = new string[] { "get", "list" }
            };

            // Please Note: Alias can be null or empty

            List<Testing<PrincipalPermissions>> ppList = new List<Testing<PrincipalPermissions>>()
            {
                new Testing<PrincipalPermissions> (typeNull, $"Missing Type for {vaultName}"),
                new Testing<PrincipalPermissions> (typeEmpty, $"Missing Type for {vaultName}"),
                new Testing<PrincipalPermissions> (displayNameNull, $"Missing DisplayName for {vaultName}"),
                new Testing<PrincipalPermissions> (displayNameEmpty, $"Missing DisplayName for {vaultName}"),
            };


            foreach (Testing<PrincipalPermissions> testCase in ppList)
            {
                try
                {
                    up.checkPPInvalidFields(vaultName, testCase.testObject);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }

        }

        [TestMethod]
        /// <summary>
        /// Verifies that the number of users in a KeyVault's AccessPolicies are being counted properly and 
        /// that the program handles if the KeyVault does not contain the minimum number of users defined in Constants.cs.
        /// </summary>
        public void TestUsersContainedInvalid()

        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();

            // Check UsersContained less than 2 error
            List<KeyVaultProperties> test = createExpectedYamlVaults();
            test[0] = new KeyVaultProperties()
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

            List<Testing<List<KeyVaultProperties>>> vaults = new List<Testing<List<KeyVaultProperties>>>()
            {
                new Testing<List<KeyVaultProperties>> (test, "KeyVault 'RG1Test1' does not contain at least two users. Skipped.")
            };

            foreach (Testing<List<KeyVaultProperties>> testCase in vaults)
            {
                try
                {
                    up.updateVaults(testCase.testObject, vaultsRetrieved, null, null, null);
                    // Assert.Fail(); WHY DO I HAVE TO COMMENT THIS OUT -----------------------------------------------------------------------------------------------------------------------
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that the program handles invalid or repeated permissions.
        /// </summary>
        public void TestCheckValidPermissionsInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);

            PrincipalPermissions keyPermissionInvalid = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "getS", "list" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            PrincipalPermissions secretPermissionInvalid = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { },
                PermissionsToSecrets = new string[] { "managecontacts", "list" },
                PermissionsToCertificates = new string[] { }
            };

            PrincipalPermissions certificatePermissionInvalid = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { },
                PermissionsToSecrets = new string[] {},
                PermissionsToCertificates = new string[] { "managecontactz", "managecontactz" }
            };

            PrincipalPermissions keyPermissionRepeated = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { "read", "read", "purge", "purge" },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { }
            };

            PrincipalPermissions secretPermissionRepeatedWithSpacing = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { },
                PermissionsToSecrets = new string[] { "get", "      get" },
                PermissionsToCertificates = new string[] { }
            };

            PrincipalPermissions certificatePermissionRepeatedDisregardingCase = new PrincipalPermissions
            {
                Type = "User",
                DisplayName = "User A",
                Alias = "ua@valid.com",
                PermissionsToKeys = new string[] { },
                PermissionsToSecrets = new string[] { },
                PermissionsToCertificates = new string[] { "all - PURGE", "all - purge" }
        };

            List<Testing<PrincipalPermissions>> ppList = new List<Testing<PrincipalPermissions>>()
            {
                new Testing<PrincipalPermissions> (keyPermissionInvalid, "Invalid key permission 'gets'"),
                new Testing<PrincipalPermissions> (secretPermissionInvalid, "Invalid secret permission 'managecontacts'"),
                new Testing<PrincipalPermissions> (certificatePermissionInvalid, "Invalid certificate permission 'managecontactz'"),
                new Testing<PrincipalPermissions> (keyPermissionRepeated, "Key permission(s) 'read, purge' repeated"),
                new Testing<PrincipalPermissions> (secretPermissionRepeatedWithSpacing, "Secret permission(s) 'get' repeated"),
                new Testing<PrincipalPermissions> (certificatePermissionRepeatedDisregardingCase, "Certificate permission(s) 'all - purge' repeated")
            };


            foreach (Testing<PrincipalPermissions> testCase in ppList)
            {
                try
                {
                    up.checkValidPermissions(testCase.testObject);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }

        }
        
        /// <summary>
        /// Verifies that the shorthands are translated to their respective 
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
        /// Verifies that "all" shorthand cannot be repeated.
        /// </summary>
        [TestMethod]
        public void TestTranslateShorthandInvalid()
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
        /// Verifies that the correct permissions are being identified by the shorthand keyword.
        /// </summary>
        [TestMethod]
        public void TestGetShorthandPermissionsInvalid()
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
        /// Creates the expected yamlVaults list of KeyVaultProperties from the deserialized yaml.
        /// </summary>
        /// <returns>The list of KeyVaultProperties from the deserialized yaml</returns>
        public static List<KeyVaultProperties> createExpectedYamlVaults()
        {
            var expectedYamlVaults = new List<KeyVaultProperties>();

            expectedYamlVaults.Add(new KeyVaultProperties
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

            expectedYamlVaults.Add(new KeyVaultProperties
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

            expectedYamlVaults.Add(new KeyVaultProperties
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

            expectedYamlVaults.Add(new KeyVaultProperties
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

            return expectedYamlVaults;
        }
    }
}

