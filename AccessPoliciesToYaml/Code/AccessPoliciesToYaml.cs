using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest.Azure;
using Microsoft.Graph;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using System.IO;
using log4net;
using log4net.Config;

namespace RBAC
{
    /// <summary>
    /// "Phase 1" Code that serializes a list of Key Vaults into Yaml.
    /// </summary>
    public class AccessPoliciesToYaml
    {
        /// <summary>
        /// Constructor to create an instance of the AccessPoliciesToYaml class for use in Unit Testing.
        /// </summary>
        /// <param name="testing">True if unit tests are being run. Otherwise, false.</param>
        public AccessPoliciesToYaml(bool testing)
        {
            Testing = testing;
        }

        /// <summary>
        /// This method verifies that the file arguments are of the correct type.
        /// </summary>
        /// <param name="args">The string array of program arguments</param>
        public void verifyFileExtensions(string[] args)
        {
            try
            {
                if (args.Length == 0 || args == null)
                {
                    throw new Exception("Missing 3 input files.");
                }
                if (args.Length == 1)
                {
                    throw new Exception("Missing 2 input files.");
                }
                if (args.Length == 2)
                {
                    throw new Exception("Missing 1 input file.");
                }
                if (args.Length > 3)
                {
                    throw new Exception("Too many input files. Maximum needed is 3.");
                }
                if (System.IO.Path.GetExtension(args[0]) != ".json")
                {
                    throw new Exception("The 1st argument is not a .json file.");
                }
                if (System.IO.Path.GetExtension(args[1]) != ".yml")
                {
                    throw new Exception("The 2nd argument is not a .yml file.");
                }

                if (!System.IO.Directory.Exists(args[2]))
                {
                    throw new Exception("The 3rd argument is not a valid path.");
                }
                else
                {
                    log4net.GlobalContext.Properties["Log"] = $"{args[2]}/Temp";
                }
                log.Info("File extensions verified!");
            }
            catch (Exception e)
            {
                log.Error("InvalidArgs", e);
                log.Debug("To define the location of your input MasterConfig.json file and the output YamlOutput.yml file, edit the Project Properties. " +
                    "\n Click on the Debug tab and within Application arguments, add your file path to the json file, enter a space, and add your file path to the yaml file.");
                Exit(e.Message);
            }
        }

