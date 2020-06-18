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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;

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
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(false);
            Constants.toggle = "phase1";
            Console.WriteLine("Reading input file...");
            ap.verifyFileExtensions(args);
            JsonInput vaultList = ap.readJsonFile(args[0]);
            Console.WriteLine("Finished!");
          
            Console.WriteLine("\nGrabbing secrets...");
            var secrets = ap.getSecrets(vaultList);
            Console.WriteLine("Finished!");

            Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
            var kvmClient = ap.createKVMClient(secrets);
            var graphClient = ap.createGraphClient(secrets);
            Console.WriteLine("Finished!");

            Console.WriteLine("\nRetrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = ap.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Finished!");

            Console.WriteLine("\nGenerating YAML output...");
            ap.convertToYaml(vaultsRetrieved, args[1]);
            Console.WriteLine("Finished!");
        }
    }
}
