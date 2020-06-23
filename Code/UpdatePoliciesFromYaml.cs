using log4net.Util;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
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
        /// <param name="yamlDirectory"> The directory of the YAML file </param>
        /// <returns>The list of KeyVaultProperties if the input file has the correct formatting. Otherwise, exits the program.</returns>
        public List<KeyVaultProperties> deserializeYaml(string yamlDirectory)
        {
            List<KeyVaultProperties> yamlVaults = new List<KeyVaultProperties>();
            try
            {
                log.Info("Reading .yml file...");
                string yaml = System.IO.File.ReadAllText(yamlDirectory);
                log.Info("File successfully read!");
                log.Info("Deserializing .yml file...");
                var deserializer = new DeserializerBuilder().Build();
                yamlVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);
                log.Info("File successfully deserialized!");
            }
            catch(Exception e)
            {
                log.Error($"DeserializationFail", e);
                log.Debug("Refer to the .yml Sample (https://github.com/microsoft/Managing-RBAC-in-Azure/blob/Katie/Config/YamlSample.yml) for questions on formatting and inputs. Ensure that you have all the required fields with valid values, then try again.");
            }
            try 
            {
                log.Info("Checking for invalid fields...");
                foreach (KeyVaultProperties kv in yamlVaults)
                {
                    checkVaultInvalidFields(kv);
                    foreach (PrincipalPermissions sp in kv.AccessPolicies)
                    {
                        checkSPInvalidFields(kv.VaultName, sp);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"InvalidFields", e);
                log.Debug("Please add or modify the specified field. 'VaultName', 'ResourceGroupName', 'SubscriptionId', 'Location', 'TenantId', and 'AccessPolicies' should be defined for each KeyVault. " + "For more information on the fields required for each Security Principal in 'AccessPolicies', refer to the 'Editing the Access Policies' section: https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/README.md");
                Exit($"Error: {e.Message}");
            }
            log.Info("Fields validated!");
            return yamlVaults;
        }

        /// <summary>
        /// This method checks that the amount of changes made do not exceed the maximum number of changes defined in the Constants file.
        /// </summary>
        /// <param name="yamlVaults">The list of KeyVaultProperties obtained from the Yaml file</param>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties obtained from the MasterConfig.json file</param>
        /// <returns>The number of changes made</returns>
        public int checkChanges(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved)
        {
            log.Info($"Checking the number of changes made...");
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
                log.Error("ChangesExceedLimit");
                log.Debug($"Too many AccessPolicies have been changed; the maximum is {Constants.MAX_NUM_CHANGES} changes, but you have changed {changes} policies. Refer to the 'Global Constants and Considerations' section for more information on how changes are defined: https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/README.md");
                Exit($"Error: You have changed too many policies. The maximum is {Constants.MAX_NUM_CHANGES}, but you have changed {changes} policies.");
            }
            log.Info("The number of changes made was valid!");
            log.Info("Scanning the .yml and .json files...");
            foreach (KeyVaultProperties kv in vaultsRetrieved)
            {
                if (yamlVaults.ToLookup(v => v.VaultName)[kv.VaultName].Count() == 0)
                {
                    log.Error($"VaultDeleted");
                    log.Debug($"KeyVault '{kv.VaultName}' specified in the .json file was deleted from the .yml file! Please re-add this KeyVault or re-run AccessPoliciesToYamlProgram.cs to retrieve the full list of KeyVaults.");
                    Exit($"Error: KeyVault, {kv.VaultName}, specified in the JSON file was not found in the YAML file.");
                }
            }
            foreach (KeyVaultProperties kv in yamlVaults)
            {
                if (vaultsRetrieved.ToLookup(v => v.VaultName)[kv.VaultName].Count() == 0)
                {
                    log.Error($"VaultAdded");
                    log.Debug($"KeyVault '{kv.VaultName}' was not specified in the .json file and was added to the .yml file! Please remove this KeyVault.");
                    Exit($"Error: KeyVault '{kv.VaultName}' in the YAML file was not found in the JSON file.");
                }
            }
            log.Info("Files verified!");
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
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        public void checkSPInvalidFields(string name, PrincipalPermissions sp)
        {
            if (sp.Type == null || sp.Type.Trim() == "")
            {
                throw new Exception($"Missing Type for {name}");
            }
            if (sp.DisplayName == null || sp.DisplayName.Trim() == "")
            {
                throw new Exception($"Missing DisplayName for {name}");
            }
            if (sp.PermissionsToKeys == null)
            {
                throw new Exception($"Missing PermissionsToKeys for {name}");
            }
            if (sp.PermissionsToSecrets == null)
            {
                throw new Exception($"Missing PermissionsToSecrets for {name}");
            }
            if (sp.PermissionsToCertificates == null)
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
        /// <param name="graphClient">The GraphServiceClient to obtain the service principal's data</param>
        public void updateVaults(List<KeyVaultProperties> yamlVaults, List<KeyVaultProperties> vaultsRetrieved, KeyVaultManagementClient kvmClient,
            Dictionary<string, string> secrets, GraphServiceClient graphClient)
        {
            foreach (KeyVaultProperties kv in yamlVaults)
            {
                try
                {
                    log.Info($"Verifying that only 'AccessPolicies' has been changed for KeyVault '{kv.VaultName}'...");
                    checkVaultChanges(vaultsRetrieved, kv);
                    log.Info("Vault changes verified!");
                    log.Info("Verifying the number of access policies for type 'User'...");
                    if (!vaultsRetrieved.Contains(kv))
                    {
                        int numUsers = kv.usersContained();
                        if (numUsers < Constants.MIN_NUM_USERS)
                        {
                            log.Error($"TooFewUserPolicies: KeyVault '{kv.VaultName}' skipped!");
                            log.Debug($"KeyVault '{kv.VaultName}' contains only {numUsers} Users, but each KeyVault must contain access policies for at least {Constants.MIN_NUM_USERS} Users. Please modify the AccessPolicies to reflect this.");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"KeyVault '{kv.VaultName}' does not contain at least two users. Skipped.");
                            Console.ResetColor();
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
                catch (Exception e)
                {
                    log.Error($"VaultFieldsChanged: KeyVault '{kv.VaultName}' skipped!", e);
                    log.Debug("Changes made to any fields other than the 'AccessPolicies' field are prohibited. Please modify the specified field.");
                    Console.ForegroundColor = ConsoleColor.Red; 
                    Console.WriteLine($"Error: {e.Message} Vault Skipped.");
                    Console.ResetColor();
                }
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
                throw new Exception($"VaultName for KeyVault '{kv.VaultName}' was changed or added.");
            }

            // If KeyVault name was correct, then check the other fields
            KeyVaultProperties originalKV = lookupName[kv.VaultName].ToList()[0];
            if (originalKV.ResourceGroupName != kv.ResourceGroupName.Trim())
            {
                throw new Exception($"ResourceGroupName for KeyVault '{kv.VaultName}' was changed.");
            }
            if (originalKV.SubscriptionId != kv.SubscriptionId.Trim())
            {
                throw new Exception($"SubscriptionId for KeyVault '{kv.VaultName}' was changed.");
            }
            if (originalKV.Location != kv.Location.Trim())
            {
                throw new Exception($"Location for KeyVault '{kv.VaultName}' was changed.");
            }
            if (originalKV.TenantId != kv.TenantId.Trim())
            {
                throw new Exception($"TenantId for KeyVault '{kv.VaultName}' was changed.");
            }
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

                foreach (PrincipalPermissions sp in kv.AccessPolicies)
                {
                    try
                    {
                        log.Info($"Verifying that permissions exist for {sp.DisplayName}...");
                        int total = sp.PermissionsToCertificates.Length + sp.PermissionsToKeys.Length + sp.PermissionsToSecrets.Length;
                        if (total != 0)
                        {
                            log.Info("Permissions exist!");
                            string type = sp.Type.ToLower().Trim();
                            log.Info($"Verifying that the access policy for {sp.DisplayName} is unique...");
                            if (((type == "user" || type == "group") && kv.AccessPolicies.ToLookup(v => v.Alias)[sp.Alias].Count() > 1) ||
                                (type != "user" && type != "group" && kv.AccessPolicies.ToLookup(v => v.DisplayName)[sp.DisplayName].Count() > 1))
                            {
                                log.Error("AccessPolicyAlreadyDefined");
                                log.Debug($"An access policy has already been defined for {sp.DisplayName} in KeyVault '{kv.VaultName}'. Please remove one of these access policies.");
                                Exit($"Error: An access policy has already been defined for {sp.DisplayName} in KeyVault '{kv.VaultName}'.");
                            }
                            log.Info("Access policies are 1:1!");
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

                                    log.Info($"Validating the permissions for {sp.DisplayName}...");
                                    checkValidPermissions(sp); //errors with invalid valsd or repreated info
                                    log.Info("Permissions are valid!");
                                    log.Info("Translating shorthands...");
                                    translateShorthands(sp);

                                    properties.AccessPolicies.Add(new AccessPolicyEntry(new Guid(secrets["tenantId"]), sp.ObjectId,
                                            new Permissions(sp.PermissionsToKeys, sp.PermissionsToSecrets, sp.PermissionsToCertificates)));
                                }
                                catch (Exception e)
                                {
                                    // Errors caught for checkValidPermissions
                                    if (e.Message.Contains("Invalid") || e.Message.Contains("repeated"))
                                    {
                                        log.Error("InvalidPermission");
                                        log.Debug($"{e.Message}. Refer to Constants.cs to see the list of valid permission values.");
                                    }
                                    // Errors caught for translateShorthands
                                    else
                                    {
                                        log.Error("InvalidShorthand");
                                        log.Debug($"{e.Message}. For more information regarding shorthands, refer to the 'Use of Shorthands' section: https://github.com/microsoft/Managing-RBAC-in-Azure/blob/Katie/README.md");
                                    }
                                    Exit($"Error: {e.Message} for {sp.DisplayName} in {kv.VaultName}.");
                                }
                            }
                        }
                        else
                        {
                            log.Error($"UndefinedAccessPolicies: {sp.DisplayName} skipped!");
                            log.Debug($"'{sp.DisplayName}' of Type '{sp.Type}' does not have any permissions specified. Grant the {sp.Type} at least one permission or delete the {sp.Type} entirely to remove all of their permissions.");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error: Skipped {sp.Type}, '{sp.DisplayName}'. Does not have any permissions specified.");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception e)
                    {
                        if (Testing)
                        {
                            throw new Exception(e.Message);
                        }
                        log.Error("UnknownType: Skipped!");
                        log.Debug(e.Message);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {e.Message} Skipped!");
                        Console.ResetColor();
                    }
                }
                if (!Testing)
                {
                    Vault updatedVault = kvmClient.Vaults.CreateOrUpdateAsync(kv.ResourceGroupName, kv.VaultName, new VaultCreateOrUpdateParameters(kv.Location, properties)).Result;
                }
                else
                {
                    Changed.Add(kv);
                }
            }
            catch (Exception e)
            {
                if (Testing)
                {
                    throw new Exception(e.Message);
                }
                log.Error("VaultNotFound", e);
                log.Debug($"Please verify that the ResourceGroupName '{kv.ResourceGroupName}' and the VaultName '{kv.VaultName}' are correct.");
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
        public Dictionary<string, string> verifyServicePrincipal(PrincipalPermissions sp, string type, GraphServiceClient graphClient)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            log.Info($"Verifying {sp.DisplayName}...");
            if (type == "user")
            {
                try
                {
                    if (sp.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {sp.DisplayName}.");
                    }

                    User user = graphClient.Users[sp.Alias.ToLower().Trim()]
                    .Request()
                    .GetAsync().Result;

                    if (sp.DisplayName.Trim().ToLower() != user.DisplayName.ToLower())
                    {
                        throw new Exception($"The DisplayName '{sp.DisplayName}' is incorrect and cannot be recognized.");
                    }
                    data["ObjectId"] = user.Id;
                    log.Info($"{sp.DisplayName} verified!");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("ResourceNotFound"))
                    {
                        log.Error($"ResourceNotFound: User with Alias '{sp.Alias}' skipped!", e);
                        log.Debug($"The User with Alias '{sp.Alias}' could not be found. Please verify that this User exists in your Azure Active Directory. For more information on adding Users to AAD, visit https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/add-users-azure-active-directory");
                        Console.WriteLine($"Error: Could not find User with Alias '{sp.Alias}'. User skipped.");
                    }
                    else
                    {
                        log.Error("UserFieldsInvalid");
                        log.Debug(e.Message);
                        Console.WriteLine($"Error: {e.Message} User skipped.");
                    }
                    Console.ResetColor();
                }
            }
            else if (type == "group")
            {
                try
                {
                    if (sp.Alias.Trim().Length == 0)
                    {
                        throw new Exception($"Alias is required for {sp.DisplayName}.");
                    }

                    Group group = graphClient.Groups
                    .Request()
                    .Filter($"startswith(Mail,'{sp.Alias}')")
                    .GetAsync().Result[0];

                    if (sp.DisplayName.Trim().ToLower() != group.DisplayName.ToLower())
                    {
                        throw new Exception($"The DisplayName '{sp.DisplayName}' is incorrect and cannot be recognized.");
                    }
                    data["ObjectId"] = group.Id;
                    data["Alias"] = group.Mail;
                    log.Info($"{sp.DisplayName} verified!");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("out of range"))
                    {
                        log.Error($"ResourceNotFound: Group with Alias '{sp.Alias}' skipped!", e);
                        log.Debug($"The Group with Alias '{sp.Alias}' could not be found. Please verify that this Group exists in your Azure Active Directory. For more information on adding Groups to AAD, visit https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-groups-create-azure-portal");
                        Console.WriteLine($"Error: Could not find Group with DisplayName '{sp.DisplayName}'. Group skipped.");
                    }
                    else
                    {
                        log.Error("GroupFieldsInvalid");
                        log.Debug(e.Message);
                        Console.WriteLine($"Error: {e.Message} Group skipped.");
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

                    if (sp.Alias.Length != 0)
                    {
                        throw new Exception($"The Alias '{sp.Alias}' should not be defined and cannot be recognized for {sp.DisplayName}.");
                    }
                    data["ObjectId"] = app.Id;
                    data["ApplicationId"] = app.AppId;
                    log.Info($"{sp.DisplayName} verified!");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("out of range"))
                    {
                        log.Error($"ResourceNotFound: Application with DisplayName '{sp.DisplayName}' skipped!", e);
                        log.Debug($"The Application with DisplayName '{sp.DisplayName}' could not be found. Please verify that this Application exists in your Azure Active Directory. For more information on creating an Application in AAD, visit https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#create-an-azure-active-directory-application");
                        Console.WriteLine($"Error: Could not find Application with DisplayName '{sp.DisplayName}'. Application skipped.");
                    }
                    else
                    {
                        log.Error("ApplicationFieldsInvalid");
                        log.Debug(e.Message);
                        Console.WriteLine($"Error: {e.Message} Application skipped.");
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

                    if (sp.Alias.Length != 0)
                    {
                        throw new Exception($"The Alias '{sp.Alias}' should not be defined and cannot be recognized for {sp.DisplayName}.");
                    }
                    data["ObjectId"] = principal.Id;
                    log.Info($"{sp.DisplayName} verified!");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (e.Message.Contains("out of range"))
                    {
                        log.Error($"ResourceNotFound: ServicePrincipal with DisplayName '{sp.DisplayName}' skipped!", e);
                        log.Debug($"The ServicePrincipal with DisplayName '{sp.DisplayName}' could not be found. Please verify that this Service Principal exists in your Azure Active Directory. For more information on creating a ServicePrincipal in AAD, visit https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal");
                        Console.WriteLine($"Error: Could not find ServicePrincipal with DisplayName '{sp.DisplayName}'. Service Principal skipped.");
                    }
                    else
                    {
                        log.Error("ServicePrincipalFieldsInvalid");
                        log.Debug(e.Message);
                        Console.WriteLine($"Error: {e.Message} Service Principal skipped.");
                    }
                    Console.ResetColor();
                }
            }
            else
            {
                throw new Exception($"'{sp.Type}' is not a valid type for {sp.DisplayName}. Valid types are 'User', 'Group', 'Application', or 'Service Principal'.");
            }
            return data;
        }

        /// <summary>
        /// This method verifies that the PrincipalPermissions object has valid permissions and does not contain duplicate permissions.
        /// </summary>
        /// <param name="sp">The PrincipalPermissions for which we want to validate</param>
        public void checkValidPermissions(PrincipalPermissions sp)
        {
            var trimKeyPermissions = new string[sp.PermissionsToKeys.Length];
            foreach (string kp in sp.PermissionsToKeys)
            {
                var k = kp.Trim().ToLower();
                trimKeyPermissions[trimKeyPermissions.Count(s => s != null)] = k;
                if (!Constants.VALID_KEY_PERMISSIONS.Contains(k.ToLower()) && (!k.ToLower().StartsWith("all -")) && (!k.ToLower().StartsWith("read -"))
                    && (!k.ToLower().StartsWith("write -")) && (!k.ToLower().StartsWith("storage -")) && (!k.ToLower().StartsWith("crypto - ")))
                {
                    throw new Exception($"Invalid key permission '{kp}'");
                }
            }
            sp.PermissionsToKeys = trimKeyPermissions;

            var trimSecPermissions = new string[sp.PermissionsToSecrets.Length];
            foreach (string s in sp.PermissionsToSecrets)
            {
                var se = s.Trim().ToLower();
                trimSecPermissions[trimSecPermissions.Count(s => s != null)] = se;
                if (!Constants.VALID_SECRET_PERMISSIONS.Contains(se.ToLower()) && (!se.ToLower().StartsWith("all -")) && (!se.ToLower().StartsWith("read -"))
                    && (!se.ToLower().StartsWith("write -")) && (!se.ToLower().StartsWith("storage -")))
                {
                    throw new Exception($"Invalid secret permission '{s}'");
                }
            }
            sp.PermissionsToSecrets = trimSecPermissions;

            var trimCertPermissions = new string[sp.PermissionsToCertificates.Length];
            foreach (string cp in sp.PermissionsToCertificates)
            {
                var c = cp.Trim().ToLower();
                trimCertPermissions[trimCertPermissions.Count(s => s != null)] = c;
                if (!Constants.VALID_CERTIFICATE_PERMISSIONS.Contains(c.ToLower()) && (!c.ToLower().StartsWith("all -")) && (!c.ToLower().StartsWith("read -"))
                    && (!c.ToLower().StartsWith("write -")) && (!c.ToLower().StartsWith("storage -")) && (!c.ToLower().StartsWith("management -")))
                {
                    throw new Exception($"Invalid certificate permission '{cp}'");
                }
            }
            sp.PermissionsToCertificates = trimCertPermissions;

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
        public void translateShorthands(PrincipalPermissions sp)
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
        public string[] getShorthandPermissions(string shorthand, string permissionType)
        {
            if (shorthand == "all")
            {
                throw new Exception("Cannot remove 'all' from a permission");
            }
            else
            {
                if (permissionType.ToLower() == "key")
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
                else if (permissionType.ToLower() == "secret")
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
                else if (permissionType.ToLower() == "certificate")
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
      
        /// <summary>
        /// This method throws an exception instead of exiting the program when Testing is true. 
        /// Otherwise, if Testing is false, the program exits.
        /// </summary>
        /// <param name="message">The error message to print to the console</param>
        public void Exit(string message)
        {
            if (!Testing)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
                log.Info("Program exited.");
                Environment.Exit(1);
            }
            else
            {
                throw new Exception($"{message}");
            }
        }

        // This field indicates if unit tests are being run
        public bool Testing { get; set; }
        // This field indicates if the KeyVaults have changed
        public List<KeyVaultProperties> Changed { get; set; }
        // This field defines the logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}
