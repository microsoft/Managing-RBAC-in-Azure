using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
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

            Console.WriteLine("\nUpdating key vaults...");
            updateVaults(yamlVaults, vaultsRetrieved, kvmClient, secrets);
            Console.WriteLine("Success!");
        }

        private static void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets)
        {
            foreach(KeyVaultProperties kv in yamlVaults)
            {
                if (!vaultsRetrieved.Contains(kv))
                {
                    updateVault(kv, kvmClient, secrets);
                }
            }
            /*
            foreach(KeyVaultProperties kv in vaultsRetrieved)
            {
                if (!yamlVaults.Contains(kv))
                {
                    deleteVault(kv, kvmClient);
                }
            }
            */
        }

        private static void deleteVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient)
        {
            kvmClient.Vaults.DeleteAsync(kv.ResourceGroupName, kv.VaultName);
        }

        private static void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets)
        {
            Console.WriteLine("Updating " + kv.VaultName);
            kvmClient.SubscriptionId = kv.SubscriptionId;
            var p = kvmClient.Vaults.GetAsync(kv.ResourceGroupName, kv.VaultName).Result.Properties;
            foreach(ServicePrincipalPermissions sp in kv.AccessPolicies)
            {
                p.AccessPolicies.Add(new Microsoft.Azure.Management.KeyVault.Models.AccessPolicyEntry(new Guid(secrets["tenantId"]), sp.ObjectId, new Microsoft.Azure.Management.KeyVault.Models.Permissions(sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates)));
            }
            var res = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new Microsoft.Azure.Management.KeyVault.Models.VaultCreateOrUpdateParameters(kv.Location, p)).Result;
        }
    }
}
