// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Graph;
using static Microsoft.Azure.Management.Fluent.Azure;

namespace RBAC
{
    public class ToYamlProgram
    {
        /// <summary>
        /// This method reads in a Json config file and converts it into a serialized list of KeyVaults that are displayed in a Yaml file.
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
            Dictionary<string, string> secrets = ap.getSecrets();
            Console.WriteLine("Finished!");

            Console.WriteLine("Creating KeyVaultManagementClient, GraphServiceClient, and AzureClient...");
            KeyVaultManagementClient kvmClient = ap.createKVMClient(secrets);
            GraphServiceClient graphClient = ap.createGraphClient(secrets);
            IAuthenticated azureClient = ap.createAzureClient(secrets);
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
