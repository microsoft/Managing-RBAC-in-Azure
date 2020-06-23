using Serilog;
using System;
using System.Collections.Generic;

namespace RBAC
{
    public class AccessPoliciesToYamlProgram
    {
        /// <summary>
        /// This method reads in a Json config file and prints out a serialized list of Key Vaults into a Yaml file.
        /// </summary>
        /// <param name="args">Contains the Json directory and Yaml directory</param>
        public static void Main(string[] args)
        {
            // ..\..\..\..\Config\MasterConfig.json 
            // ..\..\..\..\Config\YamlOutput.yml
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(false);

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

            Console.WriteLine("Generating YAML output...");
            ap.convertToYaml(vaultsRetrieved, args[1]);
            Console.WriteLine("Finished!");
        }
    }
}
