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
        /// This method reads in a Json config file and prints out a serialized list of Key Vaults into a Yaml file.
        /// </summary>
        /// <param name="args">Contains the Json directory and Yaml directory</param>
        static void Main(string[] args)
        {
            // ..\..\..\..\Config\MasterConfig.json 
            // ..\..\..\..\Config\YamlOutput.yml
            Constants.toggle = "phase1";
            
            Console.WriteLine("Reading input file...");
            AccessPoliciesToYaml.verifyFileExtensions(args);
            JsonInput vaultList = AccessPoliciesToYaml.readJsonFile(args[0]);
            Console.WriteLine("Success!");

            Console.WriteLine("Grabbing secrets...");
            var secrets = AccessPoliciesToYaml.getSecrets(vaultList);
            Console.WriteLine("Success!");

            Console.WriteLine("Creating KeyVaultManagementClient and GraphServiceClient...");
            var kvmClient = AccessPoliciesToYaml.createKVMClient(secrets);
            var graphClient = AccessPoliciesToYaml.createGraphClient(secrets);
            Console.WriteLine("Success!");

            Console.WriteLine("Retrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = AccessPoliciesToYaml.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Success!");

            Console.WriteLine("Generating YAML output...");
            AccessPoliciesToYaml.convertToYaml(vaultsRetrieved, args[1]);
            Console.WriteLine("Success!");
        }
    }
}
