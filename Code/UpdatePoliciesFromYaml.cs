using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.PolicyAssignment.Definition;
using Microsoft.Graph;
using Namotion.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using YamlDotNet.Serialization;

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
                try
                {
                    checkVaultChanges(vaultsRetrieved, kv);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    System.Environment.Exit(1);
                }
                
                if (kv.usersContained() < 2)
                {
                    Console.WriteLine($"\nError: {kv.VaultName} does not contain at least two users. Vault skipped.");
                }
                else if (!vaultsRetrieved.Contains(kv))
                {
                    Console.WriteLine("\nUpdating " + kv.VaultName + "...");
                    updateVault(kv, kvmClient, secrets, graphClient);
                }
            }
        }

        /// <summary>
        /// This method throws an error if any of the fields for a KeyVault have been changed in the Yaml, other than the AccessPolicies.
        /// </summary>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <param name="kv">The current KeyVault</param>
        private static void checkVaultChanges(List<KeyVaultProperties> vaultsRetrieved, KeyVaultProperties kv)
        {
            var lookupName = vaultsRetrieved.ToLookup(kv => kv.VaultName);
            if (lookupName[kv.VaultName].ToList().Count != 1)
            {
                throw new Exception($"\nError: VaultName {kv.VaultName} was changed or added.");
            }

            // If Key Vault name was correct, then check the other fields
            KeyVaultProperties originalKV = lookupName[kv.VaultName].ToList()[0];
            if (originalKV.ResourceGroupName != kv.ResourceGroupName.Trim())
            {
                throw new Exception($"\nError: ResourceGroupName for {kv.VaultName} was changed.");
            }
            if (originalKV.SubscriptionId != kv.SubscriptionId.Trim())
            {
                throw new Exception($"\nError: SubscriptionId for {kv.VaultName} was changed.");
            }
            if (originalKV.Location != kv.Location.Trim())
            {
                throw new Exception($"\nError: Location for {kv.VaultName} was changed.");
            }
            if (originalKV.TenantId != kv.TenantId.Trim())
            {
                throw new Exception($"\nError: TenantId for {kv.VaultName} was changed.");
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
            try
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
                            properties.AccessPolicies.Add(new AccessPolicyEntry(new Guid(secrets["tenantId"]), sp.ObjectId,
                                    new Permissions(sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates)));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"\nError: {e.Message} for {sp.DisplayName} in {kv.VaultName}.");
                            System.Environment.Exit(1);
                        }
                    }
                }

                Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
                Console.WriteLine("" + updatedVault.Name + " successfully updated!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
            }
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
                    if (sp.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {sp.DisplayName}.");
                    }

                    User user = graphClient.Users[sp.Alias.ToLower().Trim()]
                        .Request()
                        .GetAsync().Result;
                    data["ObjectId"] = user.Id;

                    if (sp.DisplayName.Trim().ToLower() != user.DisplayName.ToLower())
                    {
                        throw new Exception($"{sp.DisplayName} could not be found.");
                    }
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
                Console.WriteLine($"\nError: {e.Message}");
            }

            return data;
        }

        /// <summary>
        /// This method verifies that each ServicePrincipal has the necessary fields and valid permissions.
        /// </summary>
        /// <param name="name">The Key Vault name</param>
        /// <param name="sp">The ServicePrincipalPermissions for which we want to validate</param>
        /// <returns>True if all of the permission entries are valid and are defined. Otherwise, returns throws an exception.</returns>
        private static bool checkSPInvalidFields(string name, ServicePrincipalPermissions sp)
        {
            if (sp.Type == null)
            {
                throw new Exception($"\nMissing Type for {name}");
            }
            if (sp.DisplayName == null)
            {
                throw new Exception($"\nMissing DisplayName for {name}");
            }
            if (sp.PermissionsToKeys == null)
            {
                throw new Exception($"\nMissing PermissionsToKeys for {name}");
            }
            if (sp.PermissionsToSecrets == null)
            {
                throw new Exception($"\nMissing PermissionsToSecrets for {name}");
            }
            if (sp.PermissionsToCertificates == null)
            {
                throw new Exception($"\nMissing PermissionsToCertificates for {name}");
            }

            foreach (string kp in sp.PermissionsToKeys)
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

        /// <summary>
        /// This method verifies that each KeyVault has the necessary fields and were not deleted from the Yaml.
        /// </summary>
        /// <param name="kv">The current KeyVaultProperties object</param>
        private static void checkVaultInvalidFields(KeyVaultProperties kv)
        {
            if (kv.VaultName == null)
            {
                throw new Exception($"\nMissing VaultName for {kv.VaultName}");
            }
            if (kv.ResourceGroupName == null)
            {
                throw new Exception($"\nMissing ResourceGroupName for {kv.VaultName}");
            }
            if (kv.SubscriptionId == null)
            {
                throw new Exception($"\nMissing SubscriptionId for {kv.VaultName}");
            }
            if (kv.Location == null)
            {
                throw new Exception($"\nMissing Location for {kv.VaultName}");
            }
            if (kv.TenantId == null)
            {
                throw new Exception($"\nMissing TenantId for {kv.VaultName}");
            }

            // foreach (ServicePrincipalPermissions sp in kv.AccessPolicies)
            // {
            //     checkSPInvalidFields(kv.VaultName, sp);
            // }
        }

        /// <summary>
        /// This method reads in the Yaml file and stores the data in a list of KeyVaultProperties. If any of the fields are removed, throw an error.
        /// </summary>
        /// <returns>The list of KeyVaultProperties if the input file has the correct formatting. Otherwise, exits the program.</returns>
        public static List<KeyVaultProperties> deserializeYaml()
        {
            try
            {
                string yaml = System.IO.File.ReadAllText(@"..\..\..\..\Config\YamlOutput.yml");
                var deserializer = new DeserializerBuilder().Build();
                List<KeyVaultProperties> yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);

                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    checkVaultInvalidFields(kv);

                    foreach (ServicePrincipalPermissions sp in kv.AccessPolicies)
                    {
                        checkSPInvalidFields(kv.VaultName, sp);
                    }

                }
                return yamlVaults;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
                System.Environment.Exit(1);
                return null;
            }
        }
    }
}