        /// <summary>
        /// This method reads in and deserializes the json input file.
        /// </summary>
        /// <param name="jsonDirectory">The json file path</param>
        /// <returns>A JsonInput object that stores the json input data</returns>
        public JsonInput readJsonFile(string jsonDirectory)
        {
            log.Info("Reading in Json file....");
            try
            {
                string masterConfig = System.IO.File.ReadAllText(jsonDirectory);
                JsonInput vaultList = JsonConvert.DeserializeObject<JsonInput>(masterConfig);

                JObject configVaults = JObject.Parse(masterConfig);
                checkJsonFields(vaultList, configVaults);
                checkMissingAadFields(vaultList, configVaults);
                checkMissingResourceFields(vaultList, configVaults);
                log.Info("Json file read!");
                return vaultList;
            }
            catch (Exception e)
            {
                log.Error("DeserializationFail", e);
                log.Debug("Refer to https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/Config/MasterConfigExample.jsonfor for questions on formatting and inputs. Ensure that you have all the required fields with valid values, then try again.");
                Exit(e.Message);
                return null;
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist within the Json file.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        public void checkJsonFields(JsonInput vaultList, JObject configVaults)
        {
            List<string> missingInputs = new List<string>();
            int numValid = 0;
            if (vaultList.AadAppKeyDetails != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("AadAppKeyDetails");
            }

            if (vaultList.Resources != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("Resources");
            }

            int numMissing = missingInputs.Count();
            if (missingInputs.Count() == 0 && configVaults.Children().Count() != numValid)
            {
                throw new Exception($"Invalid fields in Json were defined. Valid fields are 'AadAppKeyDetails' and 'Resources'.");
            }
            else if (missingInputs.Count() != 0 && configVaults.Children().Count() != numValid)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} in Json. Invalid fields were defined; " +
                    $"valid fields are 'AadAppKeyDetails' and 'Resources'.");
            }
            else if (missingInputs.Count() > 0)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} in Json.");
            }
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist for the AadAppKeyDetails object.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        public void checkMissingAadFields(JsonInput vaultList, JObject configVaults)
        {
            Tuple<List<string>, int> missing = getMissingInputs(vaultList);
            List<string> missingInputs = missing.Item1;
            int numValid = missing.Item2;
            int numMissing = missingInputs.Count();

            JToken aadDetails = configVaults.SelectToken($".AadAppKeyDetails");
            if (numMissing == 0 && (aadDetails.Children().Count() != numValid))
            {
                throw new Exception($"Invalid fields for AadAppKeyDetails were defined. " +
                    $"Valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.");
            }
            else if (numMissing != 0 && aadDetails.Children().Count() != numValid)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} for AadAppKeyDetails. Invalid fields were defined; " +
                    $"valid fields are 'AadAppName', 'VaultName', 'ClientIdSecretName', 'ClientKeySecretName', and 'TenantIdSecretName'.");
            }
            else if (numMissing > 0)
            {
                throw new Exception($"Missing {string.Join(" ,", missingInputs)} for AadAppKeyDetails.");
            }
        }

        /// <summary>
        /// This method returns a list of missing inputs from AadAppKeyDetails as well as the number of valid inputs.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <returns>A list of missing inputs from AadAppKeyDetails</returns>
        public Tuple<List<string>, int> getMissingInputs(JsonInput vaultList)
        {
            List<string> missingInputs = new List<string>();
            int numValid = 0;
            if (vaultList.AadAppKeyDetails.AadAppName != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("AadAppName");
            }

            if (vaultList.AadAppKeyDetails.VaultName != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("VaultName");
            }

            if (vaultList.AadAppKeyDetails.ClientIdSecretName != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("ClientIdSecretName");
            }

            if (vaultList.AadAppKeyDetails.ClientKeySecretName != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("ClientKeySecretName");
            }

            if (vaultList.AadAppKeyDetails.TenantIdSecretName != null)
            {
                ++numValid;
            }
            else
            {
                missingInputs.Add("TenantIdSecretName");
            }
            return new Tuple<List<string>, int>(missingInputs, numValid);
        }

        /// <summary>
        /// This method verifies that all of the required inputs exist for each Resource object.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtained from MasterConfig.json file</param>
        /// <param name="configVaults">The Json object formed from parsing the MasterConfig.json file</param>
        public void checkMissingResourceFields(JsonInput vaultList, JObject configVaults)
        {
            JEnumerable<JToken> resourceList = configVaults.SelectToken($".Resources").Children();

            int i = 0;
            foreach (Resource res in vaultList.Resources)
            {
                JToken jres = resourceList.ElementAt(i);

                if (res.SubscriptionId != null && res.ResourceGroups.Count() == 0 && jres.Children().Count() > 1)
                {
                    throw new Exception($"Invalid fields for Resource with SubscriptionId '{res.SubscriptionId}' were defined. Valid fields are 'SubscriptionId' and 'ResourceGroups'.");
                }
                else if (res.SubscriptionId == null && jres.Children().Count() > 0)
                {
                    throw new Exception($"Missing 'SubscriptionId' for Resource. Invalid fields were defined; valid fields are 'SubscriptionId' and 'ResourceGroups'.");
                }
                else if (res.SubscriptionId != null && res.ResourceGroups.Count() != 0)
                {
                    if (jres.Children().Count() > 2)
                    {
                        throw new Exception($"Invalid fields other than 'SubscriptionId' and 'ResourceGroups' were defined for Resource with SubscriptionId '{res.SubscriptionId}'.");
                    }

                    int j = 0;
                    foreach (ResourceGroup resGroup in res.ResourceGroups)
                    {
                        JEnumerable<JToken> groupList = jres.SelectToken($".ResourceGroups").Children();
                        JToken jresGroup = groupList.ElementAt(j);

                        if (resGroup.ResourceGroupName != null && resGroup.KeyVaults.Count() == 0 && jresGroup.Children().Count() > 1)
                        {
                            throw new Exception($"Invalid fields for ResourceGroup with ResourceGroupName '{resGroup.ResourceGroupName}' were defined. " +
                                $"Valid fields are 'ResourceGroupName' and 'KeyVaults'.");
                        }
                        else if (resGroup.ResourceGroupName == null && jresGroup.Children().Count() > 0)
                        {
                            throw new Exception("Missing 'ResourceGroupName' for ResourceGroup. Invalid fields were defined; valid fields are 'ResourceGroupName' and 'KeyVaults'.");
                        }
                        else if (resGroup.ResourceGroupName != null && resGroup.KeyVaults.Count() != 0 && jresGroup.Children().Count() > 2)
                        {
                            throw new Exception($"Invalid fields other than 'ResourceGroupName' and 'KeyVaults' were defined for ResourceGroup " +
                                $"with ResourceGroupName '{resGroup.ResourceGroupName}'.");
                        }
                        ++j;
                    }
                }
                ++i;
            }
        }

        /// <summary>
        /// This method retrieves the AadAppSecrets using a SecretClient and returns a Dictionary of the secrets.
        /// </summary>
        /// <param name="vaultList">The KeyVault information obtaind from MasterConfig.json file</param>
        /// <returns>The dictionary of secrets obtained from the SecretClient</returns>
        public Dictionary<string, string> getSecrets(JsonInput vaultList)
        {
            log.Info("Getting Secrets...");

            Dictionary<string, string> secrets = new Dictionary<string, string>();
            try
            {
                log.Info("Getting app Name...");
                secrets["appName"] = vaultList.AadAppKeyDetails.AadAppName;
                log.Info("App name retrieved!");

                // Creates the SecretClient and grabs secrets
                string keyVaultName = vaultList.AadAppKeyDetails.VaultName;
                string keyVaultUri = Constants.HTTP + keyVaultName + Constants.AZURE_URL;
                SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

                getSecret(secretClient, secrets, vaultList.AadAppKeyDetails.ClientIdSecretName, "clientId");
                getSecret(secretClient, secrets, vaultList.AadAppKeyDetails.ClientKeySecretName, "clientKey");
                getSecret(secretClient, secrets, vaultList.AadAppKeyDetails.TenantIdSecretName, "tenantId");

            }
            catch (Exception e)
            {
                log.Error($"AppName was NOT retrieved.", e);
                Exit(e.Message);
            }
            log.Info("Secrets retrieved!");
            return secrets;
        }

        /// <summary>
        /// This method provides error-handling for getting a secret value.
        /// </summary>
        /// <param name="secretClient">The SecretClient utilized to retrieve the secret value</param>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <param name="name">The name of the secret, specified in the MasterConfig.json file</param>
        /// <param name="key">The type of secret i.e. tenantId</param>
        private void getSecret(SecretClient secretClient, Dictionary<string, string> secrets, string name, string key)
        {
            try
            {
                log.Info($"Getting {key}...");
                KeyVaultSecret secret = secretClient.GetSecret(name);
                secrets[key] = secret.Value;
                log.Info($"{key} retrieved!");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("404"))
                {
                    log.Error($"{key}Secret could not be found");
                    Exit($"{key}Secret could not be found.");
                }
                else
                {
                    log.Error($"{key}Secret was not retrieved. {e.Message}.");
                    Exit($"{key}Secret {e.Message}.");
                }
            }
        }

        /// <summary>
        /// This method creates and returns a KeyVaulManagementClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The KeyVaultManagementClient created using the secret information</returns>
        public Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient createKVMClient(Dictionary<string, string> secrets)
        {
            log.Info("Creating KVM Client...");
            try
            {
                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(secrets["clientId"],
                    secrets["clientKey"], secrets["tenantId"], AzureEnvironment.AzureGlobalCloud);
                var kvmClient = new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient(credentials);
                log.Info("KVM Client created!");
                return kvmClient;
            }
            catch (Exception e)
            {
                log.Error("KVMClientFail", e);
                log.Debug($"Refer to https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.keyvault.keyvaultmanagementclient?view=azure-dotnet for information on KeyVaultMananagentClient class");
                Exit(e.Message);
                return null;
            }
        }

        /// <summary>
        /// This method creates and returns a GraphServiceClient.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The GraphServiceClient created using the secret information</returns>
        public GraphServiceClient createGraphClient(Dictionary<string, string> secrets)
        {
            log.Info("Creating Graph Client...");
            try
            {
                string auth = Constants.MICROSOFT_LOGIN + secrets["tenantId"];
                string redirectUri = Constants.HTTP + secrets["appName"];

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(secrets["clientId"])
                                                              .WithAuthority(auth)
                                                              .WithRedirectUri(redirectUri)
                                                              .WithClientSecret(secrets["clientKey"])
                                                              .Build();

                List<string> scopes = new List<string>()
                {
                    Constants.GRAPHCLIENT_URL
                };
                MsalAuthenticationProvider authProvider = new MsalAuthenticationProvider(cca, scopes.ToArray());
                var graphClient = new GraphServiceClient(authProvider);
                log.Info("Graph Client created!");
                return graphClient;
            }
            catch (Exception e)
            {
                log.Error("GraphClientFail", e);
                log.Debug($"Refer to https://docs.microsoft.com/en-us/graph/sdks/create-client?tabs=CS for information on creating a graph client");
                Exit(e.Message);
                return null;
            }
        }

        /// <summary>
        /// This method creates and returns an azure client.
        /// </summary>
        /// <param name="secrets">The dictionary of information obtained from SecretClient</param>
        /// <returns>The azure client created using the secret information</returns>
        public Microsoft.Azure.Management.Fluent.Azure.IAuthenticated createAzureClient(Dictionary<string, string> secrets)
        {
            log.Info("Creating Azure Client...");
            try
            {
                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(secrets["clientId"],
                   secrets["clientKey"], secrets["tenantId"], AzureEnvironment.AzureGlobalCloud);
                var azureClient = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials);

                log.Info("Azure Client created!");
                return azureClient;
            }
            catch (Exception e)
            {
                log.Error("AzureClientFail", e);
                log.Debug($"Azure client was unable to be created due to a failure with the AzureCredentials. Please verify that the ClientId, ClientKey, and TenantId secrets are correct.");
                Exit(e.Message);
                return null;
            }
        }

        /// <summary>
        /// This method verifies that the Contributor permission has been granted on sufficient scopes to retrieve the key vaults.
        /// </summary>
        /// <param name="vaultList">The data obtained from deserializing json file</param>
        /// <param name="azureClient">The IAzure client used to access role assignments</param>
        public void checkAccess(JsonInput vaultList, Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azureClient)
        {
            log.Info("Verifying access to Vaults...");
            List<string> accessNeeded = new List<string>();
            IRoleAssignments accessControl = azureClient.RoleAssignments;
            foreach (Resource res in vaultList.Resources)
            {
                try
                {
                    string subsPath = Constants.SUBS_PATH + res.SubscriptionId;
                    var roleAssignments = accessControl.ListByScope(subsPath).ToLookup(r => r.Inner.Scope);

                    var subsAccess = roleAssignments[subsPath].Count();
                    if (subsAccess == 0)
                    {
                        // At Subscription scope
                        if (res.ResourceGroups.Count == 0)
                        {
                            accessNeeded.Add(subsPath);
                        }
                        else
                        {
                            foreach (ResourceGroup resGroup in res.ResourceGroups)
                            {
                                string resGroupPath = subsPath + Constants.RESGROUP_PATH + resGroup.ResourceGroupName;
                                var resGroupAccess = roleAssignments[resGroupPath].Count();
                                if (resGroupAccess == 0)
                                {
                                    // At ResourceGroup scope
                                    if (resGroup.KeyVaults.Count == 0)
                                    {
                                        accessNeeded.Add(subsPath);
                                    }
                                    else
                                    {
                                        // At Vault scope
                                        foreach (string vaultName in resGroup.KeyVaults)
                                        {
                                            string vaultPath = resGroupPath + Constants.VAULT_PATH + vaultName;
                                            var vaultAccess = roleAssignments[vaultPath].Count();
                                            if (vaultAccess == 0)
                                            {
                                                accessNeeded.Add(vaultPath);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (CloudException e)
                {
                    log.Error("SubscriptionNotFound");
                    log.Debug($"{e.Message}. Please verify that your SubscriptionId is valid.");
                    Exit(e.Message);
                }
            }

            if (accessNeeded.Count() != 0)
            {
                log.Error("AuthorizationFail");
                log.Debug($"Contributor access is needed on the following scope(s): \n{string.Join("\n", accessNeeded)}. \nEnsure that your ResourceGroup and KeyVault names are spelled correctly " +
                    $"before proceeding. Note that if you are retrieving specific KeyVaults, your AAD must be granted access at either the KeyVault, ResourceGroup, Subscription level. " +
                    $"If you are retrieving all of the KeyVaults from a ResourceGroup, your AAD must be granted access at either the ResourceGroup or Subscription level. " +
                    $"If you are retrieving all of the KeyVaults from a SubscriptionId, your AAD must be granted access at the Subscription level. " +
                    $"Refer to the 'Granting Access to the AAD Application' section for more information on granting this access: https://github.com/microsoft/Managing-RBAC-in-Azure/blob/master/README.md");
                Exit($"Contributor access is needed on the following scope(s): \n{string.Join("\n", accessNeeded)}");
            }
            log.Info("Access verified!");
        }

        /// <summary>
        /// This method retrieves each of the KeyVaults specified in the vaultList.
        /// </summary>
        /// <param name="vaultList">The data obtained from deserializing json file</param>
        /// <param name="kvmClient">The KeyVaultManagementClient containing Vaults</param>
        /// <param name="graphClient">The Microsoft GraphServiceClient for obtaining display names</param>
        /// <returns>The list of KeyVaultProperties containing the properties of each KeyVault</returns>
        public List<KeyVaultProperties> getVaults(JsonInput vaultList,
            Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient, GraphServiceClient graphClient)
        {
            log.Info("Getting Vaults...");
            List<Vault> vaultsRetrieved = new List<Vault>();

            foreach (Resource res in vaultList.Resources)
            {
                log.Info($"Entering SubscriptionID: {res.SubscriptionId}");

                // Associates the client with the subscription
                kvmClient.SubscriptionId = res.SubscriptionId;

                // Retrieves all KeyVaults at the Subscription scope
                if (res.ResourceGroups.Count == 0)
                {
                    vaultsRetrieved = getVaultsAllPages(kvmClient, vaultsRetrieved);
                }
                else
                {
                    bool notFound = false;
                    foreach (ResourceGroup resGroup in res.ResourceGroups)
                    {
                        log.Info($"Entering ResourceGroup: {resGroup.ResourceGroupName}");
                        // If the Subscription is not found, then do not continue looking for vaults in this subscription
                        if (notFound)
                        {
                            break;
                        }

                        // Retrieves all KeyVaults at the ResourceGroup scope
                        if (resGroup.KeyVaults.Count == 0)
                        {
                            vaultsRetrieved = getVaultsAllPages(kvmClient, vaultsRetrieved, resGroup.ResourceGroupName);
                        }
                        // Retrieves all KeyVaults at the Resource scope
                        else
                        {
                            foreach (string vaultName in resGroup.KeyVaults)
                            {
                                log.Info($"Entering VaultName: {vaultName}");
                                try
                                {
                                    vaultsRetrieved.Add(kvmClient.Vaults.Get(resGroup.ResourceGroupName, vaultName));
                                }
                                catch (CloudException e)
                                {
                                    log.Error(e.Message);
                                    ConsoleError(e.Message);
                                    if (e.Body.Code == "SubscriptionNotFound")
                                    {
                                        notFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            List<KeyVaultProperties> keyVaultsRetrieved = new List<KeyVaultProperties>();
            foreach (Vault curVault in vaultsRetrieved)
            {
                keyVaultsRetrieved.Add(new KeyVaultProperties(curVault, graphClient));
            }
            log.Info("Vaults retrieved!");
            return keyVaultsRetrieved;
        }

        /// <summary>
        /// This method retrieves the KeyVaults from all the pages of KeyVaults as one page can only store a limited number of KeyVaults.
        /// </summary>
        /// <param name="kvmClient">The KeyVaultManagementClient</param>
        /// <param name="vaultsRetrieved">The list of Vault objects to add to</param>
        /// <param name="resourceGroup">The ResourceGroup name(if applicable). Default is null.</param>
        /// <returns>The updated vaultsRetrieved list</returns>
        public List<Vault> getVaultsAllPages(Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient kvmClient,
            List<Vault> vaultsRetrieved, string resourceGroup = "")
        {
            IPage<Vault> vaultsCurPg = null;
            // Retrieves the first page of KeyVaults at the Subscription scope
            if (resourceGroup.Length == 0)
            {
                try
                {
                    vaultsCurPg = kvmClient.Vaults.ListBySubscription();
                }
                catch (CloudException e)
                {
                    log.Error(e.Message);
                    ConsoleError(e.Message);
                }
            }
            // Retrieves the first page of KeyVaults at the ResourceGroup scope
            else
            {
                try
                {
                    vaultsCurPg = kvmClient.Vaults.ListByResourceGroup(resourceGroup);
                }
                catch (CloudException e)
                {
                    log.Error(e.Message);
                    ConsoleError(e.Message);
                }
            }

            // Get remaining pages if vaults were found
            if (vaultsCurPg != null)
            {
                vaultsRetrieved.AddRange(vaultsCurPg);
                while (vaultsCurPg.NextPageLink != null)
                {
                    IPage<Vault> vaultsNextPg = null;
                    // Retrieves the remaining pages of KeyVaults at the Subscription scope
                    if (resourceGroup.Length == 0) // then by Subscription
                    {
                        vaultsNextPg = kvmClient.Vaults.ListBySubscriptionNext(vaultsCurPg.NextPageLink);
                    }
                    // Retrieves the remaining pages of KeyVaults at the ResourceGroup scope
                    else
                    {
                        vaultsNextPg = kvmClient.Vaults.ListByResourceGroupNext(vaultsCurPg.NextPageLink);
                    }
                    vaultsRetrieved.AddRange(vaultsNextPg);
                    vaultsCurPg = vaultsNextPg;
                }
            }
            return vaultsRetrieved;
        }

        /// <summary>
        /// This method converts the Vault objects to KeyVaultProperties objects, serializes the list of objects, and outputs the YAML.
        /// </summary>
        /// <param name="vaultsRetrieved">The list of KeyVaultProperties to serialize</param>
        /// <param name="yamlDirectory"> The directory of the outputted yaml file </param>
        public void convertToYaml(List<KeyVaultProperties> vaultsRetrieved, string yamlDirectory)
        {
            log.Info("Converting to YAML...");
            try
            {
                var serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(vaultsRetrieved);

                System.IO.File.WriteAllText(yamlDirectory, yaml);
                log.Info("YAML created!");
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
        /// This method throws an exception instead of exiting the program when Testing is true. 
        /// Otherwise, if Testing is false, the program exits.
        /// </summary>
        /// <param name="message">The error message to print to the console</param>
        public void Exit(string message)
        {
            if (!Testing)
            {
                ConsoleError(message);
                log.Info("Progam exited.");
                var path = log4net.GlobalContext.Properties["Log"] as string;
                var logged = System.IO.File.ReadAllText($"{path}.log");
                log.Logger.Repository.Shutdown();
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
            Console.ResetColor();
        }

        // This field indicates if unit tests are being run
        public bool Testing { get; set; }
        // This field defines the logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}