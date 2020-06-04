using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RBAC
{
    class UpdatePoliciesFromYaml
    {
        /// <summary>
        /// This method updates the access policies for each KeyVault in the yamlVaults list.
        /// </summary>
        /// <param name="yamlVaults">he list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        public static void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets)
        {
            foreach(KeyVaultProperties kv in yamlVaults)
            {
                if (!vaultsRetrieved.Contains(kv))
                {
                    Console.WriteLine("\nUpdating " + kv.VaultName + "...");
                    updateVault(kv, kvmClient, secrets);
                }
            }
        }

        /// <summary>
        /// This method updates the access policies of the specified KeyVault in Azure.
        /// </summary>
        /// <param name="kv">The KeyVault you want to update</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        private static void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets)
        {
            kvmClient.SubscriptionId = kv.SubscriptionId;

            VaultProperties properties = kvmClient.Vaults.GetAsync(kv.ResourceGroupName, kv.VaultName).Result.Properties;
            properties.AccessPolicies = new List<AccessPolicyEntry>();

            foreach(ServicePrincipalPermissions sp in kv.AccessPolicies)
            {
                try
                {
                    checkPermissions(sp);
                    properties.AccessPolicies.Add(new AccessPolicyEntry(new Guid(secrets["tenantId"]), sp.ObjectId, 
                        new Permissions(sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates)));
                }
                catch(Exception e)
                {
                    Console.WriteLine($"{e.Message} for {sp.DisplayName} in {kv.VaultName}");
                    System.Environment.Exit(1); 
                }
            }

            Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
            Console.WriteLine("" + updatedVault.Name + " successfully updated!");
        }

        /// <summary>
        /// This method verifies that each permission entry is valid.
        /// </summary>
        /// <param name="sp">The ServicePrincipalPermissions for which we want to validate</param>
        /// <returns>True if all of the permission entries are valid. Otherwise, returns throws an exception.</returns>
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
    }
}
