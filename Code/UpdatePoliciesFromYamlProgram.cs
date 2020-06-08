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
            Console.WriteLine("Reading input file...");
            string masterConfig = System.IO.File.ReadAllText(@"..\..\..\..\Config\Demo_MasterConfig.json");
            JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
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
                    List<KeyVaultProperties> yamlVaults = UpdatePoliciesFromYaml.deserializeYaml();
                    Console.WriteLine("Success!");

                    Console.WriteLine("\nUpdating key vaults...");
                    UpdatePoliciesFromYaml.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
                    Console.WriteLine("Updates finished!");
                }
            }
        }
    }
}


