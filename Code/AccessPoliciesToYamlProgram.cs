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
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using System.IO;

namespace RBAC
{
    class AccessPoliciesToYamlProgram
    {
        /// <summary>
        /// This method reads in a JSON config file and prints out a serialized list of Key Vaults into a YAML file.
        /// </summary>
        /// <param name="args">Contains the json directory & yaml directory</param>
        static void Main(string[] args)
        {
            // ..\..\..\..\Config\MasterConfig.json 
            // ..\..\..\..\Config\YamlOutput.yml

            if (args[0].Substring(args[0].Length-4) != "json")
            {
                throw new Exception("The 1st argument is not a json file");
                
            }

            if (args[1].Substring(args[1].Length - 3) != "yml")
            {
                throw new Exception("The 2nd argument is not a yml file");
            }

            Console.WriteLine("Reading input file...");
            JsonInput vaultList = null;
            try
            {
                string masterConfig = System.IO.File.ReadAllText(args[0]);
                vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
            }
            Console.WriteLine("Success!");


            Console.WriteLine("\nGrabbing secrets...");
            var secrets = AccessPoliciesToYaml.getSecrets(vaultList);

            // If secrets contains all 4 secrets needed, continue
            if (secrets.Count == 4)
            {
                Console.WriteLine("Success!");
                Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
                var kvmClient = AccessPoliciesToYaml.createKVMClient(secrets);
                var graphClient = AccessPoliciesToYaml.createGraphClient(secrets);

                Console.WriteLine("Success!");

                Console.WriteLine("\nRetrieving key vaults...");
                List<KeyVaultProperties> vaultsRetrieved = AccessPoliciesToYaml.getVaults(vaultList, kvmClient, graphClient);
                Console.WriteLine("Success!");

                Console.WriteLine("\nGenerating YAML output...");
                AccessPoliciesToYaml.convertToYaml(vaultsRetrieved, args[1]);
                Console.WriteLine("Success!");
            }
        }
    }
}
