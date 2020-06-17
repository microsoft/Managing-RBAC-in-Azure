﻿using Microsoft.Extensions.Azure;
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
            Console.WriteLine("Reading input file...");
            ap.verifyFileExtensions(args);
            JsonInput vaultList = ap.readJsonFile(args[0]);
            Console.WriteLine("Success!");

            Console.WriteLine("\nGrabbing secrets...");
            var secrets = ap.getSecrets(vaultList);
            Console.WriteLine("Success!");

            Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
            var kvmClient = ap.createKVMClient(secrets);
            var graphClient = ap.createGraphClient(secrets);
            Console.WriteLine("Success!");

            Console.WriteLine("\nRetrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = ap.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Success!");

            Console.WriteLine("\nReading yaml file...");
            List<KeyVaultProperties> yamlVaults = UpdatePoliciesFromYaml.deserializeYaml(args[1]);
            int changes = UpdatePoliciesFromYaml.checkChanges(yamlVaults, vaultsRetrieved);
            Console.WriteLine("Success!");
            if(changes != 0)
            {
                Console.WriteLine("\nUpdating key vaults...");
                UpdatePoliciesFromYaml.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
                Console.WriteLine("Updates finished!");
            }
            else
            {
                Console.WriteLine("There is no difference between the YAML and the Key Vaults. No changes made");
            }
        }
    }
}


