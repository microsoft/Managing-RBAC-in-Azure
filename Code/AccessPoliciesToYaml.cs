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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace RBAC
{
    /// <summary>
    /// "Phase 1" Code that serializes a list of Key Vaults into Yaml.
    /// </summary>
    public class AccessPoliciesToYaml
    {
        public AccessPoliciesToYaml(bool testing)
        {
            Testing = testing;
        }
        /// <summary>
        /// This method verifies that the file arguments are of the correct type.
        /// </summary>
        /// <param name="args">The string array of program arguments</param>
        public void verifyFileExtensions(string[] args)
        {
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Logging starting...");
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Verifying file extensions");
            try {
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
            catch(Exception e)
            {
                Exit($"Error: {e.Message}");
            }
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": File extension verified");
            Constants.getLog().Flush();
        }

        /// <summary>
        /// This method reads in and deserializes the Json input file.
        /// </summary>
        /// <param name="jsonDirectory">The Json file path</param>
        /// <returns>A JsonInput object that stores the Json input data</returns>
        public JsonInput readJsonFile(string jsonDirectory)
        {
            try
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Reading json file & checking for invalid entries");
                string masterConfig = System.IO.File.ReadAllText(jsonDirectory);
                JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
                
                JObject configVaults = JObject.Parse(masterConfig);
                checkJsonFields(vaultList, configVaults);
                checkMissingAadFields(vaultList, configVaults);
                checkMissingResourceFields(vaultList, configVaults);
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": json files succesfully checked");
                return vaultList; 
            }
            catch (Exception e)
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $": ERROR: {e.Message}");
                Constants.getLog().Flush();
                Exit($"\nError: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist within the Json file.
        /// </summary>

        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        public void checkJsonFields(JsonInput vaultList, JObject configVaults)
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
        public void checkMissingAadFields(JsonInput vaultList, JObject configVaults)
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
        public void checkMissingResourceFields(JsonInput vaultList, JObject configVaults)
        {
            JEnumerable<JToken> resourceList = configVaults.SelectToken($".Resources").Children();
                
            int i = 0;
            foreach (Resource res in vaultList.Resources)
            {
                JToken jres = resourceList.ElementAt(i);

                if (res.SubscriptionId != null && res.ResourceGroups.Count() == 0 && jres.Children().Count() > 1)
                {
                    throw new Exception($"Invalid fields for Resource with SubscriptionId '{res.SubscriptionId}' were defined. Valid fields are 'SubscriptionId' and 'ResourceGroups'.");
                }
                else if (res.SubscriptionId == null && jres.Children().Count() > 0)
                {
                    throw new Exception($"Missing 'SubscriptionId' for Resource. Invalid fields were defined; valid fields are 'SubscriptionId' and 'ResourceGroups'.");
                }
                else if (res.SubscriptionId != null && res.ResourceGroups.Count() != 0)
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

                        if (resGroup.ResourceGroupName != null && resGroup.KeyVaults.Count() == 0 && jresGroup.Children().Count() > 1)
                        {
                            throw new Exception($"Invalid fields for ResourceGroup with ResourceGroupName '{resGroup.ResourceGroupName}' were defined. " +
                                $"Valid fields are 'ResourceGroupName' and 'KeyVaults'.");
                        }
                        else if (resGroup.ResourceGroupName == null && jresGroup.Children().Count() > 0)
                        {
                            throw new Exception("Missing 'ResourceGroupName' for ResourceGroup. Invalid fields were defined; valid fields are 'ResourceGroupName' and 'KeyVaults'.");
                        }
                        else if (resGroup.ResourceGroupName != null && resGroup.KeyVaults.Count() != 0 && jresGroup.Children().Count() > 2)
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
        public Dictionary<string, string> getSecrets(JsonInput vaultList)
        {
            Dictionary<string, string> secrets = new Dictionary<string, string>();
            try
            {
                secrets["appName"] = vaultList.AadAppKeyDetails.AadAppName;

                // Creates the SecretClient and grabs secrets
                string keyVaultName = vaultList.AadAppKeyDetails.VaultName;
                string keyVaultUri = Constants.HTTP + keyVaultName + Constants.AZURE_URL;
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Creating secret client");
                SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Secret client created");
                try
                {
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Getting client id secret");
                    KeyVaultSecret clientIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientIdSecretName);
                    secrets["clientId"] = clientIdSecret.Value;
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Client id retrieved");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {

                        Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to find client id secret\n" + e.ToString());
                        Constants.getLog().Flush();
                        Exit($"\nError: clientIdSecret could not be found.");
                    }
                    else
                    {
                        Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error retrieving client id secret\n" + e.ToString());
                        Constants.getLog().Flush();
                        Exit($"\nError: clientIdSecret {e.Message}.");
                    }
                }
                try
                {
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Getting client key secret");
                    KeyVaultSecret clientKeySecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientKeySecretName);
                    secrets["clientKey"] = clientKeySecret.Value;
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Client key retrieved");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to find client key secret\n" + e.ToString());
                        Constants.getLog().Flush();
                        Exit($"\nError: clientKeySecret could not be found.");
                    }
                    else
                    {
                        Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error retrieving client key secret\n" + e.ToString());
                        Constants.getLog().Flush();
                        Exit($"\nError: clientKeySecret {e.Message}.");
                    }
                }
                try
                {
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Getting tenant id secret");
                    KeyVaultSecret tenantIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.TenantIdSecretName);
                    secrets["tenantId"] = tenantIdSecret.Value;
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Tenant id retrieved");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to find tenant id secret\n" + e.ToString());
                        Constants.getLog().Flush();
                        Exit($"\nError: tenantIdSecret could not be found.");
                    }
                    else
                    {
                        Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error retrieving tenant id secret\n" + e.ToString());
                        Constants.getLog().Flush();
                        Exit($"\nError: tenantIdSecret {e.Message}.");
                    }
                }
            } 
            catch (Exception e)
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error creating secret client\n" + e.ToString());
                Constants.getLog().Flush();
                Exit($"\nError: {e.Message}");
            }
            return secrets;
        }

        /// <summary>
        /// This method creates and returns a KeyVaulManagementClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The KeyVaultManagementClient created using the secret information</returns>
        public Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient createKVMClient(Dictionary<string, string> secrets)
        {
            try
            {
                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(secrets["clientId"], 
                    secrets["clientKey"], secrets["tenantId"], AzureEnvironment.AzureGlobalCloud);
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Creating Key Vault Management Client");
                var ret = new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient(credentials);
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Key Vault Management Client Created");
                return ret;
            } 
            catch (Exception e)
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to create Key Vault Management Client\n" + e.ToString());
                Constants.getLog().Flush();
                Exit($"\nError: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// This method creates and returns a GraphServiceClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The GraphServiceClient created using the secret information</returns>
        public GraphServiceClient createGraphClient(Dictionary<string, string> secrets)
        {
            try
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Creating graph client");
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
                var ret = new GraphServiceClient(authProvider);
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Graph client created");
                return ret;
            }
            catch (Exception e)
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error creating graph client\n" + e.ToString());
                Constants.getLog().Flush();
                Exit($"\nError: {e.Message}");   
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
        public List<KeyVaultProperties> getVaults(JsonInput vaultList, 
            Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, GraphServiceClient graphClient)
        {
            List<Vault> vaultsRetrieved = new List<Vault>();
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Retrieving Key Vaults from client");
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
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Error: {e.Message}");
                                    Console.ResetColor();
                                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $": Unable to retrieve Key Vault, {vaultName} from client\n" + e.ToString());
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
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Available Key Vaults retrieved from client");
            
            List<KeyVaultProperties> keyVaultsRetrieved = new List<KeyVaultProperties>();
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Retrieving data from graph client");
            foreach (Vault curVault in vaultsRetrieved) 
            {
                keyVaultsRetrieved.Add(new KeyVaultProperties(curVault, graphClient, Constants.getLog()));
            }
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Graph client data retrieved");
            return keyVaultsRetrieved;
        }

        /// <summary>
        /// This method retrieves the KeyVaults from all the pages of KeyVaults as one page can only store a limited number of KeyVaults.
        /// </summary>
        /// <param name="kvmClient">The KeyVaultManagementClient</param>
        /// <param name="vaultsRetrieved">The list of Vault objects to add to</param>
        /// <param name="resourceGroup">The ResourceGroup name(if applicable). Default is null.</param>
        /// <returns>The updated vaultsRetrieved list</returns>
        public List<Vault> getVaultsAllPages(Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, 
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {e.Message}");
                    Console.ResetColor();
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to retrieve Key Vaults from client\n" + e.ToString());
                    
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {e.Message}");
                    Console.ResetColor();
                    Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to retrieve Key Vaults from client\n" + e.ToString());
                    
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
        public void convertToYaml(List<KeyVaultProperties> vaultsRetrieved, string yamlDirectory)
        {
            try
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Serializing data");
                var serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(vaultsRetrieved);

                System.IO.File.WriteAllText(yamlDirectory, yaml);
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Data serialized");
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Logging complete...");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error serializing data\n" + e.ToString());
            }
            Constants.getLog().Flush();
        }
        public void Exit(string message)
        {
            if (!Testing)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
                Constants.getLog().Close();
                Environment.Exit(1);
            }
            else
            {
                throw new Exception($"{message}");
            }
        }
        public bool Testing { get; set; }

    }
}