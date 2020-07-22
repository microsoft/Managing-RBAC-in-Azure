using System;
using System.Collections.Generic;

namespace RBAC
{
    public class UpdatePoliciesProgram
    {
        /// <summary>
        /// This method reads in the Yaml file with access policy changes and updates these policies in Azure.
        /// </summary>
        static void Main(string[] args)
        {
            runProgram(args, false);
        }
        public static List<KeyVaultProperties> runProgram(string[] args, bool testing)
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(testing);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Refer to 'Config/Log.log' for more details should an error be thrown.\n");
            Console.ResetColor();

            Console.WriteLine("Reading input files...");
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
            Console.WriteLine("Finished!"); ;

            Console.WriteLine("Checking access and retrieving key vaults...");
            ap.checkAccess(vaultList, azureClient);
            List<KeyVaultProperties> vaultsRetrieved = ap.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Finished!");

            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(testing);

            Console.WriteLine("Reading yaml file...");
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml(args[1]);
            Console.WriteLine("Finished!");

            Console.WriteLine("Updating key vaults...");
            List<KeyVaultProperties> deletedPolicies = up.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
            Console.WriteLine("Finished!");

            Console.WriteLine("Generating DeletedPolicies yaml...");
            up.convertToYaml(deletedPolicies);
            Console.WriteLine("Finished!");

            if (testing)
            {
                return up.Changed;
            }
            return null;
        }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}


