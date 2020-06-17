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
using System.Text;
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
            if (args.Length != 2)
            {
                throw new Exception("\nError: Missing input file.");
            }
            if (System.IO.Path.GetExtension(args[0]) != ".json")
            {
                throw new Exception("\nError: The 1st argument is not a .json file");
            }
            if (System.IO.Path.GetExtension(args[1]) != ".yml")
            {
                throw new Exception("\nError: The 2nd argument is not a .yml file");
            }
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
                string masterConfig = System.IO.File.ReadAllText(jsonDirectory);
                JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);

                checkMissingJsonInputFields(vaultList);
                foreach (Resource res in vaultList.Resources)
                {
                    checkMissingResourceFields(res);
                }
                return vaultList;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                Exit();
                return null;
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist for the Json input file.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtaind from MasterConfig.json file</param>
        public void checkMissingJsonInputFields(JsonInput vaultList)
        {
            if (vaultList.AadAppKeyDetails == null)
            {
                throw new Exception("Missing AadAppKeyDetails in Json input.");
            }
            if (vaultList.AadAppKeyDetails.AadAppName == null)
            {
                throw new Exception("Missing AadAppName for AadAppKeyDetails in Json input.");
            }
            if (vaultList.AadAppKeyDetails.VaultName == null)
            {
                throw new Exception("Missing VaultName for AadAppKeyDetails in Json input.");
            }
            if (vaultList.AadAppKeyDetails.ClientIdSecretName == null)
            {
                throw new Exception("Missing ClientIdSecretName for AadAppKeyDetails in Json input.");
            }
            if (vaultList.AadAppKeyDetails.ClientKeySecretName == null)
            {
                throw new Exception("Missing ClientKeySecretName for AadAppKeyDetails in Json input.");
            }
            if (vaultList.AadAppKeyDetails.TenantIdSecretName == null)
            {
                throw new Exception("Missing TenantIdSecretName for AadAppKeyDetails in Json input.");
            }
            if (vaultList.Resources == null)
            {
                throw new Exception("Missing Resources list in Json input.");
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist for each Resource object.
        /// </summary>
        /// <param name="res">The Resouce object for which we want to check</param>
        public void checkMissingResourceFields(Resource res)
        {
            if (res.SubscriptionId == null)
            {
                throw new Exception("Missing SubscriptionId for a Resource.");
            }
            if (res.ResourceGroups != null)
            {
                foreach(ResourceGroup resGroup in res.ResourceGroups)
                {
                    if (resGroup.ResourceGroupName == null)
                    {
                        throw new Exception($"Missing ResourceGroupName for Resource with SubscriptionId {res.SubscriptionId}.");
                    }
                }
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
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Creating secret client");
                SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Secret client created");
                try
                {
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Getting client id secret");
                    KeyVaultSecret clientIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientIdSecretName);
                    secrets["clientId"] = clientIdSecret.Value;
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Client id retrieved");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to find client id secret\n" + e.ToString());
                        Console.WriteLine($"\nError: clientIdSecret could not be found.");
                    }
                    else
                    {
                        log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error retrieving client id secret\n" + e.ToString());
                        Console.WriteLine($"\nError: clientIdSecret {e.Message}.");
                    }
                    Exit();
                }
                try
                {
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Getting client key secret");
                    KeyVaultSecret clientKeySecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientKeySecretName);
                    secrets["clientKey"] = clientKeySecret.Value;
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Client key retrieved");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to find client key secret\n" + e.ToString());
                        Console.WriteLine($"\nError: clientKeySecret could not be found.");
                    }
                    else
                    {
                        log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error retrieving client key secret\n" + e.ToString());
                        Console.WriteLine($"\nError: clientKeySecret {e.Message}.");
                    }
                    Exit();
                }
                try
                {
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Getting tenant id secret");
                    KeyVaultSecret tenantIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.TenantIdSecretName);
                    secrets["tenantId"] = tenantIdSecret.Value;
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Tenant id retrieved");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404"))
                    {
                        log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to find tenant id secret\n" + e.ToString());
                        Console.WriteLine($"\nError: tenantIdSecret could not be found.");
                    }
                    else
                    {
                        log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error retrieving tenant id secret\n" + e.ToString());
                        Console.WriteLine($"\nError: tenantIdSecret {e.Message}.");
                    }
                    Exit();
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error creating secret client\n" + e.ToString());
                Exit();
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
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Creating Key Vault Management Client");
                var ret = new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient(credentials);
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Key Vault Management Client Created");
                return ret;
            } 
            catch (Exception e)
            {
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to create Key Vault Management Client\n" + e.ToString());
                Console.WriteLine($"\nError: {e.Message}");
                Exit();
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
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Creating graph client");
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
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Graph client created");
                return ret;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error creating graph client\n" + e.ToString());
                Exit();
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
            log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Retrieving Key Vaults from client");
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
                                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $": Unable to retrieve Key Vault, {vaultName} from client\n" + e.ToString());
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
            log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Available Key Vaults retrieved from client");
            
            List<KeyVaultProperties> keyVaultsRetrieved = new List<KeyVaultProperties>();
            log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Retrieving data from graph client");
            foreach (Vault curVault in vaultsRetrieved) 
            {
                keyVaultsRetrieved.Add(new KeyVaultProperties(curVault, graphClient, log));
            }
            log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Graph client data retrieved");
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
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to retrieve Key Vaults from client\n" + e.ToString());
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
                    log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Unable to retrieve Key Vaults from client\n" + e.ToString());
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
        public void convertToYaml(List<KeyVaultProperties> vaultsRetrieved, string yamlDirectory)
        {
            try
            {
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Serializing data");
                var serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(vaultsRetrieved);

                System.IO.File.WriteAllText(yamlDirectory, yaml);
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Data serialized");
            }
            catch (Exception e)
            {
                log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Error serializing data\n" + e.ToString());
                Console.WriteLine($"\nError: {e.Message}");
            }
            log.Flush();
            log.Close();
        }
        public void Exit()
        {
            if (!Testing)
            {
                log.Flush();
                log.Close();
                Environment.Exit(1);
            }
            else
            {
                throw new Exception("Exit with error code 1");
            }
        }
        public bool Testing { get; set; }
        public static StreamWriter log = new StreamWriter(new FileStream(Constants.LOG_FILE_PATH, FileMode.Create, FileAccess.Write));
    }
}