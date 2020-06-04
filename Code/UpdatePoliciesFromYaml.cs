using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace RBAC
{
    class UpdatePoliciesFromYaml
    {
<<<<<<< Updated upstream
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
            Console.WriteLine("Updates finished!");
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
        }

        private static void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets)
        {
            Console.WriteLine("\nUpdating " + kv.VaultName + "...");
            kvmClient.SubscriptionId = kv.SubscriptionId;
            var properties = kvmClient.Vaults.GetAsync(kv.ResourceGroupName, kv.VaultName).Result.Properties;
            properties.AccessPolicies = new List<AccessPolicyEntry>();
            foreach(ServicePrincipalPermissions sp in kv.AccessPolicies)
            {
                try
                {
                    checkPermissions(sp);
                    properties.AccessPolicies.Add(new Microsoft.Azure.Management.KeyVault.Models.AccessPolicyEntry(new Guid(secrets["tenantId"]), sp.ObjectId, new Microsoft.Azure.Management.KeyVault.Models.Permissions(sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates)));
                }
                catch(Exception e)
                {
                    Console.WriteLine($"{e.Message} for {sp.DisplayName} in {kv.VaultName}");
                    System.Environment.Exit(1);
                }
            }
            var res = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new Microsoft.Azure.Management.KeyVault.Models.VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
            Console.WriteLine("" + res.Name + " successfully updated!");
        }

        private static bool checkPermissions(ServicePrincipalPermissions sp)
        {
            foreach(string kp in sp.PermissionsToKeys)
            {
                if (!ServicePrincipalPermissions.allKeyPermissions.Contains(kp.ToLower()))
                {
                    throw new Exception($"Invalid key permission {kp}");
                }
            }
            foreach(string s in sp.PermissionsToSecrets)
            {
                if (!ServicePrincipalPermissions.allSecretPermissions.Contains(s.ToLower()))
                {
                    throw new Exception($"Invalid secret permission {s}");
                }
            }
            foreach (string cp in sp.PermissionsToCertificates)
            {
                if (!ServicePrincipalPermissions.allCertificatePermissions.Contains(cp.ToLower()))
                {
                    throw new Exception($"Invalid certificate permission {cp}");
                }
            }
            return true;
        }
=======
        
>>>>>>> Stashed changes
    }
}
