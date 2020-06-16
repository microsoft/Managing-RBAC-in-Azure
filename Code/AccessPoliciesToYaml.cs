using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest.Azure;
using Microsoft.Graph;
using System.Management.Automation;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using Namotion.Reflection;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace RBAC
{
    /// <summary>
    /// "Phase 1" Code that serializes a list of Key Vaults into Yaml.
    /// </summary>
    class AccessPoliciesToYaml
    {

        /// <summary>
        /// This method verifies that the file arguments are of the correct type.
        /// </summary>
        /// <param name="args">The string array of program arguments</param>
        public static void verifyFileExtensions(string[] args)
        {
            try
            {
                if (args.Length != 2)
                {
                    throw new Exception("Missing input file.");
                }
                if (System.IO.Path.GetExtension(args[0]) != ".json")
                {
                    throw new Exception("The 1st argument is not a .json file");
                }
                if (System.IO.Path.GetExtension(args[1]) != ".yml")
                {
                    throw new Exception("The 2nd argument is not a .yml file");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
            }
        }

        /// <summary>
        /// This method reads in and deserializes the Json input file.
        /// </summary>
        /// <param name="jsonDirectory">The Json file path</param>
        /// <returns>A JsonInput object that stores the Json input data</returns>
        public static JsonInput readJsonFile(string jsonDirectory)
        {
            try
            {
                string masterConfig = System.IO.File.ReadAllText(jsonDirectory);
                JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
                
                JObject configVaults = JObject.Parse(masterConfig);
                checkJsonFields(vaultList, configVaults);
                checkMissingAadFields(vaultList, configVaults);
                checkMissingResourceFields(vaultList, configVaults);

                return vaultList; 
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
                return null;
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist within the Json file.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        private static void checkJsonFields(JsonInput vaultList, JObject configVaults)
        {
            List<string> missingInputs = new List<string>();
            if (vaultList.AadAppKeyDetails == null)
            {
                missingInputs.Add("AadAppKeyDetails");
            }
            if (vaultList.Resources == null)
            {
                missingInputs.Add("Resources");
            }

            int numMissing = missingInputs.Count();
            int numValid = 2 - numMissing;

            if (missingInputs.Count() == 0 && configVaults.Children().Count() != 2)
            {
                throw new Exception($"Invalid fields in Json were defined. Valid fields are 'AadAppKeyDetails' and 'Resources'.");
            }
            else if (missingInputs.Count() != 0 && configVaults.Children().Count() != numValid)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} in Json. Invalid fields were defined; " +
                    $"valid fields are 'AadAppKeyDetails' and 'Resources'.");
            }
            else if (missingInputs.Count() > 0)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} in Json.");
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist for the AadAppKeyDetails object.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        private static void checkMissingAadFields(JsonInput vaultList, JObject configVaults)
        {
            List<string> missingInputs = new List<string>();
            if (vaultList.AadAppKeyDetails.AadAppName == null)
            {
                missingInputs.Add("AadAppName");
            }
            if (vaultList.AadAppKeyDetails.VaultName == null)
            {
                missingInputs.Add("VaultName");
            }
            if (vaultList.AadAppKeyDetails.ClientIdSecretName == null)
            {
                missingInputs.Add("ClientIdSecretName");
            }
            if (vaultList.AadAppKeyDetails.ClientKeySecretName == null)
            {
                missingInputs.Add("ClientKeySecretName");
            }
            if (vaultList.AadAppKeyDetails.TenantIdSecretName == null)
            {
                missingInputs.Add("TenantIdSecretName");
            }

            int numMissing = missingInputs.Count();
            JToken aadDetails = configVaults.SelectToken($".AadAppKeyDetails");
            int numValid = 5 - numMissing;

            if (numMissing == 0 && (aadDetails.Children().Count() != 5))
            {
                throw new Exception($"Invalid fields for AadAppKeyDetails were defined. " +
                    $"Valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.");
            }
            else if (numMissing != 0 && aadDetails.Children().Count() != numValid)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} for AadAppKeyDetails. Invalid fields were defined; " +
                    $"valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.");
            }
            else if (numMissing > 0)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} for AadAppKeyDetails.");
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist for each Resource object.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        private static void checkMissingResourceFields(JsonInput vaultList, JObject configVaults)
        {
            JEnumerable<JToken> resourceList = configVaults.SelectToken($".Resources").Children();
                
            int i = 0;
            foreach (Resource res in vaultList.Resources)
            {
                JToken jres = resourceList.ElementAt(i);

                if (res.SubscriptionId != null && res.ResourceGroups == null && jres.Children().Count() > 1)
                {
                    throw new Exception($"Invalid fields for Resource with SubscriptionId '{res.SubscriptionId}' were defined. Valid fields are 'SubscriptionId' and 'ResourceGroups'.");
                }
                else if (res.SubscriptionId == null && jres.Children().Count() > 0)
                {
                    throw new Exception($"Missing 'SubscriptionId' for Resource. Invalid fields were defined; valid fields are 'SubscriptionId' and 'ResourceGroups'.");
                }
                else if (res.SubscriptionId != null && res.ResourceGroups != null)
                {
                    if (jres.Children().Count() > 2)
                    {
                        throw new Exception($"Invalid fields other than 'SubscriptionId' and 'ResourceGroups' were defined for Resource with SubscriptionId '{res.SubscriptionId}'.");
                    }

                    int j = 0;
                    foreach (ResourceGroup resGroup in res.ResourceGroups)
                    {
                        JEnumerable<JToken> groupList = jres.SelectToken($".ResourceGroups").Children();
                        JToken jresGroup = groupList.ElementAt(j);

                        if (resGroup.ResourceGroupName != null && resGroup.KeyVaults == null && jresGroup.Children().Count() > 1)
                        {
                            throw new Exception($"Invalid fields for ResourceGroup with ResourceGroupName '{resGroup.ResourceGroupName}' were defined. " +
                                $"Valid fields are 'ResourceGroupName' and 'KeyVaults'.");
                        }
                        else if (resGroup.ResourceGroupName == null && jresGroup.Children().Count() > 0)
                        {
                            throw new Exception("Missing 'ResourceGroupName' for ResourceGroup. Invalid fields were defined; valid fields are 'ResourceGroupName' and 'KeyVaults'.");
                        }
                        else if (resGroup.ResourceGroupName != null && resGroup.KeyVaults != null && jresGroup.Children().Count() > 2)
                        {
                            throw new Exception($"Invalid fields other than 'ResourceGroupName' and 'KeyVaults' were defined for ResourceGroup " +
                                $"with ResourceGroupName '{resGroup.ResourceGroupName}'.");
                        }
                        ++j;
                    }
                }
                ++i;
            }
        }

        /// <summary>
        /// This method retrieves the AadAppSecrets using a SecretClient and returns a Dictionary of the secrets.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtaind from MasterConfig.json file</param>
        /// <returns>The dictionary of secrets obtained from the SecretClient</returns>
        public static Dictionary<string, string> getSecrets(JsonInput vaultList)
        {
            Dictionary<string, string> secrets = new Dictionary<string, string>();
            try
            {
                secrets["appName"] = vaultList.AadAppKeyDetails.AadAppName;

                // Creates the SecretClient and grabs secrets
                string keyVaultName = vaultList.AadAppKeyDetails.VaultName;
                string keyVaultUri = Constants.HTTP + keyVaultName + Constants.AZURE_URL;
            
                SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

                try
                {
                    KeyVaultSecret clientIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientIdSecretName);
                    secrets["clientId"] = clientIdSecret.Value;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        Console.WriteLine($"\nError: clientIdSecret could not be found.");
                    }
                    else
                    {
                        Console.WriteLine($"\nError: clientIdSecret {e.Message}.");
                    }
                    System.Environment.Exit(1);
                }
                try
                {
                    KeyVaultSecret clientKeySecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientKeySecretName);
                    secrets["clientKey"] = clientKeySecret.Value;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        Console.WriteLine($"\nError: clientKeySecret could not be found.");
                    }
                    else
                    {
                        Console.WriteLine($"\nError: clientKeySecret {e.Message}.");
                    }
                    System.Environment.Exit(1);
                }
                try
                {
                    KeyVaultSecret tenantIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.TenantIdSecretName);
                    secrets["tenantId"] = tenantIdSecret.Value;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        Console.WriteLine($"\nError: tenantIdSecret could not be found.");
                    }
                    else
                    {
                        Console.WriteLine($"\nError: tenantIdSecret {e.Message}.");
                    }
                    System.Environment.Exit(1);
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
            }
            return secrets;
        }

        /// <summary>
        /// This method creates and returns a KeyVaulManagementClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The KeyVaultManagementClient created using the secret information</returns>
        public static Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient createKVMClient(Dictionary<string, string> secrets)
        {
            try
            {
                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(secrets["clientId"], 
                    secrets["clientKey"], secrets["tenantId"], AzureEnvironment.AzureGlobalCloud);
                return (new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient(credentials));
            } 
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
                return null;
            }
        }

        /// <summary>
        /// This method creates and returns a GraphServiceClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The GraphServiceClient created using the secret information</returns>
        public static GraphServiceClient createGraphClient(Dictionary<string, string> secrets)
        {
            try
            {
                string auth = Constants.MICROSOFT_LOGIN + secrets["tenantId"];
                string redirectUri = Constants.HTTP + secrets["appName"];

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(secrets["clientId"])
                                                              .WithAuthority(auth)
                                                              .WithRedirectUri(redirectUri)
                                                              .WithClientSecret(secrets["clientKey"])
                                                              .Build();

                List<string> scopes = new List<string>()
                {
                    Constants.GRAPHCLIENT_URL
                };
                MsalAuthenticationProvider authProvider = new MsalAuthenticationProvider(cca, scopes.ToArray());
                return (new GraphServiceClient(authProvider));
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
                return null;
            }
        }

        /// <summary>
        /// This method retrieves each of the KeyVaults specified in the vaultList.
        /// </summary>
        /// <param name="vaultList">The data obtained from deserializing json file</param>
        /// <param name="kvmClient">The KeyVaultManagementClient containing Vaults</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient for obtaining display names</param>
        /// <returns>The list of KeyVaultProperties containing the properties of each KeyVault</returns>
        public static List<KeyVaultProperties> getVaults(JsonInput vaultList, 
            Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, GraphServiceClient graphClient)
        {
            List<Vault> vaultsRetrieved = new List<Vault>();
            foreach (Resource res in vaultList.Resources)
            {
                // Associates the client with the subscription
                kvmClient.SubscriptionId = res.SubscriptionId;
               
                // Retrieves all KeyVaults at the Subscription scope
                if (res.ResourceGroups.Count == 0)
                { 
                    vaultsRetrieved = getVaultsAllPages(kvmClient, vaultsRetrieved);
                }
                else
                {
                    bool notFound = false;
                    foreach (ResourceGroup resGroup in res.ResourceGroups) 
                    {
                        // If the Subscription is not found, then do not continue looking for vaults in this subscription
                        if (notFound)
                        {
                            break;
                        }

                        // Retrieves all KeyVaults at the ResourceGroup scope
                        if (resGroup.KeyVaults.Count == 0) 
                        { 
                            vaultsRetrieved = getVaultsAllPages(kvmClient, vaultsRetrieved, resGroup.ResourceGroupName);
                        }
                        // Retrieves all KeyVaults at the Resource scope
                        else
                        {
                            foreach (string vaultName in resGroup.KeyVaults) 
                            {
                                try
                                {
                                    vaultsRetrieved.Add(kvmClient.Vaults.Get(resGroup.ResourceGroupName, vaultName)); 
                                } 
                                catch (CloudException e)
                                {
                                    Console.WriteLine($"\nError: {e.Message}");
                                    // If the Subscription is not found, then do not continue looking for vaults in this subscription
                                    if (e.Body.Code == "SubscriptionNotFound")
                                    {
                                        notFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<KeyVaultProperties> keyVaultsRetrieved = new List<KeyVaultProperties>();
            foreach (Vault curVault in vaultsRetrieved) 
            {
                keyVaultsRetrieved.Add(new KeyVaultProperties(curVault, graphClient));
            }
            return keyVaultsRetrieved;
        }

        /// <summary>
        /// This method retrieves the KeyVaults from all the pages of KeyVaults as one page can only store a limited number of KeyVaults.
        /// </summary>
        /// <param name="kvmClient">The KeyVaultManagementClient</param>
        /// <param name="vaultsRetrieved">The list of Vault objects to add to</param>
        /// <param name="resourceGroup">The ResourceGroup name(if applicable). Default is null.</param>
        /// <returns>The updated vaultsRetrieved list</returns>
        public static List<Vault> getVaultsAllPages(Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, 
            List<Vault> vaultsRetrieved, string resourceGroup = "")
        {
            IPage<Vault> vaultsCurPg = null;
            // Retrieves the first page of KeyVaults at the Subscription scope
            if (resourceGroup.Length == 0) 
            { 
                try
                {
                    vaultsCurPg = kvmClient.Vaults.ListBySubscription();
                }
                catch (CloudException e)
                {
                    Console.WriteLine($"\nError: {e.Message}");
                }
            }
            // Retrieves the first page of KeyVaults at the ResourceGroup scope
            else
            { 
                try
                {
                    vaultsCurPg = kvmClient.Vaults.ListByResourceGroup(resourceGroup);
                }
                catch (CloudException e)
                {
                    Console.WriteLine($"\nError: {e.Message}");
                }
            }
            
            // Get remaining pages if vaults were found
            if (vaultsCurPg != null)
            {
                vaultsRetrieved.AddRange(vaultsCurPg);
                while (vaultsCurPg.NextPageLink != null)
                {
                    IPage<Vault> vaultsNextPg = null;
                    // Retrieves the remaining pages of KeyVaults at the Subscription scope
                    if (resourceGroup.Length == 0) // then by Subscription
                    {
                        vaultsNextPg = kvmClient.Vaults.ListBySubscriptionNext(vaultsCurPg.NextPageLink);
                    }
                    // Retrieves the remaining pages of KeyVaults at the ResourceGroup scope
                    else
                    {
                        vaultsNextPg = kvmClient.Vaults.ListByResourceGroupNext(vaultsCurPg.NextPageLink);
                    }
                    vaultsRetrieved.AddRange(vaultsNextPg);
                    vaultsCurPg = vaultsNextPg;
                }
            }
            return vaultsRetrieved;
        }

        /// <summary>
        /// This method serializes the list of Vault objects and outputs the YAML.
        /// </summary>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties to serialize</param>
        /// <param name="yamlDirectory"> The directory of the outputted yaml file </param>
        public static void convertToYaml(List<KeyVaultProperties> vaultsRetrieved, string yamlDirectory)
        {
            try
            {
                var serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(vaultsRetrieved);

                System.IO.File.WriteAllText(yamlDirectory, yaml);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
            }
        }
    }
}