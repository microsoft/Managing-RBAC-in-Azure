using Microsoft.Azure.Management.Cdn.Fluent.Models;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
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

        internal static void checkChanges(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved)
        {
            int changes = 0;
            foreach(KeyVaultProperties kv in yamlVaults)
            {
                if (!vaultsRetrieved.Contains(kv))
                {
                    var old = vaultsRetrieved.ToLookup(k => k.VaultName)[kv.VaultName];
                    if(old.Count() != 0)
                    {
                        var oldVault = old.First();
                        foreach (PrincipalPermissions p in kv.AccessPolicies)
                        {
                            if (!oldVault.AccessPolicies.Contains(p))
                            {
                                changes++;
                            }
                        }
                        for (int i = 0; i < oldVault.AccessPolicies.Count; i++)
                        {
                            var oldPol = oldVault.AccessPolicies[i];
                            var name = oldPol.DisplayName;
                            var curr = kv.AccessPolicies.ToLookup(p => p.DisplayName)[name];
                            if (oldPol.Type.ToLower() == "user")
                            {
                                curr = kv.AccessPolicies.ToLookup(p => p.Alias)[oldPol.Alias];
                            }
                            if (curr.Count() == 0)
                            {
                                changes++;
                            }
                        }
                    }  
                }
            }
            if(changes > Constants.MAX_NUM_CHANGES)
            {
                Console.WriteLine($"You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed {changes} policies.");
                System.Environment.Exit(1);
            }
            foreach(KeyVaultProperties kv in vaultsRetrieved)
            {
                if(yamlVaults.ToLookup(v => v.VaultName)[kv.VaultName].Count() == 0)
                {
                    Console.WriteLine($"Key Vault, {kv.VaultName}, specified in the JSON file was not found in the YAML file.");
                }
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
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + " Vault Skipped.");
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
                                translatePermissions(sp, kv.VaultName);
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
                Console.WriteLine($"\nError: {e.InnerException.Message}");
            }
            return data;
        }

        /// <summary>
        /// This method translates all of the short-hand notations for Keys, Secrets, and Certificates to their respective permissions.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        /// <param name="vaultName">The name of the KeyVault you are updating</param>
        private static void translatePermissions(PrincipalPermissions sp, string vaultName)
        {
            // Convert all permissions to lowercase
            sp.PermissionsToKeys = sp.PermissionsToKeys.Select(s => s.ToLowerInvariant()).ToArray();
            sp.PermissionsToSecrets = sp.PermissionsToSecrets.Select(s => s.ToLowerInvariant()).ToArray();
            sp.PermissionsToCertificates = sp.PermissionsToCertificates.Select(s => s.ToLowerInvariant()).ToArray();

            checkValidPermissions(sp, vaultName);
            translateKeys(sp, vaultName);
            translateSecrets(sp, vaultName);
            translateCertificates(sp, vaultName);
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has valid permissions.
        /// </summary>
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        /// <param name="vaultName">The name of the KeyVault you are updating</param>
        private static void checkValidPermissions(PrincipalPermissions sp, string vaultName)
        {
            foreach (string kp in sp.PermissionsToKeys)
            {
                if (!Constants.VALID_KEY_PERMISSIONS.Contains(kp.ToLower()) && (!kp.ToLower().StartsWith("all")))
                {
                    throw new Exception($"Invalid key permission {kp} for {sp.DisplayName} in {vaultName}.");
                }
            }

            foreach (string s in sp.PermissionsToSecrets)
            {
                if (!Constants.VALID_SECRET_PERMISSIONS.Contains(s.ToLower()) && (!s.ToLower().StartsWith("all")))
                {
                    throw new Exception($"Invalid secret permission {s} for {sp.DisplayName} in {vaultName}.");
                }
            }

            foreach (string cp in sp.PermissionsToCertificates)
            {
                if (!Constants.VALID_CERTIFICATE_PERMISSIONS.Contains(cp.ToLower()) && (!cp.ToLower().StartsWith("all")))
                {
                    throw new Exception($"Invalid certificate permission {cp} for {sp.DisplayName} in {vaultName}.");
                }
            }
        }

        /// <summary>
        /// This method translates the short-hand notations for Keys to their respective permissions.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        /// <param name="vaultName">The name of the KeyVault you are updating</param>
        private static void translateKeys(PrincipalPermissions sp, string vaultName)
        {
            if (sp.PermissionsToKeys.Contains("all"))
            {
                if (sp.PermissionsToKeys.Length == 1)
                {
                    sp.PermissionsToKeys = Constants.ALL_KEY_PERMISSIONS;
                }
                else
                {
                    throw new Exception($"'All' permission removes need for other key permissions for {sp.DisplayName} in {vaultName}.");
                }
            }
            else
            {
                var allKeyword = sp.PermissionsToKeys.ToLookup(p => p.Trim().StartsWith("all"));
                if (allKeyword.Count > 0)
                {
                    string[] allMinusInstances = allKeyword[true].Where(val => val.Trim() != "all").ToArray();
                    if (allMinusInstances.Length == 1)
                    {
                        string inst = allMinusInstances[0];
                        const string minusLabel = "-";
                        int minusLabelStart = inst.IndexOf(minusLabel);
                        int start = minusLabelStart + minusLabel.Length;

                        string[] valuesToRemove = inst.Substring(start).Split(',').Select(p => p.Trim()).ToArray();
                     
                        // Verifies that each permission is valid
                        foreach (string p in valuesToRemove)
                        {
                            if (!Constants.VALID_KEY_PERMISSIONS.Contains(p.ToLower()))
                            {
                                throw new Exception($"Invalid 'All - <key permission>' {p} for {sp.DisplayName} in {vaultName}.");
                            }
                        }
                        sp.PermissionsToKeys = Constants.ALL_KEY_PERMISSIONS.Except(valuesToRemove).ToArray();
                    }
                    else if (allMinusInstances.Length > 1)
                    {
                        throw new Exception($"'All - <key permission>' is duplicated for {sp.DisplayName} in {vaultName}.");
                    }
                }

                if (sp.PermissionsToKeys.Contains("read"))
                {
                    var common = sp.PermissionsToKeys.Intersect(Constants.READ_KEY_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'get' and 'list' " +
                            $"permissions are already included in Key 'read' permission.");
                    }
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Concat(Constants.READ_KEY_PERMISSIONS).ToArray();
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Where(val => val != "read").ToArray();
                }

                if (sp.PermissionsToKeys.Contains("write"))
                {
                    var common = sp.PermissionsToKeys.Intersect(Constants.WRITE_KEY_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'delete', 'create' and 'update' " +
                            $"permissions are already included in Key 'write' permission.");
                    }
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Concat(Constants.WRITE_KEY_PERMISSIONS).ToArray();
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Where(val => val != "write").ToArray();
                }

                if (sp.PermissionsToKeys.Contains("crypto"))
                {
                    var common = sp.PermissionsToKeys.Intersect(Constants.CRYPTOGRAPHIC_KEY_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'decrypt', 'encrypt', 'unwrapkey', 'wrapkey', 'verify', 'sign' " +
                            $"permissions are already included in Key 'crypto' permission.");
                    }
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Concat(Constants.CRYPTOGRAPHIC_KEY_PERMISSIONS).ToArray();
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Where(val => val != "crypto").ToArray();
                }

                if (sp.PermissionsToKeys.Contains("storage"))
                {
                    var common = sp.PermissionsToKeys.Intersect(Constants.STORAGE_KEY_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'import', 'recover', 'backup', and 'restore' " +
                            $"permissions are already included in Key 'storage' permission.");
                    }
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Concat(Constants.STORAGE_KEY_PERMISSIONS).ToArray();
                    sp.PermissionsToKeys = sp.PermissionsToKeys.Where(val => val != "storage").ToArray();
                }
            }
        }

        /// <summary>
        /// This method translates the short-hand notations for Secrets to their respective permissions.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        /// <param name="vaultName">The name of the KeyVault you are updating</param>
        private static void translateSecrets(PrincipalPermissions sp, string vaultName)
        {
            if (sp.PermissionsToSecrets.Contains("all"))
            {
                if (sp.PermissionsToSecrets.Length == 1)
                {
                    sp.PermissionsToSecrets = Constants.ALL_SECRET_PERMISSIONS;
                }
                else
                {
                    throw new Exception($"'All' permission removes need for other secret permissions for {sp.DisplayName} in {vaultName}.");
                }
            }
            else
            {
                var allKeyword = sp.PermissionsToSecrets.ToLookup(p => p.Trim().StartsWith("all"));
                if (allKeyword.Count > 0)
                {
                    string[] allMinusInstances = allKeyword[true].Where(val => val.Trim() != "all").ToArray();
                    if (allMinusInstances.Length == 1)
                    {
                        string inst = allMinusInstances[0];
                        const string minusLabel = "-";
                        int minusLabelStart = inst.IndexOf(minusLabel);
                        int start = minusLabelStart + minusLabel.Length;

                        string[] valuesToRemove = inst.Substring(start).Split(',').Select(p => p.Trim()).ToArray();

                        // Verifies that each permission is valid
                        foreach (string p in valuesToRemove)
                        {
                            if (!Constants.VALID_SECRET_PERMISSIONS.Contains(p.ToLower()))
                            {
                                throw new Exception($"Invalid 'All - <secret permission>' {p} for {sp.DisplayName} in {vaultName}.");
                            }
                        }
                        sp.PermissionsToSecrets = Constants.ALL_SECRET_PERMISSIONS.Except(valuesToRemove).ToArray();
                    }
                    else if (allMinusInstances.Length > 1)
                    {
                        throw new Exception($"'All - <secret permission>' is duplicated for {sp.DisplayName} in {vaultName}.");
                    }
                }

                if (sp.PermissionsToSecrets.Contains("read"))
                {
                    var common = sp.PermissionsToSecrets.Intersect(Constants.READ_SECRET_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'get' and 'list' " +
                            $"permissions are already included in Secret 'read' permission.");
                    }
                    sp.PermissionsToSecrets = sp.PermissionsToSecrets.Concat(Constants.READ_SECRET_PERMISSIONS).ToArray();
                    sp.PermissionsToSecrets = sp.PermissionsToSecrets.Where(val => val != "read").ToArray();
                }

                if (sp.PermissionsToSecrets.Contains("write"))
                {
                    var common = sp.PermissionsToSecrets.Intersect(Constants.WRITE_SECRET_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'set' and 'delete' " +
                            $"permissions are already included in Secret 'write' permission.");
                    }
                    sp.PermissionsToSecrets = sp.PermissionsToSecrets.Concat(Constants.WRITE_SECRET_PERMISSIONS).ToArray();
                    sp.PermissionsToSecrets = sp.PermissionsToSecrets.Where(val => val != "write").ToArray();
                }

                if (sp.PermissionsToSecrets.Contains("storage"))
                {
                    var common = sp.PermissionsToKeys.Intersect(Constants.STORAGE_SECRET_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'recover', 'backup', and 'restore' " +
                            $"permissions are already included in Secret 'storage' permission.");
                    }
                    sp.PermissionsToSecrets = sp.PermissionsToSecrets.Concat(Constants.STORAGE_SECRET_PERMISSIONS).ToArray();
                    sp.PermissionsToSecrets = sp.PermissionsToSecrets.Where(val => val != "storage").ToArray();
                }
            }
        }

        /// <summary>
        /// This method translates the short-hand notations for Certificates to their respective permissions.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        /// <param name="vaultName">The name of the KeyVault you are updating</param>
        private static void translateCertificates(PrincipalPermissions sp, string vaultName)
        {
            if (sp.PermissionsToCertificates.Contains("all"))
            {
                if (sp.PermissionsToCertificates.Length == 1)
                {
                    sp.PermissionsToCertificates = Constants.ALL_CERTIFICATE_PERMISSIONS;
                }
                else
                {
                    throw new Exception($"'All' permission removes need for other certificate permissions for {sp.DisplayName} in {vaultName}.");
                }
            }
            else
            {
                var allKeyword = sp.PermissionsToCertificates.ToLookup(p => p.Trim().StartsWith("all"));
                if (allKeyword.Count > 0)
                {
                    string[] allMinusInstances = allKeyword[true].Where(val => val.Trim() != "all").ToArray();
                    if (allMinusInstances.Length == 1)
                    {
                        string inst = allMinusInstances[0];
                        const string minusLabel = "-";
                        int minusLabelStart = inst.IndexOf(minusLabel);
                        int start = minusLabelStart + minusLabel.Length;

                        string[] valuesToRemove = inst.Substring(start).Split(',').Select(p => p.Trim()).ToArray();

                        // Verifies that each permission is valid
                        foreach (string p in valuesToRemove)
                        {
                            if (!Constants.VALID_CERTIFICATE_PERMISSIONS.Contains(p.ToLower()))
                            {
                                throw new Exception($"Invalid 'All - <certificate permission>' {p} for {sp.DisplayName} in {vaultName}.");
                            }
                        }
                        sp.PermissionsToCertificates = Constants.ALL_CERTIFICATE_PERMISSIONS.Except(valuesToRemove).ToArray();
                    }
                    else if (allMinusInstances.Length > 1)
                    {
                        throw new Exception($"'All - <certificate permission>' is duplicated for {sp.DisplayName} in {vaultName}.");
                    }
                }

                if (sp.PermissionsToCertificates.Contains("read"))
                {
                    var common = sp.PermissionsToCertificates.Intersect(Constants.READ_CERTIFICATE_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'get' and 'list' " +
                            $"permissions are already included in Certificate 'read' permission.");
                    }
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Concat(Constants.READ_CERTIFICATE_PERMISSIONS).ToArray();
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Where(val => val != "read").ToArray();
                }

                if (sp.PermissionsToCertificates.Contains("write"))
                {
                    var common = sp.PermissionsToCertificates.Intersect(Constants.WRITE_CERTIFICATE_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'delete', 'create' and 'update' " +
                            $"permissions are already included in Certificate 'write' permission.");
                    }
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Concat(Constants.WRITE_CERTIFICATE_PERMISSIONS).ToArray();
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Where(val => val != "write").ToArray();
                }

                if (sp.PermissionsToCertificates.Contains("storage"))
                {
                    var common = sp.PermissionsToCertificates.Intersect(Constants.STORAGE_CERTIFICATE_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}. 'import', 'recover', 'backup', and 'restore' " +
                            $"permissions are already included in Certificate 'storage' permission.");
                    }
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Concat(Constants.STORAGE_CERTIFICATE_PERMISSIONS).ToArray();
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Where(val => val != "storage").ToArray();
                }

                if (sp.PermissionsToCertificates.Contains("manage"))
                {
                    var common = sp.PermissionsToCertificates.Intersect(Constants.MANAGE_CERTIFICATE_PERMISSIONS);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"Error for {sp.DisplayName} in {vaultName}.'managecontacts', 'manageissuers', 'getissuers', 'listissuers', 'setissuers', 'deleteissuers' " +
                            $"permissions are already included in Certificate 'manage' permission.");
                    }
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Concat(Constants.MANAGE_CERTIFICATE_PERMISSIONS).ToArray();
                    sp.PermissionsToCertificates = sp.PermissionsToCertificates.Where(val => val != "manage").ToArray();
                }
            }
        }
    }
}
