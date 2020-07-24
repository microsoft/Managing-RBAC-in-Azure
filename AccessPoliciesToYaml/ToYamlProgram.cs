using System;
using System.Collections.Generic;

namespace RBAC
{
    public class ToYamlProgram
    {
        /// <summary>
        /// This method reads in a Json config file and prints out a serialized list of Key Vaults into a Yaml file.
        /// </summary>
        public static void Main(string[] args)
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(false);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Refer to 'Log.log' for more details should an error be thrown.\n");
            Console.ResetColor();

            Console.WriteLine("Reading input file...");
            ap.verifyFileExtensions(args);
            JsonInput vaultList = ap.readJsonFile(args[0]);
            Console.WriteLine("Finished!");
          
            Console.WriteLine("Grabbing secrets...");
            var secrets = ap.getSecrets(vaultList);
            Console.WriteLine("Finished!");

            Console.WriteLine("Creating KeyVaultManagementClient, GraphServiceClient, and AzureClient...");
            var kvmClient = ap.createKVMClient(secrets);
            var graphClient = ap.createGraphClient(secrets);
            var azureClient = ap.createAzureClient(secrets);
            Console.WriteLine("Finished!");;

            Console.WriteLine("Checking access and retrieving key vaults...");
            ap.checkAccess(vaultList, azureClient);
            List<KeyVaultProperties> vaultsRetrieved = ap.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Finished!");

            Console.WriteLine("Generating YAML output...");
            ap.convertToYaml(vaultsRetrieved, args[1]);
            Console.WriteLine("Finished!");
        }
    }
}
