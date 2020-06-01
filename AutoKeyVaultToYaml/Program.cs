﻿using System;
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
using Microsoft.Azure.Management;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Linq;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest.Azure;
using Namotion.Reflection;
using YamlDotNet.Core.Tokens;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Graph;

namespace AutoKeyVaultToYaml
{
    class Program
    {
        static void Main(string[] args)

        {
            Console.WriteLine("Reading input file...");
            string masterConfig = System.IO.File.ReadAllText(@"C:\src\SRE.common\Automation\RBACAutomation\MasterConfig.json");
            Config vaultList = JsonConvert.DeserializeObject<Config>(masterConfig);
            Console.WriteLine("Success!");

            Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
            var secrets = getSecrets(vaultList);
            var kvmClient = createKVMClient(secrets);
            var graphClient = createGraphClient(secrets);
            Console.WriteLine("Success!");

            Console.WriteLine("\nRetrieving key vaults...");
            List<KeyVault> vaultsRetrieved = getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Success!");

            Console.WriteLine("\nGenerating YAML output...");
            convertToYaml(vaultsRetrieved);
            Console.WriteLine("Success!");
        }

        /**
         * Retrieves the AadAppSecrets using a SecretClient and returns a Dictionary of the secrets
         */
        public static Dictionary<string, string> getSecrets(Config vaultList)
        {
            Dictionary<string, string> secrets = new Dictionary<string, string>();
            secrets["appName"] = vaultList.AadAppKeyDetails.AadAppName;

            // Creates SecretClient and grabs secrets
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

        /**
         * Creates and returns a KeyVaultManagementClient
         */
        public static Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient createKVMClient(Dictionary<string, string> secrets)
        {
            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(secrets["clientId"], secrets["clientKey"], secrets["tenantId"], AzureEnvironment.AzureGlobalCloud);
            return (new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient(credentials));
        }

        /**
         * Creates and returns a GraphServiceClient
         */
        public static GraphServiceClient createGraphClient(Dictionary<string, string> secrets)
        {
            string auth = "https://" + "login.microsoftonline.com/" + secrets["tenantId"] + "/v2.0";
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


        /**
         * Creates an IAzure client for each Resource in the MasterConfig, each associated with the specified subscription, and retrieves the specified KeyVaults
         * Converts each IVault object to a Vault object, adds each to a list of Vault objects, and returns that list
         */
        public static List<KeyVault> getVaults(Config vaultList, Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, GraphServiceClient graphClient)
        {
            List<Vault> vaultsRetreived = new List<Vault>();
            foreach (Resource res in vaultList.Resources)
            {
                // Associate the client with the subscription
                kvmClient.SubscriptionId = res.SubscriptionId;

                // Retrieves all KeyVaults
                if (res.ResourceGroups == null) // then get all vaults at subscription scope
                { 
                    vaultsRetreived = getVaultsAllPages(kvmClient, vaultsRetreived);
                } 
                else 
                {
                    foreach (ResourceGroup resGroup in res.ResourceGroups) 
                    {
                        if (resGroup.KeyVaults == null) // then get all vaults at resource group scope
                        { 
                            vaultsRetreived = getVaultsAllPages(kvmClient, vaultsRetreived, resGroup.ResourceGroupName);
                        }
                        else // then get specific Key Vaults
                        { 
                            foreach (string vaultName in resGroup.KeyVaults) 
                            {
                                vaultsRetreived.Add(kvmClient.Vaults.Get(resGroup.ResourceGroupName, vaultName));
                            }
                        }
                    }
                }
            }

            List<KeyVault> keyVaultsRetrieved = new List<KeyVault>();
            foreach (Vault curVault in vaultsRetreived) 
            {
                keyVaultsRetrieved.Add(new KeyVault(curVault, graphClient));
            }
            return keyVaultsRetrieved;
        }

        /* 
         * Retrieves all the KeyVaults from all the pages of KeyVaults
         */
        public static List<Vault> getVaultsAllPages(Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, List<Vault> vaultsRetrieved, string resourceGroup = null)
        {
            // Get first page
            IPage<Vault> vaults_curPg;
            if (resourceGroup == null) // then by Subscription
            { 
                vaults_curPg = kvmClient.Vaults.ListBySubscription();
            }
            else // then by ResourceGroup
            { 
                vaults_curPg = kvmClient.Vaults.ListByResourceGroup(resourceGroup);
            }
            vaultsRetrieved.AddRange(vaults_curPg);

            // Get remaining pages
            while (vaults_curPg.NextPageLink != null) 
            {
                IPage<Vault> vaults_nextPg;
                if (resourceGroup == null) // then by Subscription
                {
                    vaults_nextPg = kvmClient.Vaults.ListBySubscriptionNext(vaults_curPg.NextPageLink);
                } else // then by ResourceGroup
                {
                    vaults_nextPg = kvmClient.Vaults.ListByResourceGroupNext(vaults_curPg.NextPageLink);
                }
                vaultsRetrieved.AddRange(vaults_nextPg);
                vaults_curPg = vaults_nextPg;
            }
            return vaultsRetrieved;
        }

        /**
         * Serializes the list of Vault objects and outputs the YAML
         */
        public static void convertToYaml(List<KeyVault> vaultsRetrieved)
        {
            var serializer = new SerializerBuilder().Build();

            StringBuilder yamlOutput = new StringBuilder();
            yamlOutput.Append(serializer.Serialize(vaultsRetrieved));

            System.IO.File.WriteAllText(@"C:\src\SRE.common\Automation\RBACAutomation\AutoKeyVaultToYaml\outputFile.yml", yamlOutput.ToString());
        }

    }
}