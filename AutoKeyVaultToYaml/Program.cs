using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Linq;

namespace AutoKeyVaultToYaml
{
    class Program
    {
        static void Main(string[] args)
        {
            string masterConfig = File.ReadAllText(@"C:\src\SRE.common\Automation\RBACAutomation\MasterConfig.json");
            Config vaultList = JsonConvert.DeserializeObject<Config>(masterConfig);

            IAzure azure = getAzureClient(vaultList);
            List<IVault> vaultsRetrieved = getVaults(vaultList, azure);

            List<Vault> vaultObjects = convertIVaultToVault(vaultsRetrieved, azure);
            convertToYaml(vaultObjects);
        }

        /**
         * Retrieves the secrets from the specified Azure KeyVault and returns an Azure client
         */
        public static IAzure getAzureClient(Config vaultList)
        {
            //Creates SecretClient and grabs secrets
            string keyVaultName = vaultList.AadAppKeyDetails.VaultName; 
            string keyVaultUri = "https://" + keyVaultName + ".vault.azure.net";

            SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            KeyVaultSecret clientIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientIdSecretName); 
            string clientId = clientIdSecret.Value;
            KeyVaultSecret clientKeySecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.ClientKeySecretName); 
            string clientKey = clientKeySecret.Value;
            KeyVaultSecret tenantIdSecret = secretClient.GetSecret(vaultList.AadAppKeyDetails.TenantIdSecretName);
            string tenantId = tenantIdSecret.Value;

            string subscriptionId = vaultList.AadAppKeyDetails.SubscriptionId;

            //Creates Azure client
            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientKey, tenantId, AzureEnvironment.AzureGlobalCloud);
            IAzure azureClient = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);

            return azureClient;
        }

        /**
         * Adds each of the vaults specified in the MasterConfig file to a list of IVault objects and returns the list
         */
        public static List<IVault> getVaults(Config vaultList, IAzure azure)
        {
            List<IVault> vaultsRetrieved = new List<IVault>();
            
            foreach (Resource res in vaultList.Resources)
            {
                if (res.ResourceGroups == null) // then get by SubscriptionId
                {
                    List<IVault> listOfVaults = azure.Vaults.List().ToList();
                    vaultsRetrieved.Concat(listOfVaults);
                }
                else // then specifies ResourceGroups
                {
                    foreach (ResourceGroup resGroup in res.ResourceGroups)
                    {
                        if (resGroup.KeyVaults == null) // then get by ResourceGroupName
                        {
                            List<IVault> listOfVaults = azure.Vaults.ListByResourceGroup(resGroup.ResourceGroupName).ToList();
                            vaultsRetrieved.Concat(listOfVaults);
                        }
                        else // then get specific KeyVault list
                        {
                            foreach (string KV in resGroup.KeyVaults)
                            {
                                IVault keyVault = azure.Vaults.GetByResourceGroup(resGroup.ResourceGroupName, KV);
                                vaultsRetrieved.Add(keyVault);
                            }
                        }
                    }
                }
            }
            return vaultsRetrieved;
        }

        /**
         * Converts the list of IVault objects to a list of Vault objects and returns the list
         */
        public static List<Vault> convertIVaultToVault(List<IVault> vaultsRetrieved, IAzure azure)
        {
            List<Vault> vaultObjects = new List<Vault>();

            var vaultParser = vaultsRetrieved.GetEnumerator();
            while (vaultParser.MoveNext())
            {
                IVault currentVault = vaultParser.Current;
                Vault currentVaultObject = new Vault(azure, currentVault);
                vaultObjects.Add(currentVaultObject);
            }

            return vaultObjects;
        }

        /**
         * Serializes the list of Vault objects and outputs the YAML
         */
        public static void convertToYaml(List<Vault> vaultObjects)
        {
            var serializer = new SerializerBuilder().Build();

            StringBuilder yamlOutput = new StringBuilder();
            yamlOutput.Append(serializer.Serialize(vaultObjects));

            File.WriteAllText(@"C:\src\SRE.common\Automation\RBACAutomation\AutoKeyVaultToYaml\outputFile.yml", yamlOutput.ToString()); 
        }
    }
}
