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
        /// <param name="args">Contains the Json directory and Yaml directory</param>
        static void Main(string[] args)
        {
            Console.WriteLine("Reading input file...");
            AccessPoliciesToYaml.verifyFileExtensions(args);
            JsonInput vaultList = AccessPoliciesToYaml.readJsonFile(args[0]);
            Console.WriteLine("Success!");

            Console.WriteLine("\nGrabbing secrets...");
            var secrets = AccessPoliciesToYaml.getSecrets(vaultList);
            Console.WriteLine("Success!");

            Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
            var kvmClient = AccessPoliciesToYaml.createKVMClient(secrets);
            var graphClient = AccessPoliciesToYaml.createGraphClient(secrets);
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


