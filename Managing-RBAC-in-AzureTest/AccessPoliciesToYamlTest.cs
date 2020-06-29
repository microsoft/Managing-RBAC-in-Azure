using Microsoft.Azure.Management.Storage.Fluent.Models;
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
        public class MainArgs
        {
            public string[] extensions { get; set; }
            public string error { get; set; }

            public MainArgs(string[] extensions, string error = null)
            {
                this.extensions = extensions;
                this.error = error;
            }

        }
        [TestMethod]
        /// <summary>
        /// This method verifies that the negative/invalid Main Args are handeled
        /// </summary>
        public void TestVerifyFileExtensionsNegative()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            List<MainArgs> negativeTestCases = new List<MainArgs>()
            {
                new MainArgs(new string[] {}, "Missing 2 input files."),
                new MainArgs(new string[] { "file.json" }, "Missing 1 input file."),
                new MainArgs(new string[] { "file1.json", "file2.json", "yaml.json" }, "Too many input files. Maximum needed is 2."),
                new MainArgs(new string[] { "file.jsn", "file.yml" }, "The 1st argument is not a .json file."),
                new MainArgs(new string[] { "file.json", "file.yaml" }, "The 2nd argument is not a .yml file.")
            };

            
            foreach (MainArgs args in negativeTestCases)
            {
                try
                {
                    ap.verifyFileExtensions(args.extensions);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(args.error, e.Message);
                }
            }
        }
        [TestMethod]
         /// <summary>
        /// This method verifies that the the positive Main Args work
        /// </summary>
        public void TestVerifyFileExtensionsPositive()
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(true);
            List<MainArgs> positiveTestCases = new List<MainArgs>()
            {
                new MainArgs(new string[] { "file.json", "file.yml" })


            };


            foreach (MainArgs args in positiveTestCases)
            {
                try
                {
                    ap.verifyFileExtensions(args.extensions);
                }
                catch
                {
                    Assert.Fail();
                }
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


        /// <summary>
        /// This method creates an expected json that is used for testing purposes.
        /// </summary>
        /// <returns>The expected deserialized JsonInput</returns>
        private JsonInput createExpectedJson()
        {
            var exp = new JsonInput();
            exp.AadAppKeyDetails = new AadAppKey();
            exp.Resources = new List<Resource>();
            exp.AadAppKeyDetails.AadAppName = "AppName";
            exp.AadAppKeyDetails.ClientIdSecretName = "ClientId";
            exp.AadAppKeyDetails.VaultName = "KeyVault";
            exp.AadAppKeyDetails.ClientKeySecretName = "ClientKey";
            exp.AadAppKeyDetails.TenantIdSecretName = "TenantId";

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

