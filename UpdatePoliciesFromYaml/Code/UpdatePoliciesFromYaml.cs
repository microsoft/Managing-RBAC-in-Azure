using log4net.Util;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace RBAC
{
    public class UpdatePoliciesFromYaml
    {
        /// <summary>
        /// Constructor to create an instance of the UpdatePoliciesFromYaml class for use in Unit Testing.
        /// </summary>
        /// <param name="testing">True if unit tests are being run. Otherwise, false.</param>
        public UpdatePoliciesFromYaml(bool testing)
        {
            Testing = testing;
            Changed = new List<KeyVaultProperties>();
        }

        /// <summary>
        /// This method reads in the Yaml file and stores the data in a list of KeyVaultProperties. If any of the fields are removed, throw an error.
        /// </summary>
        /// <returns>The list of KeyVaultProperties if the input file has the correct formatting. Otherwise, exits the program.</returns>
        /// <param name="yamlDirectory">The directory of the yaml file</param>
        public List<KeyVaultProperties> deserializeYaml(string yamlDirectory)
        {
            List<KeyVaultProperties> yamlVaults = new List<KeyVaultProperties>();
            try
            {
                log.Info("Reading YAML file...");
                string yaml = System.IO.File.ReadAllText(yamlDirectory);
                log.Info("YAML successfully read!");
                log.Info("Deserializing YAML file...");
                var deserializer = new DeserializerBuilder().Build();
                yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);
                log.Info("YAML successfully deserialized!");
            }
            catch (Exception e)
            {
                log.Error($"DeserializationFail", e);
                log.Debug("Refer to the YamlSample.yml (https://github.com/microsoft/Managing-RBAC-in-Azure/blob/Katie/Config/YamlSample.yml) for questions on " +
                    "formatting and inputs. Ensure that you have all the required fields with valid values, then try again.");
                Exit(e.Message);
            }
            try
            {
                log.Info("Checking for null or empty fields...");
                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    checkVaultInvalidFields(kv);
                    foreach (PrincipalPermissions principalPermissions in kv.AccessPolicies)
                    {
                        if (principalPermissions.Type.ToLower() == "unknown")
                        {
                            log.Error($"UnknownPrincipal");
                            log.Debug($"There is a policy of type 'Unknown' within KeyVault '{kv.VaultName}', meaning that this principal has recently been deleted from the Tenant. " +
                                $"Please remove this policy and re-run.");
                            Exit($"Principal policy of Type 'Unknown' was found in KeyVault '{kv.VaultName}'.");
                        }
                        checkPPInvalidFields(kv.VaultName, principalPermissions);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"InvalidFields", e);
                log.Debug("Please add or modify the specified field. 'VaultName', 'ResourceGroupName', 'SubscriptionId', 'Location', 'TenantId', and 'AccessPolicies' " +
                    "should be defined for each KeyVault. For more information on the fields required for each Security Principal in 'AccessPolicies', refer to the " +
                    "'Editing the Access Policies' section: https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/README.md");
                Exit(e.Message);
            }
            log.Info("Fields validated!");
            return yamlVaults;
        }

        /// <summary>
        /// This method checks for KeyVault additions or deletions as well as any fields in the KeyVaults that have changed, other than the AccessPolicies.
        /// </summary>
        /// <param name="yamlVaults">The list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        public void checkVaultChanges(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved)
        {
            try
            {
                log.Info("Checking for KeyVault changes...");
                foreach (KeyVaultProperties kv in vaultsRetrieved)
                {
                    if (yamlVaults.ToLookup(v => v.VaultName)[kv.VaultName].Count() == 0)
                    {
                        log.Error($"VaultDeleted");
                        log.Debug($"KeyVault '{kv.VaultName}' specified in the .json file was deleted from the .yml file! Please re-add this KeyVault or re-run " +
                            $"AccessPoliciesToYamlProgram.cs to retrieve the full list of KeyVaults.");
                        throw new Exception($"KeyVault '{kv.VaultName}' specified in the JSON file was not found in the YAML file.");
                    }
                }

                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    var lookup = vaultsRetrieved.ToLookup(kv => kv.VaultName)[kv.VaultName];
                    if (lookup.Count() == 0)
                    {
                        log.Error($"VaultAdded");
                        log.Debug($"KeyVault '{kv.VaultName}' was not specified in the JSON file and was added to the YAML file! " +
                            $"Check if the 'VaultName' for KeyVault '{kv.VaultName}' has been changed, or, if you added this KeyVault, please remove it before trying again.");
                        throw new Exception($"KeyVault '{kv.VaultName}' in the YAML file was not found in the JSON file.");
                    }
                    else
                    {
                        KeyVaultProperties originalVault = lookup.First();
                        if (originalVault.ResourceGroupName != kv.ResourceGroupName.Trim())
                        {
                            log.Error($"VaultFieldsChanged");
                            log.Debug("Changes made to any fields other than the 'AccessPolicies' field are prohibited. Please modify the specified field.");
                            throw new Exception($"ResourceGroupName for KeyVault '{kv.VaultName}' was changed.");
                        }
                        if (originalVault.SubscriptionId != kv.SubscriptionId.Trim())
                        {
                            log.Error($"VaultFieldsChanged");
                            log.Debug("Changes made to any fields other than the 'AccessPolicies' field are prohibited. Please modify the specified field.");
                            throw new Exception($"SubscriptionId for KeyVault '{kv.VaultName}' was changed.");
                        }
                        if (originalVault.Location != kv.Location.Trim())
                        {
                            log.Error($"VaultFieldsChanged");
                            log.Debug("Changes made to any fields other than the 'AccessPolicies' field are prohibited. Please modify the specified field.");
                            throw new Exception($"Location for KeyVault '{kv.VaultName}' was changed.");
                        }
                        if (originalVault.TenantId != kv.TenantId.Trim())
                        {
                            log.Error($"VaultFieldsChanged");
                            log.Debug("Changes made to any fields other than the 'AccessPolicies' field are prohibited. Please modify the specified field.");
                            throw new Exception($"TenantId for KeyVault '{kv.VaultName}' was changed.");
                        }
                    }
                }
                log.Info("Changes checked successfully!");
            }
            catch (Exception e)
            {
                Exit(e.Message);
            }
        }

        /// <summary>
        /// This method gets the number of edits made in the Yaml, verifies that only one access policy exists for each principal per KeyVault, 
        /// validates a security principal's permissions, and translates the shorthand keywords.
        /// </summary>
        /// <param name="yamlVaults">The list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <returns>A Tuple with the first item being the list of KeyVaultProperties to write to the DeletedPolicies.yml and the second item being the number of changes made</returns>
        public Tuple<List<KeyVaultProperties>, int> getChanges(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved)
        {
            List<KeyVaultProperties> deletedVaultPolicies = new List<KeyVaultProperties>();
            int changes = 0;
            foreach (KeyVaultProperties kv in yamlVaults)
            {
                List<PrincipalPermissions> deletedPolicies = new List<PrincipalPermissions>();
                if (!vaultsRetrieved.Contains(kv))
                {
                    var oldVault = vaultsRetrieved.ToLookup(k => k.VaultName)[kv.VaultName].First();
                    List<PrincipalPermissions> portalPolicies = oldVault.AccessPolicies;

                    // Check if the access policy was deleted
                    foreach (PrincipalPermissions oldPolicy in portalPolicies)
                    {
                        IEnumerable<PrincipalPermissions> newPolicy;
                        if (oldPolicy.Type.ToLower() == "user" || oldPolicy.Type.ToLower() == "group")
                        {
                            newPolicy = kv.AccessPolicies.ToLookup(pp => pp.Alias)[oldPolicy.Alias];
                        }
                        else
                        {
                            newPolicy = kv.AccessPolicies.ToLookup(pp => pp.DisplayName)[oldPolicy.DisplayName];
                        }
                        if (newPolicy.Count() == 0)
                        {
                            changes++;
                        }
                    }

                    foreach (PrincipalPermissions principalPermissions in kv.AccessPolicies)
                    {
                        string type = principalPermissions.Type.ToLower().Trim();
                        log.Info($"Verifying that the access policy for {principalPermissions.DisplayName} with Alias '{principalPermissions.Alias}' is unique...");
                        if (((type == "user" || type == "group") && kv.AccessPolicies.ToLookup(pp => pp.Alias)[principalPermissions.Alias].Count() > 1) ||
                                (type != "user" && type != "group" && kv.AccessPolicies.ToLookup(pp => pp.DisplayName)[principalPermissions.DisplayName].Count() > 1))
                        {
                            log.Error("AccessPolicyAlreadyDefined");
                            log.Debug($"An access policy has already been defined for {principalPermissions.DisplayName} with Alias '{principalPermissions.Alias}' in " +
                                $"KeyVault '{kv.VaultName}'. Please remove one of these access policies.");
                            Exit($"An access policy has already been defined for {principalPermissions.DisplayName} in KeyVault '{kv.VaultName}'.");
                        }
                        log.Info("Access policies are 1:1!");

                        try
                        {
                            checkValidPermissions(principalPermissions);
                        }
                        catch (Exception e)
                        {
                            log.Error("InvalidPermission");
                            log.Debug($"{e.Message}. Refer to Constants.cs to see the list of valid permission values.");
                            Exit($"{e.Message} for {principalPermissions.DisplayName} in {kv.VaultName}.");
                        }

                        try
                        {
                            translateShorthands(principalPermissions);
                        }
                        catch (Exception e)
                        {
                            log.Error("InvalidShorthand");
                            log.Debug($"{e.Message}. For more information regarding shorthands, refer to the 'Use of Shorthands' section: " +
                                $"https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/README.md");
                            Exit($"{e.Message} for {principalPermissions.DisplayName} in {kv.VaultName}.");
                        }

                        if (!portalPolicies.Contains(principalPermissions))
                        {
                            changes++;

                            // Check if the access policy was added (count = 0) or updated (count > 0)
                            IEnumerable<PrincipalPermissions> portalPolicy;
                            if (type == "user" || type == "group")
                            {
                                portalPolicy = portalPolicies.ToLookup(pp => pp.Alias)[principalPermissions.Alias];
                            }
                            else
                            {
                                portalPolicy = portalPolicies.ToLookup(pp => pp.DisplayName)[principalPermissions.DisplayName];
                            }

                            if (portalPolicy.Count() != 0)
                            {
                                var portalPermissions = portalPolicy.First();

                                string[] deletedKeys = portalPermissions.PermissionsToKeys.Except(principalPermissions.PermissionsToKeys).ToArray();
                                string[] deletedSecrets = portalPermissions.PermissionsToSecrets.Except(principalPermissions.PermissionsToSecrets).ToArray();
                                string[] deletedCertificates = portalPermissions.PermissionsToCertificates.Except(principalPermissions.PermissionsToCertificates).ToArray();
                                if (!(deletedKeys.Length == 0 && deletedSecrets.Length == 0 && deletedCertificates.Length == 0))
                                {
                                    deletedPolicies.Add(new PrincipalPermissions()
                                    {
                                        Type = portalPermissions.Type,
                                        DisplayName = portalPermissions.DisplayName,
                                        Alias = portalPermissions.Alias,
                                        PermissionsToKeys = deletedKeys,
                                        PermissionsToSecrets = deletedSecrets,
                                        PermissionsToCertificates = deletedCertificates
                                    });
                                }
                            }
                        }
                    }

                    if (deletedPolicies.Count() != 0)
                    {
                        deletedVaultPolicies.Add(new KeyVaultProperties()
                        {
                            VaultName = oldVault.VaultName,
                            ResourceGroupName = oldVault.ResourceGroupName,
                            SubscriptionId = oldVault.SubscriptionId,
                            Location = oldVault.Location,
                            TenantId = oldVault.TenantId,
                            AccessPolicies = deletedPolicies
                        });
                    }
                }
            }

            return new Tuple<List<KeyVaultProperties>, int>(deletedVaultPolicies, changes);
        }

        /// <summary>
        /// This method serializes the list of Vault objects and outputs the DeletedPolicies yaml.
        /// </summary>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties to serialize</param>
        /// <param name="yamlDirectory">The directory of the outputted yaml file</param>
        public void convertToYaml(List<KeyVaultProperties> deleted, string yamlDirectory)
        {
            log.Info("Generating DeletedPolicies.yml...");
            try
            {
                var serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(deleted);

                System.IO.File.WriteAllText($@"{yamlDirectory}\DeletedPolicies.yml", yaml);
                log.Info("DeletedPolicies.yml complete!");
                log.Logger.Repository.Shutdown();
                var path = log4net.GlobalContext.Properties["Log"] as string;
                var logged = System.IO.File.ReadAllText($"{path}.log");
                FileStream fileStream = new FileStream($"{path.Substring(0, path.IndexOf("Temp"))}Log.log",
                    FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter fileWriter = new StreamWriter(fileStream);
                fileWriter.Write(logged);
                fileWriter.Flush();
                fileWriter.Close();
                System.IO.File.Delete($"{path}.log");
            }
            catch (Exception e)
            {
                ConsoleError(e.Message);
            }
        }

        /// <summary>
        /// This method verifies that each KeyVault has the necessary fields and were not deleted from the Yaml.
        /// </summary>
        /// <param name="kv">The current KeyVaultProperties object</param>
        public void checkVaultInvalidFields(KeyVaultProperties kv)
        {
            if (kv.VaultName == null || kv.VaultName.Trim() == "")
            {
                throw new Exception($"Missing 'VaultName' for KeyVault '{kv.VaultName}'");
            }
            if (kv.ResourceGroupName == null || kv.ResourceGroupName.Trim() == "")
            {
                throw new Exception($"Missing 'ResourceGroupName' for KeyVault '{kv.VaultName}'");
            }
            if (kv.SubscriptionId == null || kv.SubscriptionId.Trim() == "")
            {
                throw new Exception($"Missing 'SubscriptionId' for KeyVault '{kv.VaultName}'");
            }
            if (kv.Location == null || kv.Location.Trim() == "")
            {
                throw new Exception($"Missing 'Location' for KeyVault '{kv.VaultName}'");
            }
            if (kv.TenantId == null || kv.TenantId.Trim() == "")
            {
                throw new Exception($"Missing 'TenantId' for KeyVault '{kv.VaultName}'");
            }
            if (kv.AccessPolicies == null)
            {
                throw new Exception($"Missing 'AccessPolicies' for KeyVault '{kv.VaultName}'");
            }
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has the necessary fields.
        /// </summary>
        /// <param name="name">The KeyVault name</param>
        /// <param name="principalPermissions">The PrincipalPermissions for which we want to validate</param>
        public void checkPPInvalidFields(string name, PrincipalPermissions principalPermissions)
        {
            if (principalPermissions.Type == null || principalPermissions.Type.Trim() == "")
            {
                throw new Exception($"Missing Type for {name}");
            }
            if (principalPermissions.DisplayName == null || principalPermissions.DisplayName.Trim() == "")
            {
                throw new Exception($"Missing DisplayName for {name}");
            }
            if (principalPermissions.PermissionsToKeys == null)
            {
                throw new Exception($"Missing PermissionsToKeys for {name}");
            }
            if (principalPermissions.PermissionsToSecrets == null)
            {
                throw new Exception($"Missing PermissionsToSecrets for {name}");
            }
            if (principalPermissions.PermissionsToCertificates == null)
            {
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
        /// <param name="graphClient">The GraphServiceClient to obtain the security principal's data</param>
        public List<KeyVaultProperties> updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient,
            Dictionary<string, string> secrets, GraphServiceClient graphClient)
        {
            checkVaultChanges(yamlVaults, vaultsRetrieved);

            log.Info($"Checking the number of changes made...");
            Tuple<List<KeyVaultProperties>, int> changed = getChanges(yamlVaults, vaultsRetrieved);
            int numChanges = changed.Item2;
            if (numChanges == 0)
            {
                log.Info("There is no difference between the YAML and the Key Vaults. No changes made.");
                Console.WriteLine("There is no difference between the YAML and the Key Vaults. No changes made.");
            }
            else if (numChanges > Constants.MAX_NUM_CHANGES)
            {
                log.Error("ChangesExceedLimit");
                log.Debug($"Too many AccessPolicies have been changed; the maximum is {Constants.MAX_NUM_CHANGES} changes, but you have changed {numChanges} policies. " +
                    $"Refer to the 'Global Constants and Considerations' section for more information on how changes are defined: " +
                    $"https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/README.md");
                Exit($"You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed {numChanges} policies.");
            }
            else
            {
                log.Info("The number of changes made was valid!");

                log.Info("Updating vaults...");
                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    if (!vaultsRetrieved.Contains(kv))
                    {
                        log.Info("Verifying the number of access policies for type 'User'...");
                        int numUsers = kv.usersContained();
                        if (numUsers < Constants.MIN_NUM_USERS)
                        {
                            log.Error($"TooFewUserPolicies: KeyVault '{kv.VaultName}' skipped!");
                            log.Debug($"KeyVault '{kv.VaultName}' contains only {numUsers} Users, but each KeyVault must contain access policies for at " +
                                $"least {Constants.MIN_NUM_USERS} Users. Please modify the AccessPolicies to reflect this.");
                            ConsoleError($"KeyVault '{kv.VaultName}' does not contain at least two users. Skipped.");
                        }
                        else
                        {
                            log.Info("User access policies verified!");
                            log.Info($"Updating KeyVault '{kv.VaultName}'...");
                            Console.WriteLine($"Updating {kv.VaultName}...");
                            updateVault(kv, kvmClient, secrets, graphClient);
                            log.Info($"KeyVault '{kv.VaultName}' successfully updated!");
                            Console.WriteLine($"{kv.VaultName} successfully updated!");
                        }
                    }
                }
                log.Info("Updates finished!");
            }
            return (changed.Item1);
        }

        /// <summary>
        /// This method updates the access policies of the specified KeyVault in Azure.
        /// </summary>
        /// <param name="kv">The KeyVault you want to update</param>
        /// <param name="kvmClient">The KeyManagementClient</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        public void updateVault(KeyVaultProperties kv, KeyVaultManagementClient kvmClient, Dictionary<string, string> secrets,
            GraphServiceClient graphClient)
        {
            try
            {
                kvmClient.SubscriptionId = kv.SubscriptionId;

                VaultProperties properties = kvmClient.Vaults.GetAsync(kv.ResourceGroupName, kv.VaultName).Result.Properties;
                properties.AccessPolicies = new List<AccessPolicyEntry>();

                foreach (PrincipalPermissions principalPermissions in kv.AccessPolicies)
                {
                    string type = principalPermissions.Type.ToLower().Trim();
                    Dictionary<string, string> data = verifySecurityPrincipal(principalPermissions, type, graphClient);

                    // Set security principal data
                    if (data.ContainsKey("ObjectId"))
                    {
                        principalPermissions.ObjectId = data["ObjectId"];
                    }

                    properties.AccessPolicies.Add(new AccessPolicyEntry(new Guid(secrets["tenantId"]), principalPermissions.ObjectId,
                        new Permissions(principalPermissions.PermissionsToKeys, principalPermissions.PermissionsToSecrets, principalPermissions.PermissionsToCertificates)));
                }
                Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
                
            }
            catch (Exception e)
            {
                if (Testing)
                {
                    throw new Exception(e.Message);
                }
                else
                {
                    log.Error("VaultNotFound", e);
                    log.Debug($"Please verify that the ResourceGroupName '{kv.ResourceGroupName}' and the VaultName '{kv.VaultName}' are correct.");
                    ConsoleError(e.Message);
                }
            }
        }

        /// <summary>
        /// This method verifies that the security principal exists and returns a dictionary that holds its data.
        /// </summary>
        /// <param name="principalPermissions">The current PrincipalPermissions object</param>
        /// <param name="type">The PrincipalPermissions type</param>
        /// <param name="graphClient">The GraphServiceClient to obtain the security principal's data</param>
        /// <returns>A dictionary containing the security principal data</returns>
        public Dictionary<string, string> verifySecurityPrincipal(PrincipalPermissions principalPermissions, string type, GraphServiceClient graphClient)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            log.Info($"Verifying the data for {principalPermissions.DisplayName} with Alias '{principalPermissions.Alias}'...");
            if (type == "user")
            {
                try
                {
                    if (principalPermissions.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {principalPermissions.DisplayName}.");
                    }
                    User user = null;
                    if (Testing)
                    {
                        TestGraphClient client = (TestGraphClient)graphClient;
                        user = client.Users[principalPermissions.Alias.ToLower().Trim()]
                           .Request()
                           .GetAsync().Result;
                    }
                    else
                    {
                        user = graphClient.Users[principalPermissions.Alias.ToLower().Trim()]
                            .Request()
                            .GetAsync().Result;
                    } 

                    if (principalPermissions.DisplayName.Trim().ToLower() != user.DisplayName.ToLower())
                    {
                        throw new Exception($"The DisplayName '{principalPermissions.DisplayName}' is incorrect and cannot be recognized.");
                    }
                    data["ObjectId"] = user.Id;
                    log.Info($"User verified!");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("ResourceNotFound"))
                    {
                        log.Error($"ResourceNotFound: User with Alias '{principalPermissions.Alias}' skipped!", e);
                        log.Debug($"The User with Alias '{principalPermissions.Alias}' could not be found. Please verify that this User exists in your Azure Active Directory. " +
                            $"For more information on adding Users to AAD, visit https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/add-users-azure-active-directory");
                        ConsoleError($"Could not find User with Alias '{principalPermissions.Alias}'. User skipped.");
                    }
                    else
                    {
                        log.Error("UserFieldsInvalid");
                        log.Debug(e.Message);
                        ConsoleError($"{e.Message} User skipped.");
                    }
                }
            }
            else if (type == "group")
            {
                try
                {
                    if (principalPermissions.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {principalPermissions.DisplayName}.");
                    }

                    Group group = null;
                    if (Testing)
                    {
                        TestGraphClient client = (TestGraphClient)graphClient;
                        group = client.Groups
                            .Request()
                            .Filter($"startswith(Mail,'{principalPermissions.Alias}')")
                            .GetAsync().Result[0];
                    }
                    else
                    {
                        group = graphClient.Groups
                            .Request()
                            .Filter($"startswith(Mail,'{principalPermissions.Alias}')")
                            .GetAsync().Result[0];
                    }
                    

                    if (principalPermissions.DisplayName.Trim().ToLower() != group.DisplayName.ToLower())
                    {
                        throw new Exception($"The DisplayName '{principalPermissions.DisplayName}' is incorrect and cannot be recognized.");
                    }
                    data["ObjectId"] = group.Id;
                    log.Info($"Group verified!");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("out of range"))
                    {
                        log.Error($"ResourceNotFound: Group with Alias '{principalPermissions.Alias}' skipped!", e);
                        log.Debug($"The Group with Alias '{principalPermissions.Alias}' could not be found. Please verify that this Group exists in your Azure Active Directory. " +
                            $"For more information on adding Groups to AAD, visit https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-groups-create-azure-portal");
                        ConsoleError($"Could not find Group with DisplayName '{principalPermissions.DisplayName}'. Group skipped.");
                    }
                    else
                    {
                        log.Error("GroupFieldsInvalid");
                        log.Debug(e.Message);
                        ConsoleError($"{e.Message} Group skipped.");
                    }
                }
            }
            else if (type == "application")
            {
                try
                {
                    Application app = null;
                    if (Testing)
                    {
                        TestGraphClient client = (TestGraphClient)graphClient;
                        app = client.Applications
                            .Request()
                            .Filter($"startswith(DisplayName,'{principalPermissions.DisplayName}')")
                            .GetAsync().Result[0];
                    }
                    else
                    {
                        app = graphClient.Applications
                            .Request()
                            .Filter($"startswith(DisplayName,'{principalPermissions.DisplayName}')")
                            .GetAsync().Result[0];
                    }
                    

                    if (principalPermissions.Alias.Length != 0)
                    {
                        throw new Exception($"The Alias '{principalPermissions.Alias}' should not be defined and cannot be recognized for {principalPermissions.DisplayName}.");
                    }
                    data["ObjectId"] = app.Id;
                    log.Info($"Application verified!");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("out of range"))
                    {
                        log.Error($"ResourceNotFound: Application with DisplayName '{principalPermissions.DisplayName}' skipped!", e);
                        log.Debug($"The Application with DisplayName '{principalPermissions.DisplayName}' could not be found. Please verify that this Application exists in your Azure Active Directory. " +
                            $"For more information on creating an Application in AAD, visit " +
                            $"https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#create-an-azure-active-directory-application");
                        ConsoleError($"Could not find Application with DisplayName '{principalPermissions.DisplayName}'. Application skipped.");
                    }
                    else
                    {
                        log.Error("ApplicationFieldsInvalid");
                        log.Debug(e.Message);
                        ConsoleError($"{e.Message} Application skipped.");
                    }
                }
            }
            else if (type == "service principal")
            {
                try
                {
                    ServicePrincipal principal = null;
                    if (Testing)
                    {
                        TestGraphClient client = (TestGraphClient)graphClient;
                        principal = client.ServicePrincipals
                            .Request()
                            .Filter($"startswith(DisplayName,'{principalPermissions.DisplayName}')")
                            .GetAsync().Result[0];
                    }
                    else
                    {
                        principal = graphClient.ServicePrincipals
                            .Request()
                            .Filter($"startswith(DisplayName,'{principalPermissions.DisplayName}')")
                            .GetAsync().Result[0];
                    }
                     

                    if (principalPermissions.Alias.Length != 0)
                    {
                        throw new Exception($"The Alias '{principalPermissions.Alias}' should not be defined and cannot be recognized for {principalPermissions.DisplayName}.");
                    }
                    data["ObjectId"] = principal.Id;
                    log.Info($"Service Principal verified!");
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("out of range"))
                    {
                        log.Error($"ResourceNotFound: ServicePrincipal with DisplayName '{principalPermissions.DisplayName}' skipped!", e);
                        log.Debug($"The ServicePrincipal with DisplayName '{principalPermissions.DisplayName}' could not be found. Please verify that this Service Principal " +
                            $"exists in your Azure Active Directory. For more information on creating a ServicePrincipal in AAD, visit " +
                            $"https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal");
                        ConsoleError($"Could not find ServicePrincipal with DisplayName '{principalPermissions.DisplayName}'. Service Principal skipped.");
                    }
                    else
                    {
                        log.Error("ServicePrincipalFieldsInvalid");
                        log.Debug(e.Message);
                        ConsoleError($"{e.Message} Service Principal skipped.");
                    }
                }
            }
            else
            {
                try
                {
                    throw new Exception($"'{principalPermissions.Type}' is not a valid type for {principalPermissions.DisplayName}. Valid types are 'User', 'Group', " +
                    $"'Application', or 'Service Principal'.");
                }
                catch (Exception e)
                {
                    log.Error("UnknownType: Skipped!");
                    log.Debug(e.Message);
                    ConsoleError($"{e.Message} Skipped!");
                }
            }
            return data;
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has valid permissions and does not contain duplicate permissions.
        /// </summary>
        /// <param name="principalPermissions">The PrincipalPermissions for which we want to validate</param>
        public void checkValidPermissions(PrincipalPermissions principalPermissions)
        {
            log.Info($"Verifying that permissions exist for {principalPermissions.DisplayName} with Alias '{principalPermissions.Alias}'...");
            int total = principalPermissions.PermissionsToCertificates.Length + principalPermissions.PermissionsToKeys.Length + principalPermissions.PermissionsToSecrets.Length;
            if (total == 0)
            {
                log.Error($"UndefinedAccessPolicies: {principalPermissions.DisplayName} skipped!");
                log.Debug($"'{principalPermissions.DisplayName}' of Type '{principalPermissions.Type}' does not have any permissions specified. " +
                    $"Grant the {principalPermissions.Type} at least one permission or delete the {principalPermissions.Type} entirely to remove all of their permissions.");
                throw new Exception($"Skipped {principalPermissions.Type}, '{principalPermissions.DisplayName}'. Does not have any permissions specified.");
            }
            log.Info("Permissions exist!");

            log.Info($"Validating the permissions for {principalPermissions.DisplayName} with Alias '{principalPermissions.Alias}'...");
            foreach (string key in principalPermissions.PermissionsToKeys)
            {
                if (!Constants.VALID_KEY_PERMISSIONS.Contains(key) && (!key.StartsWith("all -")) && (!key.StartsWith("read -"))
                    && (!key.StartsWith("write -")) && (!key.StartsWith("storage -")) && (!key.StartsWith("crypto - ")))
                {
                    throw new Exception($"Invalid key permission '{key}'");
                }
            }
            foreach (string secret in principalPermissions.PermissionsToSecrets)
            {
                if (!Constants.VALID_SECRET_PERMISSIONS.Contains(secret) && (!secret.StartsWith("all -")) && (!secret.StartsWith("read -"))
                    && (!secret.StartsWith("write -")) && (!secret.StartsWith("storage -")))
                {
                    throw new Exception($"Invalid secret permission '{secret}'");
                }
            }
            foreach (string certif in principalPermissions.PermissionsToCertificates)
            {
                if (!Constants.VALID_CERTIFICATE_PERMISSIONS.Contains(certif) && (!certif.StartsWith("all -")) && (!certif.StartsWith("read -"))
                    && (!certif.StartsWith("write -")) && (!certif.StartsWith("storage -")) && (!certif.StartsWith("management -")))
                {
                    throw new Exception($"Invalid certificate permission '{certif}'");
                }
            }

            if (principalPermissions.PermissionsToKeys.Distinct().Count() != principalPermissions.PermissionsToKeys.Count())
            {
                List<string> duplicates = findDuplicates(principalPermissions.PermissionsToKeys);
                throw new Exception($"Key permission(s) '{string.Join(", ", duplicates)}' repeated");
            }
            if (principalPermissions.PermissionsToSecrets.Distinct().Count() != principalPermissions.PermissionsToSecrets.Count())
            {
                List<string> duplicates = findDuplicates(principalPermissions.PermissionsToSecrets);
                throw new Exception($"Secret permission(s) '{string.Join(", ", duplicates)}' repeated");
            }
            if (principalPermissions.PermissionsToCertificates.Distinct().Count() != principalPermissions.PermissionsToCertificates.Count())
            {
                List<string> duplicates = findDuplicates(principalPermissions.PermissionsToCertificates);
                throw new Exception($"Certificate permission(s) '{string.Join(", ", duplicates)}' repeated");
            }

            log.Info("Permissions are valid!");
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
        /// <param name="principalPermissions">The current PrincipalPermissions object</param>
        public void translateShorthands(PrincipalPermissions principalPermissions)
        {
            log.Info("Translating shorthands...");

            string[] updatedKeyPermissions = translateShorthand("all", "key", principalPermissions.PermissionsToKeys, Constants.ALL_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            updatedKeyPermissions = translateShorthand("read", "key", updatedKeyPermissions, Constants.READ_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            updatedKeyPermissions = translateShorthand("write", "key", updatedKeyPermissions, Constants.WRITE_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            updatedKeyPermissions = translateShorthand("storage", "key", updatedKeyPermissions, Constants.STORAGE_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            updatedKeyPermissions = translateShorthand("crypto", "key", updatedKeyPermissions, Constants.CRYPTO_KEY_PERMISSIONS,
                Constants.VALID_KEY_PERMISSIONS, Constants.SHORTHANDS_KEYS);
            principalPermissions.PermissionsToKeys = updatedKeyPermissions;

            string[] updatedSecretPermissions = translateShorthand("all", "secret", principalPermissions.PermissionsToSecrets, Constants.ALL_SECRET_PERMISSIONS,
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            updatedSecretPermissions = translateShorthand("read", "secret", updatedSecretPermissions, Constants.READ_SECRET_PERMISSIONS,
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            updatedSecretPermissions = translateShorthand("write", "secret", updatedSecretPermissions, Constants.WRITE_SECRET_PERMISSIONS,
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            updatedSecretPermissions = translateShorthand("storage", "secret", updatedSecretPermissions, Constants.STORAGE_SECRET_PERMISSIONS,
                Constants.VALID_SECRET_PERMISSIONS, Constants.SHORTHANDS_SECRETS);
            principalPermissions.PermissionsToSecrets = updatedSecretPermissions;

            string[] updatedCertifPermissions = translateShorthand("all", "certificate", principalPermissions.PermissionsToCertificates,
                Constants.ALL_CERTIFICATE_PERMISSIONS, Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            updatedCertifPermissions = translateShorthand("read", "certificate", updatedCertifPermissions,
                Constants.READ_CERTIFICATE_PERMISSIONS, Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            updatedCertifPermissions = translateShorthand("write", "certificate", updatedCertifPermissions,
                Constants.WRITE_CERTIFICATE_PERMISSIONS, Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            updatedCertifPermissions = translateShorthand("storage", "certificate", updatedCertifPermissions,
                Constants.STORAGE_CERTIFICATE_PERMISSIONS, Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            updatedCertifPermissions = translateShorthand("management", "certificate", updatedCertifPermissions,
                Constants.MANAGEMENT_CERTIFICATE_PERMISSIONS, Constants.VALID_CERTIFICATE_PERMISSIONS, Constants.SHORTHANDS_CERTIFICATES);
            principalPermissions.PermissionsToCertificates = updatedCertifPermissions;

            log.Info("Shorthands translated!");
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
        public string[] translateShorthand(string shorthand, string permissionType, string[] permissions, string[] shorthandPermissions, string[] validPermissions, string[] shorthandWords)
        {
            var shorthandInstances = permissions.Where(val => val.Trim().ToLower().StartsWith(shorthand)).ToArray();
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
                        throw new Exception($"'All' permission removes need for other {permissionType} permissions");
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
                        if (!validPermissions.Contains(p) || (!inst.StartsWith("all") && !shorthandPermissions.Contains(p)))
                        {
                            throw new Exception($"Remove values could not be recognized in {permissionType} permission '{shorthand} - <{p}>'");
                        }

                        // The remove value is a shorthand, then replace the shorthand with its permissions
                        if (shorthandWords.Contains(p))
                        {
                            if (p == "all")
                            {
                                throw new Exception("Cannot remove 'all' from a permission");
                            }
                            string[] valuesToReplace = getShorthandPermissions(p, permissionType);
                            valuesToRemove = valuesToRemove.Concat(valuesToReplace).Where(val => val != p).ToArray();
                        }
                    }
                    var permissionsToGrant = shorthandPermissions.Except(valuesToRemove);

                    // Check for duplicates
                    var common = permissions.Intersect(permissionsToGrant);
                    if (common.Count() != 0)
                    {
                        throw new Exception($"{string.Join(", ", common)} permissions are already included in {permissionType} '{shorthand}' permission");
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
        public string[] getShorthandPermissions(string shorthand, string permissionType)
        {
            if (permissionType.ToLower() == "key")
            {
                if (shorthand == "all")
                {
                    return Constants.ALL_KEY_PERMISSIONS;
                }
                else if (shorthand == "read")
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
                    return Constants.CRYPTO_KEY_PERMISSIONS;
                }
            }
            else if (permissionType.ToLower() == "secret")
            {
                if (shorthand == "all")
                {
                    return Constants.ALL_SECRET_PERMISSIONS;
                }
                else if (shorthand == "read")
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
            else if (permissionType.ToLower() == "certificate")
            {
                if (shorthand == "all")
                {
                    return Constants.ALL_CERTIFICATE_PERMISSIONS;
                }
                else if (shorthand == "read")
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
            return null;
        }

        /// <summary>
        /// This method throws an exception instead of exiting the program when Testing is true. 
        /// Otherwise, if Testing is false, the program exits.
        /// </summary>
        /// <param name="message">The error message to print to the console</param>
        public void Exit(string message)
        {
            if (!Testing)
            {
                ConsoleError(message);
                log.Info("Program exited.");
                log.Logger.Repository.Shutdown();
                var path = log4net.GlobalContext.Properties["Log"] as string;
                var logged = System.IO.File.ReadAllText($"{path}.log");
                FileStream fileStream = new FileStream($"{path.Substring(0, path.IndexOf("Temp"))}Log.log",
                    FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter fileWriter = new StreamWriter(fileStream);
                fileWriter.Write(logged);
                fileWriter.Flush();
                fileWriter.Close();
                System.IO.File.Delete($"{path}.log");
                Environment.Exit(1);
            }
            else
            {
                throw new Exception($"{message}");
            }
        }

        /// <summary>
        /// This method prints the error message to the Console in red, then resets the color.
        /// </summary>
        /// <param name="message">The error message to be printed</param>
        private void ConsoleError(string message)
        {
            error = message;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
            Console.ResetColor();
        }

        // This field indicates if unit tests are being run
        public bool Testing { get; set; }
        // This field indicates if the KeyVaults have changed
        public List<KeyVaultProperties> Changed { get; set; }
        // This field defines the logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string error;
    }
}
