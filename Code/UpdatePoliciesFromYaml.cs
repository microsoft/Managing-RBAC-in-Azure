using Microsoft.Azure.Management.Cdn.Fluent.Models;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.PolicyAssignment.Definition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        /// This method reads in the Yaml file and stores the data in a list of KeyVaultProperties. If any of the fields are removed, throw an error.
        /// </summary>
        /// <param name="yamlDirectory"> The directory of the YAML file </param>
        /// <returns>The list of KeyVaultProperties if the input file has the correct formatting. Otherwise, exits the program.</returns>
        public static List<KeyVaultProperties> deserializeYaml(string yamlDirectory)
        {
            try
            {
                string yaml = System.IO.File.ReadAllText(yamlDirectory);
                var deserializer = new DeserializerBuilder().Build();
                List<KeyVaultProperties> yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);

                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    checkVaultInvalidFields(kv);
                    foreach (PrincipalPermissions sp in kv.AccessPolicies)
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
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has the necessary fields.
        /// </summary>
        /// <param name="name">The KeyVault name</param>
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        private static void checkSPInvalidFields(string name, PrincipalPermissions sp)
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
        }

        /// <summary>
        /// This method updates the access policies for each KeyVault in the yamlVaults list.
        /// </summary>
        /// <param name="yamlVaults">he list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        public static void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient, 
            Dictionary<string, string> secrets, GraphServiceClient graphClient)
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

                if (!vaultsRetrieved.Contains(kv))
                {
                    if (kv.usersContained() < Constants.MIN_NUM_USERS)
                    {
                        Console.WriteLine($"\nError: {kv.VaultName} does not contain at least two users. Vault skipped.");
                    }
                    else
                    {
                        Console.WriteLine("\nUpdating " + kv.VaultName + "...");
                        updateVault(kv, kvmClient, secrets, graphClient);
                    }
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
        private static void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets, 
            GraphServiceClient graphClient)
        {
            try
            {
                kvmClient.SubscriptionId = kv.SubscriptionId;

                VaultProperties properties = kvmClient.Vaults.GetAsync(kv.ResourceGroupName, kv.VaultName).Result.Properties;
                properties.AccessPolicies = new List<AccessPolicyEntry>();

                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                {
                    try
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
                                sp.PermissionsToKeys = sp.PermissionsToKeys.Select(s => s.ToLowerInvariant()).ToArray();
                                sp.PermissionsToSecrets = sp.PermissionsToSecrets.Select(s => s.ToLowerInvariant()).ToArray();
                                sp.PermissionsToCertificates = sp.PermissionsToCertificates.Select(s => s.ToLowerInvariant()).ToArray();

                                checkValidPermissions(sp);
                                translateShorthands(sp);

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
                    catch (Exception e)
                    {
                        Console.WriteLine($"\nError: {e.Message}");
                    }
                }

                Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
                Console.WriteLine($"{updatedVault.Name} successfully updated!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
            }
        }

        /// <summary>
        /// This method verifies that the ServicePrincipal exists and returns a dictionary that holds its data.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        /// <param name="type">The PrincipalPermissions type</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        /// <returns>A dictionary containing the service principal data</returns>
        private static Dictionary<string, string> verifyServicePrincipal(PrincipalPermissions sp, string type, GraphServiceClient graphClient)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            try
            {
                if (type == "user")
                {
                    if (sp.Alias == null || sp.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {sp.DisplayName}.");
                    }

                    User user = graphClient.Users[sp.Alias.ToLower().Trim()]
                        .Request()
                        .GetAsync().Result;
                    
                    if (sp.DisplayName.Trim().ToLower() != user.DisplayName.ToLower())
                    {
                        throw new Exception($"{sp.DisplayName} is misspelled and cannot be recognized. Service principal skipped.");
                    }
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
                    throw new Exception($"{sp.DisplayName} was deleted and no longer exists. Service principal skipped.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
            }
            return data;
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has valid permissions.
        /// </summary>
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        private static void checkValidPermissions(PrincipalPermissions sp)
        {
            foreach (string kp in sp.PermissionsToKeys)
            {
                if (!Constants.VALID_KEY_PERMISSIONS.Contains(kp.ToLower()) && (!kp.ToLower().StartsWith("all -")) && (!kp.ToLower().StartsWith("read -"))
                    && (!kp.ToLower().StartsWith("write -")) && (!kp.ToLower().StartsWith("storage -")) && (kp.ToLower().StartsWith("crypto - ")))
                {
                    throw new Exception($"Invalid key permission {kp}");
                }
            }

            foreach (string s in sp.PermissionsToSecrets)
            {
                if (!Constants.VALID_SECRET_PERMISSIONS.Contains(s.ToLower()) && (!s.ToLower().StartsWith("all -")) && (!s.ToLower().StartsWith("read -"))
                    && (!s.ToLower().StartsWith("write -")) && (!s.ToLower().StartsWith("storage -")))
                {
                    throw new Exception($"Invalid secret permission {s}");
                }
            }

            foreach (string cp in sp.PermissionsToCertificates)
            {
                if (!Constants.VALID_CERTIFICATE_PERMISSIONS.Contains(cp.ToLower()) && (!cp.ToLower().StartsWith("all -")) && (!cp.ToLower().StartsWith("read -")) 
                    && (!cp.ToLower().StartsWith("write -")) && (!cp.ToLower().StartsWith("storage -")) && (!cp.ToLower().StartsWith("management -")))
                {
                    throw new Exception($"Invalid certificate permission {cp}");
                }
            }
        }

        /// <summary>
        /// This method translates the shorthand notations for Keys, Secrets, and Certificates to their respective permissions.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        private static void translateShorthands(PrincipalPermissions sp)
        {
            sp.PermissionsToKeys = translateShorthand("all", "Key", sp.PermissionsToKeys, Constants.ALL_KEY_PERMISSIONS, 
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            sp.PermissionsToKeys = translateShorthand("read", "Key", sp.PermissionsToKeys, Constants.READ_KEY_PERMISSIONS, 
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            sp.PermissionsToKeys = translateShorthand("write", "Key", sp.PermissionsToKeys, Constants.WRITE_KEY_PERMISSIONS, 
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            sp.PermissionsToKeys = translateShorthand("storage", "Key", sp.PermissionsToKeys, Constants.STORAGE_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            sp.PermissionsToKeys = translateShorthand("crypto", "Key", sp.PermissionsToKeys, Constants.CRYPTOGRAPHIC_KEY_PERMISSIONS, 
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);

            sp.PermissionsToSecrets = translateShorthand("all", "secret", sp.PermissionsToSecrets, Constants.ALL_SECRET_PERMISSIONS, 
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            sp.PermissionsToSecrets = translateShorthand("read", "secret", sp.PermissionsToSecrets, Constants.READ_SECRET_PERMISSIONS,
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            sp.PermissionsToSecrets = translateShorthand("write", "secret", sp.PermissionsToSecrets, Constants.WRITE_SECRET_PERMISSIONS, 
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            sp.PermissionsToSecrets = translateShorthand("storage", "secret", sp.PermissionsToSecrets, Constants.STORAGE_SECRET_PERMISSIONS, 
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);

            sp.PermissionsToCertificates = translateShorthand("all", "certificate", sp.PermissionsToCertificates, Constants.ALL_CERTIFICATE_PERMISSIONS,
                Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            sp.PermissionsToCertificates = translateShorthand("read", "certificate", sp.PermissionsToCertificates, Constants.READ_CERTIFICATE_PERMISSIONS,
                Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            sp.PermissionsToCertificates = translateShorthand("write", "certificate", sp.PermissionsToCertificates, Constants.WRITE_CERTIFICATE_PERMISSIONS,
                Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            sp.PermissionsToCertificates = translateShorthand("storage", "certificate", sp.PermissionsToCertificates, Constants.STORAGE_CERTIFICATE_PERMISSIONS,
                Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            sp.PermissionsToCertificates = translateShorthand("management", "certificate", sp.PermissionsToCertificates, Constants.MANAGEMENT_CERTIFICATE_PERMISSIONS,
                Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
        }

        private static string[] translateShorthand(string shorthand, string permissionType, string[] permissions, string[] shorthandPermissions, string[] validPermissions, string[] shorthandWords)
        {
            var shorthandInstances = permissions.Where(val => val.Trim().StartsWith(shorthand)).ToArray();
            if (shorthandInstances.Length > 1)
            {
                throw new Exception($"{permissionType} '{shorthand}' permission is duplicated");
            }
            // Either contains 'shorthand' or 'shorthand -'
            else if (shorthandInstances.Length == 1)
            {
                string inst = shorthandInstances[0].Trim().ToLower();
                if (inst == shorthand)
                {
                    if (shorthand == "all" && permissions.Length != 1)
                    {
                        throw new Exception($"'All' permission removes need for other certificate permissions");
                    }

                    // Check for duplicates
                    var common = permissions.Intersect(shorthandPermissions);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"{string.Join(", ", shorthandPermissions)} permissions are already included in {permissionType} '{shorthand}' permission");
                    }
                    return permissions.Concat(shorthandPermissions).Where(val => val != shorthand).ToArray();
                } 
                // 'Shorthand -'
                else
                {
                    const string minusLabel = "-";
                    int minusLabelStart = inst.IndexOf(minusLabel);
                    int start = minusLabelStart + minusLabel.Length;

                    string[] valuesToRemove = inst.Substring(start).Split(',').Select(p => p.Trim().ToLower()).ToArray();
                    foreach (string p in valuesToRemove)
                    {
                        if (!validPermissions.Contains(p) || (!inst.StartsWith("all") && shorthandWords.Contains(p)))
                        {
                            throw new Exception($"Invalid {permissionType} '{shorthand} - <{p}>' permission");
                        }

                        // The remove value is a shorthand, then replace the shorthand with its permissions
                        if (shorthandWords.Contains(p)) 
                        {
                            string[] valuesToReplace = getShorthandPermissions(p, permissionType);
                            valuesToRemove = valuesToRemove.Concat(valuesToReplace).Where(val => val != p).ToArray();
                        }
                    }
                    var permissionsToGrant = shorthandPermissions.Except(valuesToRemove);

                    // Check for duplicates
                    var common = permissions.Intersect(permissionsToGrant);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"{string.Join(", ", valuesToRemove)} permissions are already included in {permissionType} '{shorthand}' permission");
                    }
                    return (permissions.Concat(permissionsToGrant).Where(val => val != inst).ToArray());
                }
            }
            return permissions;
        }

        /// <summary>
        /// This method returns the shorthand permissions that correspond to the shorthand keyword.
        /// </summary>
        /// <param name="shorthand">The shorthand keyword to analyze</param>
        /// <param name="permissionType">The type of permission block</param>
        /// <returns></returns>
        private static string[] getShorthandPermissions(string shorthand, string permissionType)
        {
            if (shorthand == "all")
            {
                throw new Exception("Cannot remove 'all' from a permission");
            }
            else
            {
                if (permissionType == "key")
                {
                    if (shorthand == "read")
                    {
                        return Constants.READ_KEY_PERMISSIONS;
                    }
                    else if (shorthand == "write")
                    {
                        return Constants.WRITE_KEY_PERMISSIONS;
                    }
                    else if (shorthand == "storage")
                    {
                        return Constants.STORAGE_KEY_PERMISSIONS;
                    }
                    else if (shorthand == "crypto")
                    {
                        return Constants.CRYPTOGRAPHIC_KEY_PERMISSIONS;
                    }
                }
                else if (permissionType == "secret")
                {
                    if (shorthand == "read")
                    {
                        return Constants.READ_SECRET_PERMISSIONS;
                    }
                    else if (shorthand == "write")
                    {
                        return Constants.WRITE_SECRET_PERMISSIONS;
                    }
                    else if (shorthand == "storage")
                    {
                        return Constants.STORAGE_SECRET_PERMISSIONS;
                    }
                }
                else //certificate
                {
                    if (shorthand == "read")
                    {
                        return Constants.READ_CERTIFICATE_PERMISSIONS;
                    }
                    else if (shorthand == "write")
                    {
                        return Constants.WRITE_CERTIFICATE_PERMISSIONS;
                    }
                    else if (shorthand == "storage")
                    {
                        return Constants.STORAGE_CERTIFICATE_PERMISSIONS;
                    }
                    else if (shorthand == "management")
                    {
                        return Constants.MANAGEMENT_CERTIFICATE_PERMISSIONS;
                    }
                }
            }
            return null;
        }
    }
}
