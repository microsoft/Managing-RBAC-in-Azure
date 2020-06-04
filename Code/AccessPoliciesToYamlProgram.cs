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
        /// This method Reads in a JSON config file and prints out a serialized list of Key Vaults into a YAML file.
        /// </summary>
        /// <param name="args">None</param>
        static void Main(string[] args)
        {
             Console.WriteLine("Reading input file...");
             string masterConfig = System.IO.File.ReadAllText(@"..\..\..\..\Config\json2.json");
             JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
             Console.WriteLine("Success!");

             Console.WriteLine("\nGrabbing secrets...");
             var secrets = AccessPoliciesToYaml.getSecrets(vaultList);

             // If secrets contains all 4 secrets needed, continue
             if (secrets.Count == 4)
             {
                 Console.WriteLine("Success!");
                 Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
                 var kvmClient = AccessPoliciesToYaml.createKVMClient(secrets);
                 var graphClient = AccessPoliciesToYaml.createGraphClient(secrets);

                 // If both clients were created successfully, continue
                 if (kvmClient != null && graphClient != null)
                 {
                     Console.WriteLine("Success!");

                     Console.WriteLine("\nRetrieving key vaults...");
                     List<KeyVaultProperties> vaultsRetrieved = AccessPoliciesToYaml.getVaults(vaultList, kvmClient, graphClient);
                     Console.WriteLine("Success!");

                    /*Console.WriteLine("\nGenerating YAML output...");
                    AccessPoliciesToYaml.convertToYaml(vaultsRetrieved);
                    Console.WriteLine("Success!");*/

                    Console.WriteLine("Reading yaml file...");
                    string yaml = System.IO.File.ReadAllText(@"..\..\..\..\Config\YamlOutput.yml");
                    var deserializer = new DeserializerBuilder().Build();
                    List<KeyVaultProperties> yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);
                    Console.WriteLine("Success!");

                    Console.WriteLine("\nUpdating key vaults...");
                    UpdatePoliciesFromYaml.updateVault(kvmClient, secrets, yamlVaults, vaultsRetrieved);
                    Console.WriteLine("Updates finished!");
                }
             }
        }
    }
}
