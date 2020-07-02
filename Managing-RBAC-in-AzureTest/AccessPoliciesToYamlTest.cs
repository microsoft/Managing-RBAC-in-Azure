using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NSubstitute.Routing.AutoValues;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RBAC
{
    [TestClass]
    public class AccessPoliciesToYamlTest
    {
        public class Testing <T>
        {
            public T testObject { get; set; }
            public string error { get; set; }

            public Testing (T testObject, string error = null)
            {
                this.testObject = testObject;
                this.error = error;
            }

        }

        [TestMethod]
        /// <summary>
        /// Verifies that reading in valid Main args work
        /// </summary>
        public void TestVerifyFileExtensionsValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            List<Testing<string[]>> testCasesValid = new List<Testing<string[]>>()
            {
                new Testing<string[]> (new string[] { "file.json", "file.yml" })
            };

            foreach (Testing<string[]> testCase in testCasesValid)
            {
                try
                {
                    ap.verifyFileExtensions(testCase.testObject);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }


        [TestMethod]
        /// <summary>
        /// Verifies that reading in invalid Main args are handled
        /// </summary>
        public void TestVerifyFileExtensionsInvalid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            List<Testing<string[]>> testCasesInvalid = new List<Testing<string[]>>()
            {
                new Testing <string[]> (new string[] {}, "Missing 2 input files."),
                new Testing <string[]> (new string[] { "file.json" }, "Missing 1 input file."),
                new Testing <string[]> (new string[] { "file1.json", "file2.json", "yaml.json" }, "Too many input files. Maximum needed is 2."),
                new Testing <string[]> (new string[] { "file.jsn", "file.yml" }, "The 1st argument is not a .json file."),
                new Testing <string[]> (new string[] { "file.json", "file.yaml" }, "The 2nd argument is not a .yml file.")
            };
       
            foreach (Testing<string[]> testCase in testCasesInvalid)
            {
                try
                {
                    ap.verifyFileExtensions(testCase.testObject);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// Verifies that a valid Json file is consistent with an expected json file
        /// </summary>
        public void TestReadJsonFileValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var json = ap.readJsonFile("../../../input/MasterConfig.json");
            var exp = createExpectedJson(new AadAppKey(), new List<Resource>(), "AppName", "KeyVault", "ClientId", "ClientKey", "TenantId");

            Assert.IsTrue(exp.Equals(json));
        }

        [TestMethod]
        /// <summary>
        /// Verifies that reading in valid Json fields work
        /// </summary>
        public void TestCheckJsonFieldsValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);

            List<Testing<JsonInput>> testCasesJsonFieldsValid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson( new AadAppKey(), new List<Resource>(), "AppName", "KeyVault", "ClientId", "ClientKey", "TenantId"))
            };

            foreach (Testing<JsonInput> testCase in testCasesJsonFieldsValid)
            {
                try
                {
                    ap.checkJsonFields(testCase.testObject, configVaults);

                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// Verifies that reading in invalid Json fields are handled (checks if AppKeyDetails or Resources is null)
        /// </summary>
        public void TestCheckJsonFieldsInvalid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);;
            string masterConfig = System.IO.File.ReadAllText("../../../input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);

            List<Testing<JsonInput>> testCasesJsonFieldsInvalid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson( null, new List<Resource>(), "AppName", "KeyVault", "ClientId", "ClientKey", "TenantId"),
                "Missing AadAppKeyDetails in Json. Invalid fields were defined; valid fields are 'AadAppKeyDetails' and 'Resources'."),
                new Testing <JsonInput> (createExpectedJson( new AadAppKey() , null, "AppName", "KeyVault", "ClientId","ClientKey", "TenantId"),
                "Missing Resources in Json. Invalid fields were defined; valid fields are 'AadAppKeyDetails' and 'Resources'.")
            };

            foreach (Testing<JsonInput> testCase in testCasesJsonFieldsInvalid)
            {
                try
                {
                    ap.checkJsonFields(testCase.testObject, configVaults);
                    Assert.Fail();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }

            List<Testing<JsonInput>> testCasesAadFieldsInvalid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson(new AadAppKey(), new List<Resource>(), null, "KeyVault", "ClientId", "ClientKey", "TenantId"),
                "Missing AadAppName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'."),
                new Testing <JsonInput> (createExpectedJson(new AadAppKey(), new List<Resource>(), "Appname", null, "ClientId", "ClientKey", "TenantId"),
                "Missing VaultName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'."),
                new Testing <JsonInput> (createExpectedJson(new AadAppKey(), new List<Resource>(), "Appname", "KeyVault", null, "ClientKey", "TenantId"),
                "Missing ClientIdSecretName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'."),
                new Testing <JsonInput> (createExpectedJson(new AadAppKey(), new List<Resource>(), "Appname", "KeyVault", "ClientId" , null, "TenantId"),
                "Missing ClientKeySecretName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'."),
                new Testing <JsonInput> (createExpectedJson(new AadAppKey(), new List<Resource>(), "Appname", "KeyVault", "ClientId" , "ClientKey", null),
                "Missing TenantIdSecretName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.")
            };

            foreach (Testing<JsonInput> testCase in testCasesAadFieldsInvalid)
            {
                try
                {
                    ap.checkMissingAadFields(testCase.testObject, configVaults);
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
        /// Verifies that reading in valid Resource fields work
        /// </summary>
        public void TestCheckMissingResourceFieldsValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);

            List<Testing<JsonInput>> testMissingResourcesFieldsValid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson( new AadAppKey(), new List<Resource>(), "AppName", "KeyVault", "ClientId", "ClientKey", "TenantId"))
            };

            foreach (Testing<JsonInput> testCase in testMissingResourcesFieldsValid)
            {
                try
                {
                    ap.checkMissingResourceFields(testCase.testObject, configVaults);

                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod]
        /// <summary>
        /// Verifies that reading invalid Resource Fields are handled
        /// </summary>
        public void TestCheckMissingResourceFieldsInvalid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);


            JsonInput missingResourceGroupName = createExpectedJson(new AadAppKey(), new List<Resource>(), "AppName", "KeyVault", "ClientId", "ClientKey", "TenantId");
            missingResourceGroupName.Resources[0].ResourceGroups[0].ResourceGroupName = null;
            JsonInput missingSubscriptionId = createExpectedJson(new AadAppKey(), new List<Resource>(), "AppName", "KeyVault", "ClientId", "ClientKeye", "TenantId");
            missingSubscriptionId.Resources[1].SubscriptionId = null;

            List<Testing<JsonInput>> negativeTestMissingResourceFields = new List<Testing<JsonInput>>()
            {
                new Testing<JsonInput>(missingResourceGroupName, "Missing 'ResourceGroupName' for ResourceGroup. Invalid fields were defined; valid fields are 'ResourceGroupName' and 'KeyVaults'."),
                new Testing<JsonInput>(missingSubscriptionId, "Missing 'SubscriptionId' for Resource. Invalid fields were defined; valid fields are 'SubscriptionId' and 'ResourceGroups'.")

            };

            foreach (Testing<JsonInput> testCase in negativeTestMissingResourceFields)
            {
                try
                {
                    ap.checkMissingResourceFields(testCase.testObject, configVaults);
                    Assert.Fail();

                }
                catch (Exception e)
                {
                    Assert.AreEqual(testCase.error, e.Message);
                }
            }
        }

        /// <summary>
        /// This method creates an expected json that is used for testing purposes.
        /// </summary>
        /// <param name="aadAppKeyDetails"> An AadAppKey </param>
        /// <param name="resources"> List of Resources </param>
        /// <param name="aadAppName"> aadAppName </param>
        /// <param name="vaultName"> A keyVault name </param>
        /// <param name="clientIdSecretName"> ClientIdSecretName </param>
        /// <param name="clientKeySecretName"> ClientKeySecretName </param>
        /// <param name="tenantIdSecretName"> TenantIdSecretName </param>
        /// <returns>The expected deserialized JsonInput</returns>
        private JsonInput createExpectedJson( AadAppKey aadAppKeyDetails, List<Resource> resources, string aadAppName,
            string vaultName, string clientIdSecretName, string clientKeySecretName, string tenantIdSecretName)
        {
            var expectedJson = new JsonInput();
            expectedJson.AadAppKeyDetails = aadAppKeyDetails;
            expectedJson.Resources = resources;

            if (aadAppKeyDetails != null)
            {
                expectedJson.AadAppKeyDetails.AadAppName = aadAppName;
                expectedJson.AadAppKeyDetails.VaultName = vaultName;
                expectedJson.AadAppKeyDetails.ClientIdSecretName = clientIdSecretName;
                expectedJson.AadAppKeyDetails.ClientKeySecretName = clientKeySecretName;
                expectedJson.AadAppKeyDetails.TenantIdSecretName = tenantIdSecretName;

            }

            if ( resources != null)
            {
                var res1 = new Resource();
                res1.SubscriptionId = "sample1";
                var g1 = new ResourceGroup();
                g1.ResourceGroupName = "group a";
                g1.KeyVaults.Add("VaultA");
                var g2 = new ResourceGroup();
                g2.ResourceGroupName = "group b";
                g2.KeyVaults.Add("VaultB");
                res1.ResourceGroups.Add(g1);
                res1.ResourceGroups.Add(g2);
                expectedJson.Resources.Add(res1);

                var res2 = new Resource();
                res2.SubscriptionId = "sample2";
                expectedJson.Resources.Add(res2);

                var res3 = new Resource();
                res3.SubscriptionId = "sample3";
                g1 = new ResourceGroup();
                g1.ResourceGroupName = "RBACTest3";
                g1.KeyVaults.Add("RBACTestVault1");
                g1.KeyVaults.Add("RBACTestVault2");
                res3.ResourceGroups.Add(g1);
                expectedJson.Resources.Add(res3);
            }
            return expectedJson;
        }
    }
}

