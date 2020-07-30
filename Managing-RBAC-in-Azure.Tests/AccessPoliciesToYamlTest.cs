using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RBAC
{
    [TestClass]
    /// <summary>
    /// This class is the testing class for AccessPoliciesToYaml.
    /// </summary>
    public class AccessPoliciesToYamlTest
    {
        /// <summary>
        /// This is a wrapper class that is used for testing purposes.
        /// </summary>
        public class Testing <T>
        {
            public T testObject { get; set; }
            public string error { get; set; }

            /// <summary>
            /// Constructor to create an instance of the Testing<T> for use in Unit Testing.
            /// </summary>
            /// <param name="testObject">This is the object we are testing. Methods usually use this as an argument</param>
            /// <param name="error">The error is set to null if a what we are testing is valid, otherwise error is reassigned depending on what is thrown</param>
            public Testing (T testObject, string error = null)
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
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);

            List<Testing<string[]>> testCasesValid = new List<Testing<string[]>>()
            {
                new Testing<string[]> (new string[] { "file.json", "../../../output" })
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
        /// This method verifies that invalid file extensions are handled.
        /// </summary>
        public void TestVerifyFileExtensionsInvalid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);

            List<Testing<string[]>> testCasesInvalid = new List<Testing<string[]>>()
            {
                new Testing <string[]> (new string[] {}, "Missing 2 input files."),
                new Testing <string[]> (new string[] { "file.json" }, "Missing 1 input file."),
                new Testing <string[]> (new string[] { "file1.json", "file2.yml", "../../../output" }, "Too many input files. Maximum needed is 2."),
                new Testing <string[]> (new string[] { "file.jsn", "../../../output" }, "The 1st argument is not a .json file."),
                new Testing <string[]> (new string[] { "file.json", "../../../outp1ut" }, "The 2nd argument is not a valid path."),
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
        /// This method verifies that a valid Json file is consistent with an expected json file.
        /// </summary>
        public void TestReadJsonFileValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var json = ap.readJsonFile("../../../Input/MasterConfig.json");
            var exp = createExpectedJson(new List<Resource>());
            Assert.IsTrue(exp.Equals(json));
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that reading in valid Json fields work.
        /// </summary>
        public void TestCheckJsonFieldsValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../Input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);
            List<Testing<JsonInput>> testCasesJsonFieldsValid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson( new List<Resource>()))
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
        /// This method verifies that reading in invalid Json fields are handled (checks if AppKeyDetails or Resources is null).
        /// </summary>
        public void TestCheckJsonFieldsInvalid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);;
            string masterConfig = System.IO.File.ReadAllText("../../../Input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);

            List<Testing<JsonInput>> testCasesJsonFieldsInvalid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson( null),
                "Missing Resources in Json. Invalid fields were defined; Only valid field is 'Resources'.")
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
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that reading in valid Resource fields work.
        /// </summary>
        public void TestCheckMissingResourceFieldsValid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../Input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);

            List<Testing<JsonInput>> testMissingResourcesFieldsValid = new List<Testing<JsonInput>>()
            {
                new Testing <JsonInput> (createExpectedJson(new List<Resource>()))
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
        /// This method verifies that reading invalid Resource Fields are handled.
        /// </summary>
        public void TestCheckMissingResourceFieldsInvalid()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../Input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);

            JsonInput missingResourceGroupName = createExpectedJson(new List<Resource>());
            missingResourceGroupName.Resources[0].ResourceGroups[0].ResourceGroupName = null;
            JsonInput missingSubscriptionId = createExpectedJson(new List<Resource>());
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

        [TestMethod]
        /// <summary>
        /// This method tests the getVaults() method.
        /// </summary>
        public void TestGetVaults()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var json = ap.readJsonFile("../../../Input/TestActualVaults.json");
            var ret = ap.getVaults(json, new TestKVMClient(), new TestGraphClient(new MsalAuthenticationProvider()));
            Assert.AreEqual(4, ret.Count);

            json.Resources[0].ResourceGroups.Add(new ResourceGroup
            {
                ResourceGroupName = "RG1",
                KeyVaults = new string[] { "RG1Test1" }.ToList()
            }) ;
            json.Resources[0].ResourceGroups.Add(new ResourceGroup
            {
                ResourceGroupName = "RG2"
            });
            ret = ap.getVaults(json, new TestKVMClient(), new TestGraphClient(new MsalAuthenticationProvider()));
            Assert.AreEqual(3, ret.Count);
        }

        [TestMethod]
        /// <summary>
        /// This method tests the convertToYaml() method.
        /// </summary>
        public void TestConvertToYaml()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var vaults = UpdatePoliciesFromYamlTest.createExpectedYamlVaults();
            ap.convertToYaml(vaults, "../../../output/ActualOutput.yml");
            Assert.AreEqual(System.IO.File.ReadAllText("../../../output/ActualOutput.yml"), System.IO.File.ReadAllText("../../../output/ActualOutput.yml"));
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
        private JsonInput createExpectedJson( List<Resource> resources)
        {
            var expectedJson = new JsonInput();
            expectedJson.Resources = resources;


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

