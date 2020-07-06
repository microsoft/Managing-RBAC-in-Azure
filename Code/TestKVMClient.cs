using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest.Azure;
using RBAC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace RBAC
{
    public class TestKVMClient : KeyVaultManagementClient
    {
        public TestKVMClient()
        {
            Vaults = new TestVaults();
        }

        public override IVaultsOperations Vaults { get; }
        public new string SubscriptionId { set { TestKVMClient.subid = value; } }
        public static string subid = "00000000-0000-0000-0000-000000000000";
    }
    public class TestVaults : IVaultsOperations
    {
        public TestVaults()
        {
            string yaml = System.IO.File.ReadAllText("../../../expected/ExpectedOutput.yml");
            var deserializer = new DeserializerBuilder().Build();
            KeyVaults = deserializer.Deserialize<List<KeyVaultProperties>>(yaml);
            Updated = new List<KeyVaultProperties>();
        }
        public Task<AzureOperationResponse<Vault>> BeginCreateOrUpdateWithHttpMessagesAsync(string resourceGroupName, string vaultName, VaultCreateOrUpdateParameters parameters, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse> BeginPurgeDeletedWithHttpMessagesAsync(string vaultName, string location, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<CheckNameAvailabilityResult>> CheckNameAvailabilityWithHttpMessagesAsync(VaultCheckNameAvailabilityParameters vaultName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<Vault>> CreateOrUpdateWithHttpMessagesAsync(string resourceGroupName, string vaultName, VaultCreateOrUpdateParameters parameters, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            return Task<AzureOperationResponse<Vault>>.Factory.StartNew(() =>
            {
                return new AzureOperationResponse<Vault>
                {
                    Body = CreateOrUpdateAsync(resourceGroupName, vaultName, parameters).Result
                };
            });
        }
        public Task<Vault> CreateOrUpdateAsync(string resourceGroupName, string vaultName, VaultCreateOrUpdateParameters parameters, CancellationToken cancellationToken = default)
        {
            var aps = new List<PrincipalPermissions>();
            foreach (AccessPolicyEntry ap in parameters.Properties.AccessPolicies)
            {
                aps.Add(new PrincipalPermissions(ap, new TestGraphClient(new MsalAuthenticationProvider())));
            }
            Updated.Add(new KeyVaultProperties
            {
                ResourceGroupName = resourceGroupName,
                VaultName = vaultName,
                Location = "eastus",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                TenantId = "00000000-0000-0000-0000-000000000000",
                AccessPolicies = aps,
            });
            return Task<Vault>.Factory.StartNew(() =>
            {
                return null;
            });
        }
        public Task<AzureOperationResponse> DeleteWithHttpMessagesAsync(string resourceGroupName, string vaultName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<DeletedVault>> GetDeletedWithHttpMessagesAsync(string vaultName, string location, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<Vault>> GetWithHttpMessagesAsync(string resourceGroupName, string vaultName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            Task<AzureOperationResponse<Vault>> ret = Task<AzureOperationResponse<Vault>>.Factory.StartNew(() =>
            {
                return new AzureOperationResponse<Vault>
                {
                    Body = Get(resourceGroupName, vaultName),
                };
            });
            ret.Wait();
            return ret;
        }
        public Vault Get(string resourceGroupName, string vaultName)
        {
            var vault = KeyVaults.ToLookup(kv => kv.VaultName)[vaultName].First();
            var aps = new List<AccessPolicyEntry>();
            foreach (PrincipalPermissions pp in vault.AccessPolicies)
            {
                var objectId = "";
                if (pp.Type.ToLower() == "user")
                {
                    if (pp.DisplayName.ToLower().Contains('a'))
                    {
                        objectId = "ua";
                    }
                    else if (pp.DisplayName.ToLower().Contains('b'))
                    {
                        objectId = "ub";
                    }
                    else if (pp.DisplayName.ToLower().Contains('c'))
                    {
                        objectId = "uc";
                    }
                }
                else
                {
                    objectId = pp.DisplayName;
                }
                aps.Add(new AccessPolicyEntry
                {
                    ObjectId = objectId,
                    TenantId = new Guid("00000000-0000-0000-0000-000000000000"),
                    Permissions = new Permissions
                    {
                        Certificates = pp.PermissionsToCertificates,
                        Secrets = pp.PermissionsToSecrets,
                        Keys = pp.PermissionsToKeys,
                    }
                });
            }
            var properties = new VaultProperties
            {
                AccessPolicies = aps,
                TenantId = new Guid("00000000-0000-0000-0000-000000000000"),
            };
            var ret = new Vault(properties, $"subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{resourceGroupName}", vault.VaultName, null, "eastus", null);
            return ret;
        }
        public Task<AzureOperationResponse<IPage<Vault>>> ListByResourceGroupNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            Task<AzureOperationResponse<IPage<Vault>>> ret = Task<AzureOperationResponse<IPage<Vault>>>.Factory.StartNew(() =>
            {
                return new AzureOperationResponse<IPage<Vault>>
                {
                    Body = ListByResourceGroupNext(""),
                };
            });
            ret.Wait();
            return ret;
        }
        public IPage<Vault> ListByResourceGroup(string resourceGroupName, int? top = null)
        {
            TestPage<Vault> ret = new TestPage<Vault>();
            var vaults = KeyVaults.ToLookup(v => v.ResourceGroupName)[resourceGroupName];
            foreach(KeyVaultProperties kv in vaults)
            {
                var aps = new List<AccessPolicyEntry>();
                foreach (PrincipalPermissions pp in kv.AccessPolicies)
                {
                    var objectId = "";
                    if(pp.Type.ToLower() == "user"){
                        if (pp.DisplayName.ToLower().Contains('a'))
                        {
                            objectId = "ua";
                        }
                        else if (pp.DisplayName.ToLower().Contains('b'))
                        {
                            objectId = "ub";
                        }
                        else if (pp.DisplayName.ToLower().Contains('c'))
                        {
                            objectId = "uc";
                        }
                    }
                    else
                    {
                        objectId = pp.DisplayName;
                    }
                    aps.Add(new AccessPolicyEntry
                    {
                        ObjectId = objectId,
                        TenantId = new Guid("00000000-0000-0000-0000-000000000000"),
                        Permissions = new Permissions
                        {
                            Certificates = pp.PermissionsToCertificates,
                            Secrets = pp.PermissionsToSecrets,
                            Keys = pp.PermissionsToKeys,
                        }
                    });
                }
                var properties = new VaultProperties
                {
                    AccessPolicies = aps,
                    TenantId = new Guid("00000000-0000-0000-0000-000000000000"),
                };
                ret.Add(new Vault(properties, $"subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{resourceGroupName}", kv.VaultName, null, "eastus", null));
                
            }
            return ret;
        }
        public IPage<Vault> ListByResourceGroupNext(string nextPageLink)
        {
            return null;
        }
        public IPage<Vault> ListBySubscription(int? top = null)
        {
            TestPage<Vault> ret = new TestPage<Vault>();
            var vaults = KeyVaults.ToLookup(v => v.SubscriptionId)[TestKVMClient.subid];
            foreach (KeyVaultProperties kv in vaults)
            {
                var aps = new List<AccessPolicyEntry>();
                foreach (PrincipalPermissions pp in kv.AccessPolicies)
                {
                    var objectId = "";
                    if (pp.Type.ToLower() == "user")
                    {
                        if (pp.DisplayName.ToLower().Contains('a'))
                        {
                            objectId = "ua";
                        }
                        else if (pp.DisplayName.ToLower().Contains('b'))
                        {
                            objectId = "ub";
                        }
                        else if (pp.DisplayName.ToLower().Contains('c'))
                        {
                            objectId = "uc";
                        }
                    }
                    else
                    {
                        objectId = pp.DisplayName;
                    }
                    aps.Add(new AccessPolicyEntry
                    {
                        ObjectId = objectId,
                        TenantId = new Guid("00000000-0000-0000-0000-000000000000"),
                        Permissions = new Permissions
                        {
                            Certificates = pp.PermissionsToCertificates,
                            Secrets = pp.PermissionsToSecrets,
                            Keys = pp.PermissionsToKeys,
                        }
                    });
                }
                var properties = new VaultProperties
                {
                    AccessPolicies = aps,
                    TenantId = new Guid("00000000-0000-0000-0000-000000000000"),
                };
                ret.Add(new Vault(properties, $"subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{kv.ResourceGroupName}", kv.VaultName, null, "eastus", null));

            }
            return ret;
        }
        public IPage<Vault> ListBySubscriptionNext(string nextPageLink)
        {
            return null;
        }
        public Task<AzureOperationResponse<IPage<Vault>>> ListByResourceGroupWithHttpMessagesAsync(string resourceGroupName, int? top = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            Task<AzureOperationResponse<IPage<Vault>>> ret = Task<AzureOperationResponse<IPage<Vault>>>.Factory.StartNew(() =>
            {
                return new AzureOperationResponse<IPage<Vault>>
                {
                    Body = ListByResourceGroup(resourceGroupName),
                };
            });
            ret.Wait();
            return ret;
        }

        public Task<AzureOperationResponse<IPage<Vault>>> ListBySubscriptionNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            Task<AzureOperationResponse<IPage<Vault>>> ret = Task<AzureOperationResponse<IPage<Vault>>>.Factory.StartNew(() =>
            {
                return new AzureOperationResponse<IPage<Vault>>
                {
                    Body = ListBySubscriptionNext(""),
                };
            });
            ret.Wait();
            return ret;
        }

        public Task<AzureOperationResponse<IPage<Vault>>> ListBySubscriptionWithHttpMessagesAsync(int? top = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            var a = ListBySubscription();
            Task<AzureOperationResponse<IPage<Vault>>> ret = Task<AzureOperationResponse<IPage<Vault>>>.Factory.StartNew(() =>
            {
                return new AzureOperationResponse<IPage<Vault>>
                {
                    Body = a,
                };
            });
            ret.Wait();
            return ret;
        }

        public Task<AzureOperationResponse<IPage<DeletedVault>>> ListDeletedNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<IPage<DeletedVault>>> ListDeletedWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<IPage<Microsoft.Azure.Management.KeyVault.Models.Resource>>> ListNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<IPage<Microsoft.Azure.Management.KeyVault.Models.Resource>>> ListWithHttpMessagesAsync(int? top = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse> PurgeDeletedWithHttpMessagesAsync(string vaultName, string location, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<VaultAccessPolicyParameters>> UpdateAccessPolicyWithHttpMessagesAsync(string resourceGroupName, string vaultName, AccessPolicyUpdateKind operationKind, VaultAccessPolicyParameters parameters, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<Vault>> UpdateWithHttpMessagesAsync(string resourceGroupName, string vaultName, VaultPatchParameters parameters, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        Task<AzureOperationResponse<IPage<Microsoft.Azure.Management.KeyVault.Models.Resource>>> IVaultsOperations.ListWithHttpMessagesAsync(int? top, Dictionary<string, List<string>> customHeaders, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<AzureOperationResponse<IPage<Microsoft.Azure.Management.KeyVault.Models.Resource>>> IVaultsOperations.ListNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public List<KeyVaultProperties> KeyVaults;
        public List<KeyVaultProperties> Updated;
    }
    public class TestPage<T> : IPage<T>
    {
        public TestPage()
        {
            list = new List<T>();
        }
        public string NextPageLink => null;

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
        public void Add(T t)
        {
            list.Add(t);
        }
        private List<T> list;
    }
}
