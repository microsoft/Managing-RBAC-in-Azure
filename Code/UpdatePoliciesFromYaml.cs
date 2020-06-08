using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.PolicyAssignment.Definition;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        public static void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets, 
            GraphServiceClient graphClient)
        {
            foreach(KeyVaultProperties kv in yamlVaults)
            {
                if(kv.UsersContained() < 2)
                {
                    Console.WriteLine($"{kv.VaultName} does not contain at least two users. Vault skipped.");
                }
                else if (!vaultsRetrieved.Contains(kv))
                {
                    Console.WriteLine("\nUpdating " + kv.VaultName + "...");
                    updateVault(kv, kvmClient, secrets, graphClient);
                }
            }
        }

        /// <summary>
        /// This method updates the access policies of the specified KeyVault in Azure.
        /// </summary>
        /// <param name="kv">The KeyVault you want to update</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        private static void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets, GraphServiceClient graphClient)
        {
            kvmClient.SubscriptionId = kv.SubscriptionId;

            VaultProperties properties = kvmClient.Vaults.GetAsync(kv.ResourceGroupName, kv.VaultName).Result.Properties;
            properties.AccessPolicies = new List<AccessPolicyEntry>();

            foreach (ServicePrincipalPermissions sp in kv.AccessPolicies)
            {
                string type = sp.Type.ToLower().Trim();
                Dictionary<string, string> data = verifyServicePrincipal(sp, type, graphClient);

                if (data.ContainsKey("ObjectId"))
                {
                    // Set ServicePrincipal data
                    sp.ObjectId = data["ObjectId"];
                    if (type == "group")
                    {
                        sp.Alias = data["Alias"];
                    }
                    else if (type == "application")
                    {
                        sp.ApplicationId = data["ApplicationId"];
                    }

                    try
                    {
                        checkPermissions(sp);
                        properties.AccessPolicies.Add(new AccessPolicyEntry(new Guid(secrets["tenantId"]), sp.ObjectId,
                            new Permissions(sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message} for {sp.DisplayName} in {kv.VaultName}");
                        System.Environment.Exit(1);
                    }
                } 
            }

            Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
            Console.WriteLine("" + updatedVault.Name + " successfully updated!");
        }

        /// <summary>
        /// This method verifies that the ServicePrincipal exists and returns a dictionary that holds its data.
        /// </summary>
        /// <param name="sp">The current ServicePrincipalPermissions object</param>
        /// <param name="type">The ServicePrincipalPermissions type</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        /// <returns>A dictionary containing the service principal data</returns>
        private static Dictionary<string, string> verifyServicePrincipal(ServicePrincipalPermissions sp, string type, GraphServiceClient graphClient)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            try
            {
                if (type == "user")
                {
                    User user = graphClient.Users[sp.Alias.ToLower().Trim()]
                        .Request()
                        .GetAsync().Result;
                    data["ObjectId"] = user.Id;
                }
                else if (type == "group")
                {
                    Group group = graphClient.Groups
                        .Request()
                        .Filter($"startswith(DisplayName,'{sp.DisplayName}')")
                        .GetAsync().Result[0];
                    data["ObjectId"] = group.Id;
                    data["Alias"] = group.Mail;
                }
                else if (type == "application")
                {
                    Application app = graphClient.Applications
                        .Request()
                        .Filter($"startswith(DisplayName,'{sp.DisplayName}')")
                        .GetAsync().Result[0];
                    data["ObjectId"] = app.Id;
                    data["ApplicationId"] = app.AppId;
                }
                else if (type == "service principal")
                {
                    ServicePrincipal principal = graphClient.ServicePrincipals
                        .Request()
                        .Filter($"startswith(DisplayName,'{sp.DisplayName}')")
                        .GetAsync().Result[0];
                    data["ObjectId"] = principal.Id;
                }
                else
                {
                    // Unknown service principal, do nothing
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nERROR: " + e.Message);
            }

            return data;
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
