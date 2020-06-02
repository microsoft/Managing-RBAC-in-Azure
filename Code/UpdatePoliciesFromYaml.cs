using Microsoft.Azure.Management.KeyVault;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace RBAC
{
    class UpdatePoliciesFromYaml
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Reading input files...");
            string masterConfig = System.IO.File.ReadAllText(@"..\..\..\..\Config\MasterConfig.json");
            JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);
            string yaml = System.IO.File.ReadAllText(@"..\..\..\..\Config\YamlOutput.yml");
            var input = new StringReader(yaml);
            var deserializer = new DeserializerBuilder().Build();
            var yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(input);
            Console.WriteLine("Success!");

            Console.WriteLine("\nCreating KeyVaultManagementClient and GraphServiceClient...");
            var secrets = AccessPoliciesToYaml.getSecrets(vaultList);
            var kvmClient = AccessPoliciesToYaml.createKVMClient(secrets);
            var graphClient = AccessPoliciesToYaml.createGraphClient(secrets);
            Console.WriteLine("Success!");

            Console.WriteLine("\nRetrieving key vaults...");
            List<KeyVaultProperties> vaultsRetrieved = AccessPoliciesToYaml.getVaults(vaultList, kvmClient, graphClient);
            Console.WriteLine("Success!");

            Console.WriteLine("Updating key vaults...");
            updateVaults(yamlVaults, vaultsRetrieved, kvmClient, graphClient);
            Console.WriteLine("Success!");
        }

        private static void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient, GraphServiceClient graphClient)
        {
            throw new NotImplementedException();
        }
    }
}
