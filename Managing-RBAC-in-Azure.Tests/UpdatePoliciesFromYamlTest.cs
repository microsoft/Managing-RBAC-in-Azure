// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Constants = RBAC.UpdatePoliciesFromYamlConstants;

namespace RBAC
{
    [TestClass]
    /// <summary>
    /// This class is the testing class for UpdatePoliciesFromYaml.
    /// </summary>
    public class UpdatePoliciesFromYamlTest
    {
        /// <summary>
        /// This is a wrapper class that is used for testing purposes.
        /// </summary>
        public class Testing<T>
        {
            public T testObject { get; set; }
            public string error { get; set; }

            /// <summary>
            /// Constructor to create an instance of the Testing<T> for use in Unit Testing.
            /// </summary>
            /// <param name="testObject">This is the object we are testing. Methods usually use this as an argument</param>
            /// <param name="error">The error is set to null if a what we are testing is valid, otherwise error is reassigned depending on what is thrown</param>
            public Testing(T testObject, string error = null)
            {
                this.testObject = testObject;
                this.error = error;
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that valid file extensions work.
        /// </summary>
        public void TestVerifyFileExtensionsValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);

            List<Testing<string[]>> testCasesValid = new List<Testing<string[]>>()
            {
                new Testing<string[]> (new string[] { "file.json", "file.yml", "../../../output", "log4net.config" })
            };
            foreach (Testing<string[]> testCase in testCasesValid)
            {
                try
                {
                    up.verifyFileExtensions(testCase.testObject);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that invalid file extensions are handled.
        /// </summary>
        public void TestVerifyFileExtensionsInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);

            List<Testing<string[]>> testCasesInvalid = new List<Testing<string[]>>()
            {
                new Testing <string[]> (new string[] {}, "Missing 4 input files."),
                new Testing <string[]> (new string[] { "file.json" }, "Missing 3 input files."),
                new Testing <string[]> (new string[] { "file.json", "file.yml" }, "Missing 2 input files."),
                new Testing <string[]> (new string[] { "file.json", "file.yml", "../../../output" }, "Missing 1 input file."),
                new Testing <string[]> (new string[] { "file1.json", "file2.json", "../../../output", "file3.json", "file4.json" }, "Too many input files. Maximum needed is 4."),
                new Testing <string[]> (new string[] { "file.jsn", "file.yml", "../../../output", "log4net.config" }, "The 1st argument is not a .json file."),
                new Testing <string[]> (new string[] { "file.json", "file.yaml", "../../../output", "log4net.config" }, "The 2nd argument is not a .yml file."),
                new Testing <string[]> (new string[] { "file.json", "file.yml", "../../../outp1ut", "log4net.config" }, "The 3rd argument is not a valid path."),
                new Testing <string[]> (new string[] { "file.json", "file.yml", "../../../output", "log4net.json" }, "The 4th argument is not a .config file.")
            };
            foreach (Testing<string[]> testCase in testCasesInvalid)
            {
                try
                {
                    up.verifyFileExtensions(testCase.testObject);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that a valid yaml is able to deserialize properly.
        /// </summary>
        public void TestYamlDeserializationValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml("../../../expected/ExpectedOutput.yml");
            List<KeyVaultProperties> expectedYamlVaults = createExpectedYamlVaults();
            Assert.IsTrue(expectedYamlVaults.SequenceEqual(yamlVaults));
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that the program handles if there are invalid fields
        /// or changes made in the yaml other than those in the AccessPolicies.
        /// </summary>
        public void TestCheckVaultChangesValid()
        {
           UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
           var expectedYamlVaults = createExpectedYamlVaults();
           List<KeyVaultProperties> vaultsRetrieved = createExpectedYamlVaults();
           try
           {
                up.checkVaultChanges(expectedYamlVaults, vaultsRetrieved);
           }
           catch
           {
                Assert.Fail();
           }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that the program handles if there are invalid fields 
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
        /// This method verifies how changes are counted and that the program handles the number of 
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
                Assert.AreEqual(0, up.updateVaults(vaultsRetrieved, vaultsRetrieved, null, null, null).Count);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies how changes are counted and that the program handles the number of 
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
        /// This method verifies that the program handles invalid KeyVaultProperties fields.
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
        /// This method verifies that the program handles invalid PrincipalPermissions fields.
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
        /// This method verifies that the program handles invalid PrincipalPermissions fields.
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
        /// This method verifies that the number of users in a KeyVault's AccessPolicies are being counted properly and 
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

        [TestMethod]
        /// <summary>
        /// This method verifies that the shorthands are translated to their respective 
        /// </summary>
        public void TestTranslateShorthandsValid()
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

        [TestMethod]
        /// <summary>
        /// This method verifies that "all" shorthand cannot be repeated.
        /// </summary>
        public void TestTranslateShorthandInvalid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            List<Testing<string[]>> cases = new List<Testing<string[]>>
            {
                new Testing<string[]>(new string[] { "all ", "all - read" }, "Key 'all' permission is duplicated"),
                new Testing<string[]>(new string[] { "all ", "read" }, "'All' permission removes need for other Key permissions"),
                new Testing<string[]>(new string[] { "read ", "get" }, "get, list permissions are already included in Key 'read' permission"),
                new Testing<string[]>(new string[] { "read - create"}, "Remove values could not be recognized in Key permission 'read - <create>'"),
                new Testing<string[]>(new string[] { "read - list", "get" }, "get permissions are already included in Key 'read' permission"),
            };
            foreach(Testing<string[]> t in cases)
            {
                try
                {
                    up.translateShorthand(t.testObject[0].Substring(0, t.testObject[0].IndexOf(" ")), "Key", t.testObject, t.testObject[0].StartsWith("a") ? Constants.ALL_KEY_PERMISSIONS : Constants.READ_KEY_PERMISSIONS,
                        Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
                    Assert.Fail();
                }
                catch(Exception e)
                {
                    Assert.AreEqual(t.error, e.Message);
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// This method tests valid cases for the translateShorthand() method.
        /// </summary>
        public void TestTranslateShorthandValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            Testing<string[]> case1 = new Testing<string[]>(new string[] { "all - read, write, storage, crypto" }, "purge");
            var res = up.translateShorthand("all", "Key", case1.testObject, Constants.ALL_KEY_PERMISSIONS, Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            Assert.AreEqual(1, res.Length);
            Assert.AreEqual(case1.error, res[0]);
            var vaults = createExpectedYamlVaults();


            //TopSP Policies
            var sps = new List<TopSp>();
            var found = new HashSet<string>();
            var validTypes = new string[] { "user", "application" };
            foreach (KeyVaultProperties kv in vaults)
            {
                foreach(PrincipalPermissions pp in kv.AccessPolicies)
                {
                    if (!validTypes.Contains(pp.Type.ToLower()))
                    {

                    }
                    else if ((pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group") && found.Contains(pp.Alias))
                    {
                        var idx = sps.FindIndex(c => c.alias == pp.Alias);
                        sps[idx].count++;
                    }
                    else if((pp.Type.ToLower() == "application" || pp.Type.ToLower() == "service principal") && found.Contains(pp.DisplayName))
                    {
                        var idx = sps.FindIndex(c => c.name == pp.DisplayName);
                        sps[idx].count++;
                    }
                    else if (pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group")
                    {
                        sps.Add(new TopSp(pp.Type, pp.DisplayName, 1, pp.Alias));
                        found.Add(pp.Alias);
                    }
                    else
                    {
                        sps.Add(new TopSp(pp.Type, pp.DisplayName, 1));
                        found.Add(pp.DisplayName);
                    }
                }
            }
            sps.Sort((a, b) => b.count.CompareTo(a.count));
            foreach(var v in sps)
            {
                Console.WriteLine($"{v.type} {v.name} with alias {v.alias} has {v.count} policies\n");
            }

            //TopSP Permissions
            sps = new List<TopSp>();
            found = new HashSet<string>();
            
            foreach (KeyVaultProperties kv in vaults)
            {
                foreach (PrincipalPermissions pp in kv.AccessPolicies)
                {
                    if (!validTypes.Contains(pp.Type.ToLower()))
                    {

                    }
                    else if ((pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group") && found.Contains(pp.Alias))
                    {
                        var idx = sps.FindIndex(c => c.alias == pp.Alias);
                        sps[idx].count += pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length;
                    }
                    else if ((pp.Type.ToLower() == "application" || pp.Type.ToLower() == "service principal") && found.Contains(pp.DisplayName))
                    {
                        var idx = sps.FindIndex(c => c.name == pp.DisplayName);
                        sps[idx].count += pp.PermissionsToCertificates.Length + pp.PermissionsToKeys.Length + pp.PermissionsToSecrets.Length;
                    }
                    else if (pp.Type.ToLower() == "user" || pp.Type.ToLower() == "group")
                    {
                        sps.Add(new TopSp(pp.Type, pp.DisplayName, 1, pp.Alias));
                        found.Add(pp.Alias);
                    }
                    else
                    {
                        sps.Add(new TopSp(pp.Type, pp.DisplayName, 1));
                        found.Add(pp.DisplayName);
                    }
                }
            }
            sps.Sort((a, b) => b.count.CompareTo(a.count));
            foreach (var v in sps)
            {
                Console.WriteLine($"{v.type} {v.name} with alias {v.alias} has {v.count} permissions\n");
            }
        }

        internal class TopSp
        {
            public string type { get; set; }
            public string name { get; set; }
            public string alias { get; set; }
            public int count { get; set; }
            public TopSp(string type, string name, int count, string alias = "")
            {
                this.type = type;
                this.name = name;
                this.alias = alias;
                this.count = count;
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that the correct permissions are being identified by the shorthand keyword.
        /// </summary>
        public void TestGetShorthandPermissionsValid()
        {
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(true);
            var res = up.getShorthandPermissions("all", "key");
            Assert.IsTrue(Constants.ALL_KEY_PERMISSIONS.SequenceEqual(res));
            

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

        [TestMethod]
        /// <summary>
        /// This method tests valid cases for the verifySecurityPrincipal() method.
        /// </summary>
        public void TestValidVerifySecurityPrincipal()
        {
            var up = new UpdatePoliciesFromYaml(true);
            var yaml = createExpectedYamlVaults();
            List<Testing<Dictionary<string, string>>> list = new List<Testing<Dictionary<string, string>>> { 
                new Testing<Dictionary<string, string>>(up.verifySecurityPrincipal(yaml[0].AccessPolicies[0], "group", new TestGraphClient(new MsalAuthenticationProvider())), "g1"),
                new Testing<Dictionary<string, string>>(up.verifySecurityPrincipal(yaml[0].AccessPolicies[1], "user", new TestGraphClient(new MsalAuthenticationProvider())), "ua"),
                new Testing<Dictionary<string, string>>(up.verifySecurityPrincipal(yaml[0].AccessPolicies[2], "user", new TestGraphClient(new MsalAuthenticationProvider())), "ub"),
                new Testing<Dictionary<string, string>>(up.verifySecurityPrincipal(yaml[1].AccessPolicies[0], "service principal", new TestGraphClient(new MsalAuthenticationProvider())), "SP1"),
                new Testing<Dictionary<string, string>>(up.verifySecurityPrincipal(yaml[2].AccessPolicies[0], "application", new TestGraphClient(new MsalAuthenticationProvider())), "a1")
            };
            
            foreach(Testing<Dictionary<string,string>> t in list)
            {
                Assert.AreEqual(t.testObject["ObjectId"], t.error);
            }
        }

        [TestMethod]
        /// <summary>
        /// This method tests invalid cases for the verifySecurityPrincipal() method.
        /// </summary>
        public void TestInvalidVerifySecurityPrincipal()
        {
            var up = new UpdatePoliciesFromYaml(true);
            var tgc = new TestGraphClient(new MsalAuthenticationProvider());
            var yaml = createExpectedYamlVaults();

            var noAlias = createExpectedYamlVaults()[0].AccessPolicies[1];
            noAlias.Alias = "";
            var badDn = yaml[0].AccessPolicies[1];
            badDn.DisplayName = "user";
            var badAlias = yaml[0].AccessPolicies[2];
            badAlias.Alias = "notexist";

            var noAliasGr = createExpectedYamlVaults()[0].AccessPolicies[0];
            noAliasGr.Alias = "";
            var badDnGr = yaml[0].AccessPolicies[0];
            badDnGr.DisplayName = "group";
            var badAliasGr = createExpectedYamlVaults()[0].AccessPolicies[0];
            badAliasGr.Alias = "notexist";

            var appAlias = yaml[2].AccessPolicies[0];
            appAlias.Alias = "app";
            var badAppName = createExpectedYamlVaults()[2].AccessPolicies[0];
            badAppName.DisplayName = "notexist";

            var spAlias = yaml[1].AccessPolicies[0];
            spAlias.Alias = "sp";
            var badSpName = createExpectedYamlVaults()[1].AccessPolicies[0];
            badSpName.DisplayName = "notexist";

            var badType = createExpectedYamlVaults()[1].AccessPolicies[0];
            badType.Type = "unknown";
            List<Testing<PrincipalPermissions>> list = new List<Testing<PrincipalPermissions>> { 
                new Testing<PrincipalPermissions>(noAlias, "Alias is required for User A. User skipped."),
                new Testing<PrincipalPermissions>(badAlias, "Could not find User with Alias 'notexist'. User skipped."),
                new Testing<PrincipalPermissions>(badDn, "The DisplayName 'user' is incorrect and cannot be recognized. User skipped."),
                new Testing<PrincipalPermissions>(noAliasGr, "Alias is required for g1. Group skipped."),
                new Testing<PrincipalPermissions>(badAliasGr, "Could not find Group with DisplayName 'g1'. Group skipped."),
                new Testing<PrincipalPermissions>(badDnGr, "The DisplayName 'group' is incorrect and cannot be recognized. Group skipped."),
                new Testing<PrincipalPermissions>(appAlias, "The Alias 'app' should not be defined and cannot be recognized for a1. Application skipped."),
                new Testing<PrincipalPermissions>(badAppName, "Could not find Application with DisplayName 'notexist'. Application skipped."),
                new Testing<PrincipalPermissions>(spAlias, "The Alias 'sp' should not be defined and cannot be recognized for SP1. Service Principal skipped."),
                new Testing<PrincipalPermissions>(badSpName, "Could not find ServicePrincipal with DisplayName 'notexist'. Service Principal skipped."),
                new Testing<PrincipalPermissions>(badType, "'unknown' is not a valid type for SP1. Valid types are 'User', 'Group', 'Application', or 'Service Principal'. Skipped!")
            };
            foreach(Testing<PrincipalPermissions> t in list)
            {
                up.verifySecurityPrincipal(t.testObject, t.testObject.Type.ToLower(), tgc);
                Assert.AreEqual(t.error, up.error);
            }
        }

        [TestMethod]
        /// <summary>
        /// This method tests the updateVaults() method. 
        /// </summary>
        public void TestUpdateVaults()
        {
            var up = new UpdatePoliciesFromYaml(true);
            var tgc = new TestGraphClient(new MsalAuthenticationProvider());
            var tkvm = new TestKVMClient();
            var vaultsRetrieved = createExpectedYamlVaults();
            var yamlVaults = createExpectedYamlVaults();
            yamlVaults[0].AccessPolicies[0].PermissionsToKeys = new string[] { "list", "update", "create", "import", "delete", "recover", "backup", "restore" };
            yamlVaults[0].AccessPolicies[0].PermissionsToCertificates = new string[] { "list", "update", "create", "import", "delete", "recover", "backup", "restore", "managecontacts", "manageissuers", "getissuers", "listissuers", "setissuers", "deleteissuers" };
            yamlVaults[0].AccessPolicies[0].PermissionsToSecrets = new string[] { "list", "set", "delete", "recover", "backup", "restore" };
            var dict = new Dictionary<string, string>();
            dict["tenantId"] = "00000000-0000-0000-0000-000000000000";
            var res = up.updateVaults(yamlVaults, vaultsRetrieved, tkvm, dict, tgc);

            Assert.AreEqual(1, res.Count());
            Assert.AreEqual(1, res[0].AccessPolicies.Count);
            Assert.AreEqual("get" , res[0].AccessPolicies[0].PermissionsToKeys[0].ToLower());
            Assert.AreEqual("get", res[0].AccessPolicies[0].PermissionsToSecrets[0].ToLower());
            Assert.AreEqual("get", res[0].AccessPolicies[0].PermissionsToCertificates[0].ToLower());

            var updated = (TestVaults)tkvm.Vaults;
            Assert.AreEqual(1, updated.Updated.Count);
            Assert.AreEqual(4, updated.Updated[0].AccessPolicies.Count());
            Assert.AreEqual(8, updated.Updated[0].AccessPolicies[0].PermissionsToKeys.Length);
            Assert.AreEqual(6, updated.Updated[0].AccessPolicies[0].PermissionsToSecrets.Length);
            Assert.AreEqual(14, updated.Updated[0].AccessPolicies[0].PermissionsToCertificates.Length);
        }

        /// <summary>
        /// This method creates the expected yamlVaults list of KeyVaultProperties from the deserialized yaml.
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

