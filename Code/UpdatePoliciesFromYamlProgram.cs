using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace RBAC
{
    class UpdatePoliciesFromYamlProgram
    {
        /// <summary>
        /// This method reads in the Yaml file with access policy changes and updates these policies in Azure.
        /// </summary>
        /// <param name="args">None</param>
        static void Main(string[] args)
        {
            if (args[0].Substring(args[0].Length - 4) != "json")
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

                // If both clients were created successfully, continue
                if (kvmClient != null && graphClient != null)
                {
                    Console.WriteLine("Success!");

                    Console.WriteLine("\nRetrieving key vaults...");
                    List<KeyVaultProperties> vaultsRetrieved = AccessPoliciesToYaml.getVaults(vaultList, kvmClient, graphClient);
                    Console.WriteLine("Success!");

                    Console.WriteLine("\nReading yaml file...");
                    List<KeyVaultProperties> yamlVaults = UpdatePoliciesFromYaml.deserializeYaml(args[1]);
                    Console.WriteLine("Success!");

                    Console.WriteLine("\nUpdating key vaults...");
                    UpdatePoliciesFromYaml.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
                    Console.WriteLine("Updates finished!");
                }
            }
        }
    }
}


