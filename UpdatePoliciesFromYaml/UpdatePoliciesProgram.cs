﻿// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Graph;
using static Microsoft.Azure.Management.Fluent.Azure;

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
            UpdatePoliciesFromYaml up = new UpdatePoliciesFromYaml(testing);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Refer to 'Log.log' for more details should an error be thrown.\n");
            Console.ResetColor();

            Console.WriteLine("Reading input files...");
            up.verifyFileExtensions(args);
            JsonInput vaultList = ap.readJsonFile(args[0]);
            Console.WriteLine("Finished!");

            Console.WriteLine("Grabbing secrets...");
            Dictionary<string,string> secrets = ap.getSecrets();
            Console.WriteLine("Finished!");

            Console.WriteLine("Creating KeyVaultManagementClient, GraphServiceClient, and AzureClient...");
            KeyVaultManagementClient kvmClient = ap.createKVMClient(secrets);
            GraphServiceClient graphClient = ap.createGraphClient(secrets);
            IAuthenticated azureClient = ap.createAzureClient(secrets);
            Console.WriteLine("Finished!"); ;

            Console.WriteLine("Checking access and retrieving key vaults...");
            ap.checkAccess(vaultList, azureClient);
            List<KeyVaultProperties> vaultsRetrieved = ap.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Finished!");

            Console.WriteLine("Reading yaml file...");
            List<KeyVaultProperties> yamlVaults = up.deserializeYaml(args[1]);
            Console.WriteLine("Finished!");

            Console.WriteLine("Updating key vaults...");
            List<KeyVaultProperties> deletedPolicies = up.updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets, graphClient);
            Console.WriteLine("Finished!");

            Console.WriteLine("Generating DeletedPolicies yaml...");
            up.convertToYaml(deletedPolicies, args[2]);
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


