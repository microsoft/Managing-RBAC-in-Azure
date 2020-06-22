using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RBAC
{
    [TestClass]
    public class AccessPoliciesToYamlTest
    {
        [TestMethod]
        /// <summary>
        /// This method verifies that the the file arguments are of the correct type.
        /// </summary>
        public void TestVerifyFileExtensions()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string[] args = { "file.json", "file.yml" };
            try
            {
                ap.verifyFileExtensions(args);
            }
            catch
            {
                Assert.Fail();
            }

            string[] invalidLength = { "file.json" };
            try
            {
                ap.verifyFileExtensions(invalidLength);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual(e.Message, "Error: Missing input file.");
            }

            string[] invalidJson = { "file.jsn", "file.yml" };
            try
            {
                ap.verifyFileExtensions(invalidJson);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Error: The 1st argument is not a .json file");
            }

            string[] invalidYml = { "file.json", "file.yaml" };
            try
            {
                ap.verifyFileExtensions(invalidYml);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Error: The 2nd argument is not a .yml file");
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that the read json file is consistent with an expected json file.
        /// </summary>
        public void TestReadJsonFile()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var json = ap.readJsonFile("../../../input/MasterConfig.json");
            var exp = createExpectedJson();
            
            Assert.IsTrue(exp.Equals(json));
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that invalid json fields are handled in AadAppKeyDetails & Resources (not inside).
        /// </summary>
        public void TestCheckJsonFields()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var complete = createExpectedJson();
            string masterConfig = System.IO.File.ReadAllText("../../../input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);
            try
            {
                ap.checkJsonFields(complete, configVaults);
            }
            catch
            {
                Assert.Fail();
            }

            var missingAad = complete;
            missingAad.AadAppKeyDetails = null;
            try
            {
                ap.checkJsonFields(missingAad, configVaults);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Missing AadAppKeyDetails in Json. Invalid fields were defined; valid fields are 'AadAppKeyDetails' and 'Resources'.", e.Message);
            }

            var missingAadName = createExpectedJson();
            missingAadName.AadAppKeyDetails.AadAppName = null;
            try
            {
                ap.checkMissingAadFields(missingAadName, configVaults);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Missing AadAppName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.", e.Message);
            }

            var missingVaultName = createExpectedJson();
            missingVaultName.AadAppKeyDetails.VaultName = null;
            try
            {
                ap.checkMissingAadFields(missingVaultName, configVaults);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Missing VaultName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.", e.Message);
            }

            var missingAadKey = createExpectedJson();
            missingAadKey.AadAppKeyDetails.ClientKeySecretName = null;
            try
            {
                ap.checkMissingAadFields(missingAadKey, configVaults);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Missing ClientKeySecretName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.", e.Message);
            }

            var missingAadId = createExpectedJson();
            missingAadId.AadAppKeyDetails.ClientIdSecretName = null;
            try
            {
                ap.checkMissingAadFields(missingAadId, configVaults);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Missing ClientIdSecretName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.", e.Message);
            }

            var missingAadTenant = createExpectedJson();
            missingAadTenant.AadAppKeyDetails.TenantIdSecretName = null;
            try
            {
                ap.checkMissingAadFields(missingAadTenant, configVaults);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Missing TenantIdSecretName for AadAppKeyDetails. Invalid fields were defined; valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.", e.Message);
            }

            var missingRes = createExpectedJson();
            missingRes.Resources = null;
            try
            {
                ap.checkJsonFields(missingRes, configVaults);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreEqual("Missing Resources in Json. Invalid fields were defined; valid fields are 'AadAppKeyDetails' and 'Resources'.", e.Message);
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that incorrect Resource fields are handled.
        /// </summary>
        public void TestCheckMissingResourceFields()
        {
            var completeJson = createExpectedJson();
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            string masterConfig = System.IO.File.ReadAllText("../../../input/MasterConfig.json");
            JObject configVaults = JObject.Parse(masterConfig);
            try
            {
                ap.checkMissingResourceFields(completeJson, configVaults);
            }
            catch
            {
                Assert.Fail();
            }

            completeJson = createExpectedJson();
            completeJson.Resources[0].ResourceGroups[0].ResourceGroupName = null;

            try
            {
                ap.checkMissingResourceFields(completeJson, configVaults);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Missing 'ResourceGroupName' for ResourceGroup. Invalid fields were defined; valid fields are 'ResourceGroupName' and 'KeyVaults'.", e.Message);
            }
            completeJson = createExpectedJson();
            completeJson.Resources[1].SubscriptionId = null;
            try
            {
                ap.checkMissingResourceFields(completeJson, configVaults);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Missing 'SubscriptionId' for Resource. Invalid fields were defined; valid fields are 'SubscriptionId' and 'ResourceGroups'.", e.Message);
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that invalid secrets are handled.
        /// </summary>
        public void TestGetSecrets()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var ret = ap.getSecrets(createExpectedJson());
            Assert.AreEqual(4, ret.Count, $"Expected 4, was {ret.Count}");

            var badVaultName = createExpectedJson();
            badVaultName.AadAppKeyDetails.VaultName = "none";
            try
            {
                ap.getSecrets(badVaultName);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.IsTrue(e.Message != "Assert.Fail failed");
            }

            var badId = createExpectedJson();
            badId.AadAppKeyDetails.ClientIdSecretName = "none";
            try
            {
                ap.getSecrets(badId);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("clientIdSecret could not be found."));
            }

            var badKey = createExpectedJson();
            badKey.AadAppKeyDetails.ClientKeySecretName = "none";
            try
            {
                ap.getSecrets(badKey);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("clientKeySecret could not be found."));
            }

            var badTenant = createExpectedJson();
            badTenant.AadAppKeyDetails.TenantIdSecretName = "none";
            try
            {
                ap.getSecrets(badTenant);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("tenantIdSecret could not be found."));
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that a kvm client is succesfully created.
        /// </summary>
        public void TestCreateKVMClient()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var sec = ap.getSecrets(createExpectedJson());
            var kc = ap.createKVMClient(sec);
            Assert.IsNotNull(kc);
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that a graph client is succesfully created or handled if not.
        /// </summary>
        public void TestCreateGraphClient()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            var sec = ap.getSecrets(createExpectedJson());
            var gc = ap.createGraphClient(sec);
            Assert.IsNotNull(gc);
            sec["clientId"] = null;
            try
            {
                gc = ap.createGraphClient(sec);
                Assert.Fail();
            }
            catch(Exception e)
            {
                Assert.AreEqual("Error: No ClientId was specified.", e.Message);
            }
        }

        [TestMethod]
        /// <summary>
        /// This method verifies that the code's output matches the expected output.
        /// </summary>
        public void TestSuccessfulRun()
        {
            string[] args = { "../../../input/TestActualVaults.json", "../../../output/ActualOutput.yml" };
            AccessPoliciesToYamlProgram.Main(args);
            Assert.AreEqual(System.IO.File.ReadAllText("../../../output/ActualOutput.yml"), System.IO.File.ReadAllText("../../../expected/ExpectedOutput.yml"));
        }

        /// <summary>
        /// This method creates an expected json that is used for testing purposes.
        /// </summary>
        /// <returns>The expected deserialized JsonInput</returns>
        private JsonInput createExpectedJson()
        {
            var exp = new JsonInput();
            exp.AadAppKeyDetails = new AadAppKey();
            exp.Resources = new List<Resource>();
            exp.AadAppKeyDetails.AadAppName = "RBACAutomationApp";
            exp.AadAppKeyDetails.ClientIdSecretName = "RBACClientId";
            exp.AadAppKeyDetails.VaultName = "RBAC-KeyVault";
            exp.AadAppKeyDetails.ClientKeySecretName = "RBACAppKey";
            exp.AadAppKeyDetails.TenantIdSecretName = "RBACTenant";

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
            exp.Resources.Add(res1);

            var res2 = new Resource();
            res2.SubscriptionId = "sample2";
            exp.Resources.Add(res2);

            var res3 = new Resource();
            res3.SubscriptionId = "sample3";
            g1 = new ResourceGroup();
            g1.ResourceGroupName = "RBACTest3";
            g1.KeyVaults.Add("RBACTestVault1");
            g1.KeyVaults.Add("RBACTestVault2");
            res3.ResourceGroups.Add(g1);
            exp.Resources.Add(res3);
            return exp;
        }
    }
}
