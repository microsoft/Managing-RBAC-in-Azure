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
    class AccessPoliciesToYamlProgram
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
            var secrets = AccessPoliciesToYaml.getSecrets(vaultList);
            var kvmClient = AccessPoliciesToYaml.createKVMClient(secrets);
            var graphClient = AccessPoliciesToYaml.createGraphClient(secrets);
            Console.WriteLine("Success!");

            Console.WriteLine("\nRetrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = AccessPoliciesToYaml.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Success!");

            Console.WriteLine("\nGenerating YAML output...");
            AccessPoliciesToYaml.convertToYaml(vaultsRetrieved);
            Console.WriteLine("Success!");
        }
    }
}
