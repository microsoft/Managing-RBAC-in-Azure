using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using Serilog;
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
            run(args, false);
        }
        public static List<KeyVaultProperties> run(string[] args, bool testing)
        {
            AccessPoliciesToYaml ap = new AccessPoliciesToYaml(testing);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Refer to 'LogFile.log' for more details should an error be thrown.\n");
            Console.ResetColor();

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

            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(testing);

            Console.WriteLine("Reading yaml file...");
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml(args[1]);
            Console.WriteLine("Finished!");

            Console.WriteLine("Updating key vaults...");
            List<KeyVaultProperties> deletedPolicies = up.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
            Console.WriteLine("Finished!");

            Console.WriteLine("Generating DeletedPolicies.yml...");
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


