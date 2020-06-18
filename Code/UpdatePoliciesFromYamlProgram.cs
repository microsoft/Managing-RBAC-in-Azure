using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace RBAC
{
    public class UpdatePoliciesFromYamlProgram
    {
        /// <summary>
        /// This method reads in the Yaml file with access policy changes and updates these policies in Azure.
        /// </summary>
        /// <param name="args">Contains the Json directory and Yaml directory</param>
        static void Main(string[] args)
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(false);
            Constants.toggle = "phase2";

            Console.WriteLine("Reading input file...");
            ap.verifyFileExtensions(args);
            JsonInput vaultList = ap.readJsonFile(args[0]);
            Console.WriteLine("Finished!");

            Console.WriteLine("Grabbing secrets...");
            var secrets = ap.getSecrets(vaultList);
            Console.WriteLine("Finished!");

            Console.WriteLine("Creating KeyVaultManagementClient and GraphServiceClient...");
            var kvmClient = ap.createKVMClient(secrets);
            var graphClient = ap.createGraphClient(secrets);
            Console.WriteLine("Finished!");

            Console.WriteLine("Retrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = ap.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Finished!");

            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(false);

            Console.WriteLine("Reading yaml file...");
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml(args[1]);
            int changes = up.checkChanges(yamlVaults, vaultsRetrieved);
            Console.WriteLine("Finished!");
            if (changes != 0)
            {
                Console.WriteLine("Updating key vaults...");
                up.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
                Console.WriteLine("Updates finished!");
            }
            else
            {
                Console.WriteLine("There is no difference between the YAML and the Key Vaults. No changes made");
            }
        }
    }
}


