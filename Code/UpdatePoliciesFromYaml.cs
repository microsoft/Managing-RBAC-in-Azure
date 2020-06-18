using Microsoft.Azure.Management.Cdn.Fluent.Models;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.PolicyAssignment.Definition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Graph;
using Microsoft.Rest.Azure;
using Namotion.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using YamlDotNet.Serialization;
using System.Text;
using System.IO;

namespace RBAC
{
    public class UpdatePoliciesFromYaml
    {
        public UpdatePoliciesFromYaml(bool testing)
        {
            Testing = testing;
        }

        /// <summary>
        /// This method reads in the Yaml file and stores the data in a list of KeyVaultProperties. If any of the fields are removed, throw an error.
        /// </summary>
        /// <param name="yamlDirectory"> The directory of the YAML file </param>
        /// <returns>The list of KeyVaultProperties if the input file has the correct formatting. Otherwise, exits the program.</returns>
        public List<KeyVaultProperties> deserializeYaml(string yamlDirectory)
        {
            try
            {
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Deserializing Code");
                string yaml = System.IO.File.ReadAllText(yamlDirectory);
                var deserializer = new DeserializerBuilder().Build();
                List<KeyVaultProperties> yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);

                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Checking valid fields");
                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    checkVaultInvalidFields(kv);
                    foreach (PrincipalPermissions sp in kv.AccessPolicies)
                    {
                        checkSPInvalidFields(kv.VaultName, sp);
                    }
                }
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Finished Deserializing Code");
                return yamlVaults;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Deserialization FAILED");
                Constants.getLog().Flush();
                Constants.getLog().Close();

