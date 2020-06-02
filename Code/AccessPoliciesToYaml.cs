using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest.Azure;
using Microsoft.Graph;

namespace RBAC
{
    /// <summary>
    /// "Phase 1" Code that serializes a list of Key Vaults into Yaml.
    /// </summary>
    class AccessPoliciesToYaml
    {
        /// <summary>
        /// This method Reads in a JSON config file and prints out a serialized list of Key Vaults into a YAML file.
        /// </summary>
        /// <param name="args">None</param>
        static void Main(string[] args)

        {
            Console.WriteLine("Reading input file...");
            string masterConfig = System.IO.File.ReadAllText(@"..\..\..\..\Config\MasterConfig.json");
            JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
            Console.WriteLine("Success!");

            Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
            var secrets = getSecrets(vaultList);
            var kvmClient = createKVMClient(secrets);
            var graphClient = createGraphClient(secrets);
            Console.WriteLine("Success!");

            Console.WriteLine("\nRetrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Success!");

            Console.WriteLine("\nGenerating YAML output...");
            convertToYaml(vaultsRetrieved);
            Console.WriteLine("Success!");
        }

        /// <summary>
        /// This method retrieves the AadAppSecrets using a SecretClient and returns a Dictionary of the secrets.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtaind from MasterConfig.json file</param>
        /// <returns>The dictionary of secrets obtained from the SecretClient</returns>
        public static Dictionary<string, string> getSecrets(JsonInput vaultList)
        {
            Dictionary<string, string> secrets = new Dictionary<string, string>();
            secrets["appName"] = vaultList.AadAppKeyDetails.AadAppName;

            // Creates the SecretClient and grabs secrets
            string keyVaultName = vaultList.AadAppKeyDetails.VaultName;
            string keyVaultUri = "https://" + keyVaultName + ".vault.azure.net";

            SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            KeyVaultSecret clientIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientIdSecretName);
            secrets["clientId"] = clientIdSecret.Value;
            KeyVaultSecret clientKeySecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientKeySecretName);
            secrets["clientKey"] = clientKeySecret.Value;
            KeyVaultSecret tenantIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.TenantIdSecretName);
            secrets["tenantId"] = tenantIdSecret.Value;

            return secrets;
        }

        /// <summary>
        /// This method creates and returns a KeyVaulManagementClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The KeyVaultManagementClient created using the secret information</returns>
        public static Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient createKVMClient(Dictionary<string, string> secrets)
        {
            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(secrets["clientId"], secrets["clientKey"], secrets["tenantId"], AzureEnvironment.AzureGlobalCloud);
            return (new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient(credentials));
        }

        /// <summary>
        /// This method creates and returns a GraphServiceClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The GraphServiceClient created using the secret information</returns>
        public static GraphServiceClient createGraphClient(Dictionary<string, string> secrets)
        {
            string auth = "https://login.microsoftonline.com/" + secrets["tenantId"] + "/v2.0";
            string redirectUri = "https://" + secrets["appName"];

            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(secrets["clientId"])
                                                          .WithAuthority(auth)
                                                          .WithRedirectUri(redirectUri)
                                                          .WithClientSecret(secrets["clientKey"])
                                                          .Build();

            List<string> scopes = new List<string>()
            {
                "https://graph.microsoft.com/.default"
            };
            MsalAuthenticationProvider authProvider = new MsalAuthenticationProvider(cca, scopes.ToArray());
            return (new GraphServiceClient(authProvider));
        }


        /// <summary>
        /// This method retrieves each of the KeyVaults specified in the vaultList.
        /// </summary>
        /// <param name="vaultList">The data obtained from deserializing json file</param>
        /// <param name="kvmClient">The KeyVaultManagementClient containing Vaults</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient for obtaining display names</param>
        /// <returns>The list of KeyVaultProperties containing the properties of each KeyVault</returns>
        public static List<KeyVaultProperties> getVaults(JsonInput vaultList, Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, GraphServiceClient graphClient)
        {
            List<Vault> vaultsRetrieved = new List<Vault>();
            foreach (Resource res in vaultList.Resources)
            {
                // Associates the client with the subscription
                kvmClient.SubscriptionId = res.SubscriptionId;

                // Retrieves all KeyVaults at the Subscription scope
                if (res.ResourceGroups == null)
                { 
                    vaultsRetrieved = getVaultsAllPages(kvmClient, vaultsRetrieved);
                }
                else
                {
                    foreach (ResourceGroup resGroup in res.ResourceGroups) 
                    {
                        // Retrieves all KeyVaults at the ResourceGroup scope
                        if (resGroup.KeyVaults == null) 
                        { 
                            vaultsRetrieved = getVaultsAllPages(kvmClient, vaultsRetrieved, resGroup.ResourceGroupName);
                        }
                        // Retrieves all KeyVaults at the Resource scope
                        else
                        { 
                            foreach (string vaultName in resGroup.KeyVaults) 
                            {
                                vaultsRetrieved.Add(kvmClient.Vaults.Get(resGroup.ResourceGroupName, vaultName));
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
        public static List<Vault> getVaultsAllPages(Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, List<Vault> vaultsRetrieved, string resourceGroup = null)
        {
            IPage<Vault> vaults_curPg;
            // Retrieves the first page of KeyVaults at the Subscription scope
            if (resourceGroup == null) 
            { 
                vaults_curPg = kvmClient.Vaults.ListBySubscription();
            }
            // Retrieves the first page of KeyVaults at the ResourceGroup scope
            else
            { 
                vaults_curPg = kvmClient.Vaults.ListByResourceGroup(resourceGroup);
            }
            vaultsRetrieved.AddRange(vaults_curPg);

            // Get remaining pages
            while (vaults_curPg.NextPageLink != null) 
            {
                IPage<Vault> vaults_nextPg;
                // Retrieves the remaining pages of KeyVaults at the Subscription scope
                if (resourceGroup == null) // then by Subscription
                {
                    vaults_nextPg = kvmClient.Vaults.ListBySubscriptionNext(vaults_curPg.NextPageLink);
                }
                // Retrieves the remaining pages of KeyVaults at the ResourceGroup scope
                else
                {
                    vaults_nextPg = kvmClient.Vaults.ListByResourceGroupNext(vaults_curPg.NextPageLink);
                }
                vaultsRetrieved.AddRange(vaults_nextPg);
                vaults_curPg = vaults_nextPg;
            }
            return vaultsRetrieved;
        }

        /// <summary>
        /// This method serializes the list of Vault objects and outputs the YAML.
        /// </summary>
        /// <param name="vaultsRetrieved">The list of KeyVault Properties to serialize</param>
        public static void convertToYaml(List<KeyVaultProperties> vaultsRetrieved)
        {
            var serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(vaultsRetrieved);
            System.IO.File.WriteAllText(@"..\..\..\..\Config\YamlOutput.yml", yaml);
        }

    }
}