                System.Environment.Exit(1);
                return null;
            }
        }

        /// <summary>
        /// This method checks that the amount of changes made do not exceed the maximum number of changes defined in the Constants file.
        /// </summary>
        /// <param name="yamlVaults">The list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <returns>The number of changes made</returns>
        public int checkChanges(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved)
        {
            int changes = 0;
            foreach (KeyVaultProperties kv in yamlVaults)
            {
                if (!vaultsRetrieved.Contains(kv))
                {
                    var old = vaultsRetrieved.ToLookup(k => k.VaultName)[kv.VaultName];

                    if (old.Count() != 0)
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

            if (changes > Constants.MAX_NUM_CHANGES)
            {
                Console.WriteLine($"Error: You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed {changes} policies.");
                System.Environment.Exit(1);
            }

            foreach (KeyVaultProperties kv in vaultsRetrieved)
            {
                if (yamlVaults.ToLookup(v => v.VaultName)[kv.VaultName].Count() == 0)
                {
                    Console.WriteLine($"Error: KeyVault, {kv.VaultName}, specified in the JSON file was not found in the YAML file.");
                    System.Environment.Exit(1);
                }
            }

            foreach (KeyVaultProperties kv in yamlVaults)
            {
                if (vaultsRetrieved.ToLookup(v => v.VaultName)[kv.VaultName].Count() == 0)
                {
                    Console.WriteLine($"Error: KeyVault, {kv.VaultName}, in the YAML file was not found in the JSON file.");
                    System.Environment.Exit(1);
                }
            }
            return changes;
        }

        /// <summary>
        /// This method verifies that each KeyVault has the necessary fields and were not deleted from the Yaml.
        /// </summary>
        /// <param name="kv">The current KeyVaultProperties object</param>
        public void checkVaultInvalidFields(KeyVaultProperties kv)
        {
            if (kv.VaultName == null || kv.VaultName.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing VaultName for {kv.VaultName}");
                throw new Exception($"Missing VaultName for {kv.VaultName}");
            }
            if (kv.ResourceGroupName == null || kv.ResourceGroupName.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing ResourceGroupName for {kv.VaultName}");
                throw new Exception($"Missing ResourceGroupName for {kv.VaultName}");
            }
            if (kv.SubscriptionId == null || kv.SubscriptionId.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing SubscriptionId for {kv.VaultName}");
                throw new Exception($"Missing SubscriptionId for {kv.VaultName}");
            }
            if (kv.Location == null || kv.Location.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing Location for {kv.VaultName}");
                throw new Exception($"Missing Location for {kv.VaultName}");
            }
            if (kv.TenantId == null || kv.TenantId.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing TenantId for {kv.VaultName}");
                throw new Exception($"Missing TenantId for {kv.VaultName}");
            }
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has the necessary fields.
        /// </summary>
        /// <param name="name">The KeyVault name</param>
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        public void checkSPInvalidFields(string name, PrincipalPermissions sp)
        {
            if (sp.Type == null || sp.Type.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing Type for {name}");
                throw new Exception($"Missing Type for {name}");
            }
            if (sp.DisplayName == null || sp.DisplayName.Trim() == "")
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing DisplayName for {name}");
                throw new Exception($"Missing DisplayName for {name}");
            }
            if (sp.PermissionsToKeys == null)
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing PermissionsToKeys for {name}");
                throw new Exception($"Missing PermissionsToKeys for {name}");
            }
            if (sp.PermissionsToSecrets == null)
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing PermissionToSecrets for {name}");
                throw new Exception($"Missing PermissionsToSecrets for {name}");
            }
            if (sp.PermissionsToCertificates == null)
            {
                // log.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + $"\nMissing PermissionToCertificates for {name}");
                throw new Exception($"Missing PermissionsToCertificates for {name}");
            }
        }

        /// <summary>
        /// This method updates the access policies for each KeyVault in the yamlVaults list.
        /// </summary>
        /// <param name="yamlVaults">The list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        public void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient,
            Dictionary<string, string> secrets, GraphServiceClient graphClient)
        {
            Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Checking vault changes & updating vault");
            foreach (KeyVaultProperties kv in yamlVaults)
            {
                try
                {
                    checkVaultChanges(vaultsRetrieved, kv);
                    if (!vaultsRetrieved.Contains(kv))
                    {
                        if (kv.usersContained() < Constants.MIN_NUM_USERS)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{kv.VaultName} does not contain at least two users.");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine("Updating " + kv.VaultName + "...");
                            updateVault(kv, kvmClient, secrets, graphClient);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {e.Message} Vault Skipped.");
                    Console.ResetColor();
                }
                Constants.getLog().WriteLine(DateTime.Now.ToString("MM/dd/yyyy") + " " + DateTime.Now.ToString("h:mm:ss.fff tt") + ": Logging finished...");
                Constants.getLog().Flush();
            }
        }

        /// <summary>
        /// This method throws an error if any of the fields for a KeyVault have been changed in the Yaml, other than the AccessPolicies.
        /// </summary>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <param name="kv">The current KeyVault</param>
        public void checkVaultChanges(List<KeyVaultProperties> vaultsRetrieved, KeyVaultProperties kv)
        {
            var lookupName = vaultsRetrieved.ToLookup(kv => kv.VaultName);
            if (lookupName[kv.VaultName].ToList().Count != 1)
            {
                throw new Exception($"VaultName {kv.VaultName} was changed or added.");
            }

            // If Key Vault name was correct, then check the other fields
            KeyVaultProperties originalKV = lookupName[kv.VaultName].ToList()[0];
            if (originalKV.ResourceGroupName != kv.ResourceGroupName.Trim())
            {
                throw new Exception($"ResourceGroupName for {kv.VaultName} was changed.");
            }
            if (originalKV.SubscriptionId != kv.SubscriptionId.Trim())
            {
                throw new Exception($"SubscriptionId for {kv.VaultName} was changed.");
            }
            if (originalKV.Location != kv.Location.Trim())
            {
                throw new Exception($"Location for {kv.VaultName} was changed.");
            }
            if (originalKV.TenantId != kv.TenantId.Trim())
            {
                throw new Exception($"TenantId for {kv.VaultName} was changed.");
            }
        }

        /// <summary>
        /// This method updates the access policies of the specified KeyVault in Azure.
        /// </summary>
        /// <param name="kv">The KeyVault you want to update</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        private void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets,
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
                        int total = sp.PermissionsToCertificates.Length + sp.PermissionsToKeys.Length + sp.PermissionsToSecrets.Length;
                        if (total != 0)
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
                                    if (type == "user" && kv.AccessPolicies.ToLookup(v => v.Alias)[sp.Alias].Count() > 1 ||
                                    type != "user" && kv.AccessPolicies.ToLookup(v => v.DisplayName)[sp.DisplayName].Count() > 1)
                                    {
                                        throw new Exception($"An access policy has already been defined");
                                    }

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
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Error: {e.Message} for {sp.DisplayName} in {kv.VaultName}.");
                                    Console.ResetColor();
                                    System.Environment.Exit(1);
                                }
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Skipped {sp.Type}, '{sp.DisplayName}'. Does not have any permissions specified.");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {e.Message}");
                        Console.ResetColor();
                    }
                }

                Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
                Console.WriteLine($"{updatedVault.Name} successfully updated!");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// This method verifies that the ServicePrincipal exists and returns a dictionary that holds its data.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        /// <param name="type">The PrincipalPermissions type</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        /// <returns>A dictionary containing the service principal data</returns>
        private Dictionary<string, string> verifyServicePrincipal(PrincipalPermissions sp, string type, GraphServiceClient graphClient)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (type == "user")
            {
                try
                {
                    if (sp.Alias == null || sp.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {sp.DisplayName}. User skipped.");
                    }

                    User user = graphClient.Users[sp.Alias.ToLower().Trim()]
                    .Request()
                    .GetAsync().Result;

                    if (sp.DisplayName.Trim().ToLower() != user.DisplayName.ToLower())
                    {
                        throw new Exception($"The DisplayName '{sp.DisplayName}' is misspelled and cannot be recognized. User skipped.");
                    }
                    data["ObjectId"] = user.Id;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("ResourceNotFound"))
                    {
                        Console.WriteLine($"Error: Could not find User with Alias '{sp.Alias}'. User skipped.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                    Console.ResetColor();
                }
            }
            else if (type == "group")
            {
                try
                {
                    Group group = graphClient.Groups
                    .Request()
                    .Filter($"startswith(DisplayName,'{sp.DisplayName}')")
                    .GetAsync().Result[0];

                    if (sp.Alias != null && sp.Alias.Trim().ToLower() != group.Mail.ToLower())
                    {
                        throw new Exception($"The Alias '{sp.Alias}' is incorrect for {sp.DisplayName} and cannot be recognized. Group skipped.");
                    }
                    data["ObjectId"] = group.Id;
                    data["Alias"] = group.Mail;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("out of range"))
                    {
                        Console.WriteLine($"Error: Could not find Group with DisplayName '{sp.DisplayName}'. Group skipped.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                    Console.ResetColor();
                }
            }
            else if (type == "application")
            {
                try
                {
                    Application app = graphClient.Applications
                    .Request()
                    .Filter($"startswith(DisplayName,'{sp.DisplayName}')")
                    .GetAsync().Result[0];

                    if (sp.Alias != null || sp.Alias.Trim().Length != 0)
                    {
                        throw new Exception($"The Alias '{sp.Alias}' should not be defined and cannot be recognized for {sp.DisplayName}. Application skipped.");
                    }
                    data["ObjectId"] = app.Id;
                    data["ApplicationId"] = app.AppId;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("out of range"))
                    {
                        Console.WriteLine($"Error: Could not find Application with DisplayName '{sp.DisplayName}'. Application skipped.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                    Console.ResetColor();
                }
            }
            else if (type == "service principal")
            {
                try
                {
                    ServicePrincipal principal = graphClient.ServicePrincipals
                        .Request()
                        .Filter($"startswith(DisplayName,'{sp.DisplayName}')")
                        .GetAsync().Result[0];

                    if (sp.Alias != null || sp.Alias.Trim().Length != 0)
                    {
                        throw new Exception($"The Alias '{sp.Alias}' should not be defined and cannot be recognized for {sp.DisplayName}. ServicePrincipal skipped.");
                    }
                    data["ObjectId"] = principal.Id;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("out of range"))
                    {
                        Console.WriteLine($"Error: Could not find ServicePrincipal with DisplayName '{sp.DisplayName}'. Service Principal skipped.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                    Console.ResetColor();
                }
            }
            else
            {
                throw new Exception($"'{sp.Type}' is not a valid type for {sp.DisplayName}. Valid types are 'User', 'Group', 'Application', or 'Service Principal'. Skipped.");
            }
            return data;
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has valid permissions and does not contain duplicate permissions.
        /// </summary>
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        public void checkValidPermissions(PrincipalPermissions sp)
        {
            foreach (string kp in sp.PermissionsToKeys)
            {
                if (!Constants.VALID_KEY_PERMISSIONS.Contains(kp.ToLower()) && (!kp.ToLower().StartsWith("all -")) && (!kp.ToLower().StartsWith("read -"))
                    && (!kp.ToLower().StartsWith("write -")) && (!kp.ToLower().StartsWith("storage -")) && (kp.ToLower().StartsWith("crypto - ")))
                {
                    throw new Exception($"Invalid key permission '{kp}'");
                }
            }

            foreach (string s in sp.PermissionsToSecrets)
            {
                if (!Constants.VALID_SECRET_PERMISSIONS.Contains(s.ToLower()) && (!s.ToLower().StartsWith("all -")) && (!s.ToLower().StartsWith("read -"))
                    && (!s.ToLower().StartsWith("write -")) && (!s.ToLower().StartsWith("storage -")))
                {
                    throw new Exception($"Invalid secret permission '{s}'");
                }
            }

            foreach (string cp in sp.PermissionsToCertificates)
            {
                if (!Constants.VALID_CERTIFICATE_PERMISSIONS.Contains(cp.ToLower()) && (!cp.ToLower().StartsWith("all -")) && (!cp.ToLower().StartsWith("read -"))
                    && (!cp.ToLower().StartsWith("write -")) && (!cp.ToLower().StartsWith("storage -")) && (!cp.ToLower().StartsWith("management -")))
                {
                    throw new Exception($"Invalid certificate permission '{cp}'");
                }
            }

            if (sp.PermissionsToKeys.Distinct().Count() != sp.PermissionsToKeys.Count())
            {
                List<string> duplicates = findDuplicates(sp.PermissionsToKeys);
                throw new Exception($"Key permission(s) '{string.Join(", ", duplicates)}' repeated");
            }
            if (sp.PermissionsToSecrets.Distinct().Count() != sp.PermissionsToSecrets.Count())
            {
                List<string> duplicates = findDuplicates(sp.PermissionsToSecrets);
                throw new Exception($"Secret permission(s) '{string.Join(", ", duplicates)}' repeated");
            }
            if (sp.PermissionsToCertificates.Distinct().Count() != sp.PermissionsToCertificates.Count())
            {
                List<string> duplicates = findDuplicates(sp.PermissionsToCertificates);
                throw new Exception($"Certificate permission(s) '{string.Join(", ", duplicates)}' repeated");
            }
        }

        /// <summary>
        /// This method finds and returns the duplicate values in a permission block.
        /// </summary>
        /// <param name="permissions">The permission block for which we want to find the duplicate values</param>
        /// <returns>A list of the duplicated values</returns>
        private List<string> findDuplicates(string[] permissions)
        {
            List<string> duplicates = new List<string>();
            for (int i = 0; i < permissions.Length; ++i)
            {
                for (int j = i + 1; j < permissions.Length; ++j)
                {
                    if (permissions[i].Trim().ToLower() == permissions[j].Trim().ToLower())
                    {
                        duplicates.Add(permissions[i]);
                    }
                }
            }
            return duplicates;
        }

        /// <summary>
        /// This method translates the shorthand notations for Keys, Secrets, and Certificates to their respective permissions.
        /// </summary>
        /// <param name="sp">The current PrincipalPermissions object</param>
        private void translateShorthands(PrincipalPermissions sp)
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

        /// <summary>
        /// This method translates the specified shorthand to its respective permissions.
        /// </summary>
        /// <param name="shorthand">The shorthand keyword to analyze</param>
        /// <param name="permissionType">The type of permission block</param>
        /// <param name="permissions">The permission block array</param>
        /// <param name="shorthandPermissions">The shorthand permissions array</param>
        /// <param name="validPermissions">The array of all valid permissions</param>
        /// <param name="shorthandWords">The valid shorthand keywords array</param>
        /// <returns>A string array that has replaced the shorthands with their respective permissions</returns>
        private string[] translateShorthand(string shorthand, string permissionType, string[] permissions, string[] shorthandPermissions, string[] validPermissions, string[] shorthandWords)
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
        /// <returns>A string array of the shorthand permissions that correspond to the shorthand keyword</returns>
        private string[] getShorthandPermissions(string shorthand, string permissionType)
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

        public void Exit(string message)
        {
            if (!Testing)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
                Constants.getLog().Close();
                Environment.Exit(1);
            }
            else
            {
                throw new Exception($"{message}");
            }
        }
        public bool Testing { get; set; }
    }
}
