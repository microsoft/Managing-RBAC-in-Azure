using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RBAC
{
    public class TestGraphClient : GraphServiceClient
    {
        public TestGraphClient(IAuthenticationProvider ia) : base(ia)
        {
            Users = new TestUsersCollectionBuilder();
            Applications = new TestAppsCollectionBuilder();
            Groups = new TestGroupsCollectionBuilder();
            ServicePrincipals = new TestSpsCollectionBuilder();
        }
        public new IGraphServiceUsersCollectionRequestBuilder Users { get; }
        public new IGraphServiceApplicationsCollectionRequestBuilder Applications { get; }
        public new IGraphServiceGroupsCollectionRequestBuilder Groups { get; }
        public new IGraphServiceServicePrincipalsCollectionRequestBuilder ServicePrincipals { get; }
    }

    internal class TestSpsCollectionBuilder : IGraphServiceServicePrincipalsCollectionRequestBuilder
    {
        public IServicePrincipalRequestBuilder this[string id] => new TestSpRequestBuilder(id);

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Request()
        {
            return new TestSpsCollectionRequest();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestSpsCollectionRequest : IGraphServiceServicePrincipalsCollectionRequest
    {
        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public string Id { get; private set; }

        public Task<ServicePrincipal> AddAsync(ServicePrincipal servicePrincipal)
        {
            throw new NotImplementedException();
        }

        public Task<ServicePrincipal> AddAsync(ServicePrincipal servicePrincipal, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Expand(Expression<Func<ServicePrincipal, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Filter(string value)
        {
            Id = value.Substring(value.IndexOf('\'') + 1);
            return this;
        }

        public Task<IGraphServiceServicePrincipalsCollectionPage> GetAsync()
        {
            Task<IGraphServiceServicePrincipalsCollectionPage> ret = Task<IGraphServiceServicePrincipalsCollectionPage>.Factory.StartNew(() =>
            {
                if (Id.ToLower().StartsWith("sp1"))
                {
                    var page = new GraphServiceServicePrincipalsCollectionPage();
                    page.Add(new ServicePrincipal
                    {
                        DisplayName = "SP1",
                        Id = "SP1"
                    });
                    return page;
                }
                else
                {
                    throw new Exception("out of range sp");
                }
            });
            return ret;
        }

        public Task<IGraphServiceServicePrincipalsCollectionPage> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest OrderBy(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Select(Expression<Func<ServicePrincipal, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Skip(int value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceServicePrincipalsCollectionRequest Top(int value)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestSpRequestBuilder : IServicePrincipalRequestBuilder
    {
        private string id;

        public TestSpRequestBuilder(string id)
        {
            this.id = id;
        }

        public IServicePrincipalAppRoleAssignedToCollectionRequestBuilder AppRoleAssignedTo => throw new NotImplementedException();

        public IServicePrincipalAppRoleAssignmentsCollectionRequestBuilder AppRoleAssignments => throw new NotImplementedException();

        public IServicePrincipalEndpointsCollectionRequestBuilder Endpoints => throw new NotImplementedException();

        public IServicePrincipalOauth2PermissionGrantsCollectionWithReferencesRequestBuilder Oauth2PermissionGrants => throw new NotImplementedException();

        public IServicePrincipalMemberOfCollectionWithReferencesRequestBuilder MemberOf => throw new NotImplementedException();

        public IServicePrincipalTransitiveMemberOfCollectionWithReferencesRequestBuilder TransitiveMemberOf => throw new NotImplementedException();

        public IServicePrincipalCreatedObjectsCollectionWithReferencesRequestBuilder CreatedObjects => throw new NotImplementedException();

        public IServicePrincipalOwnersCollectionWithReferencesRequestBuilder Owners => throw new NotImplementedException();

        public IServicePrincipalOwnedObjectsCollectionWithReferencesRequestBuilder OwnedObjects => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IServicePrincipalAddKeyRequestBuilder AddKey(KeyCredential keyCredential, string proof, PasswordCredential passwordCredential = null)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalAddPasswordRequestBuilder AddPassword(PasswordCredential passwordCredential = null)
        {
            throw new NotImplementedException();
        }

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberGroupsRequestBuilder CheckMemberGroups(IEnumerable<string> groupIds)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberObjectsRequestBuilder CheckMemberObjects(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberGroupsRequestBuilder GetMemberGroups(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberObjectsRequestBuilder GetMemberObjects(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRemoveKeyRequestBuilder RemoveKey(Guid keyId, string proof)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRemovePasswordRequestBuilder RemovePassword(Guid keyId)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRequest Request()
        {
            return new TestSpRequest(id);
        }

        public IServicePrincipalRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectRestoreRequestBuilder Restore()
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestSpRequest : IServicePrincipalRequest
    {
        private string id;

        public TestSpRequest(string id)
        {
            this.id = id;
        }

        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public Task<ServicePrincipal> CreateAsync(ServicePrincipal servicePrincipalToCreate)
        {
            throw new NotImplementedException();
        }

        public Task<ServicePrincipal> CreateAsync(ServicePrincipal servicePrincipalToCreate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRequest Expand(Expression<Func<ServicePrincipal, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public Task<ServicePrincipal> GetAsync()
        {
            Task<ServicePrincipal> ret = Task<ServicePrincipal>.Factory.StartNew(() =>
            {
                if (id.ToLower().StartsWith("sp1"))
                {
                    var page = new ServicePrincipal
                    {
                        Id = "SP1",
                        DisplayName = "SP1"
                    };
                    return page;
                }
                else
                {
                    throw new Exception("out of range sp");
                }
            });
            return ret;
        }

        public Task<ServicePrincipal> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IServicePrincipalRequest Select(Expression<Func<ServicePrincipal, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public Task<ServicePrincipal> UpdateAsync(ServicePrincipal servicePrincipalToUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<ServicePrincipal> UpdateAsync(ServicePrincipal servicePrincipalToUpdate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestGroupsCollectionBuilder : IGraphServiceGroupsCollectionRequestBuilder
    {
        public IGroupRequestBuilder this[string id] => new TestGroupRequestBuilder(id);

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IGroupDeltaRequestBuilder Delta()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Request()
        {
            return new TestGroupsCollectionRequest();
        }

        public IGraphServiceGroupsCollectionRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestGroupsCollectionRequest : IGraphServiceGroupsCollectionRequest
    {
        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public string Id { get; private set; }

        public Task<Group> AddAsync(Group group)
        {
            throw new NotImplementedException();
        }

        public Task<Group> AddAsync(Group group, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Expand(Expression<Func<Group, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Filter(string value)
        {
            Id = value.Substring(value.IndexOf('\'') + 1);
            return this;
        }

        public Task<IGraphServiceGroupsCollectionPage> GetAsync()
        {
            Task<IGraphServiceGroupsCollectionPage> ret = Task<IGraphServiceGroupsCollectionPage>.Factory.StartNew(() =>
            {
                if (Id.StartsWith("g1"))
                {
                    var page = new GraphServiceGroupsCollectionPage();
                    page.Add(new Group
                    {
                        Mail = "g1@valid.com",
                        Id = "g1",
                        DisplayName = "g1"
                    });
                    return page;
                }
                else
                {
                    throw new Exception("out of range group");
                }
            });
            return ret;
        }

        public Task<IGraphServiceGroupsCollectionPage> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest OrderBy(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Select(Expression<Func<Group, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Skip(int value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceGroupsCollectionRequest Top(int value)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestGroupRequestBuilder : IGroupRequestBuilder
    {
        private string id;

        public TestGroupRequestBuilder(string id)
        {
            this.id = id;
        }

        public IGroupAppRoleAssignmentsCollectionRequestBuilder AppRoleAssignments => throw new NotImplementedException();

        public IGroupMembersCollectionWithReferencesRequestBuilder Members => throw new NotImplementedException();

        public IGroupMemberOfCollectionWithReferencesRequestBuilder MemberOf => throw new NotImplementedException();

        public IGroupMembersWithLicenseErrorsCollectionWithReferencesRequestBuilder MembersWithLicenseErrors => throw new NotImplementedException();

        public IGroupTransitiveMembersCollectionWithReferencesRequestBuilder TransitiveMembers => throw new NotImplementedException();

        public IGroupTransitiveMemberOfCollectionWithReferencesRequestBuilder TransitiveMemberOf => throw new NotImplementedException();

        public IDirectoryObjectWithReferenceRequestBuilder CreatedOnBehalfOf => throw new NotImplementedException();

        public IGroupOwnersCollectionWithReferencesRequestBuilder Owners => throw new NotImplementedException();

        public IGroupSettingsCollectionRequestBuilder Settings => throw new NotImplementedException();

        public IGroupConversationsCollectionRequestBuilder Conversations => throw new NotImplementedException();

        public IGroupPhotosCollectionRequestBuilder Photos => throw new NotImplementedException();

        public IGroupAcceptedSendersCollectionRequestBuilder AcceptedSenders => throw new NotImplementedException();

        public IGroupRejectedSendersCollectionRequestBuilder RejectedSenders => throw new NotImplementedException();

        public IGroupThreadsCollectionRequestBuilder Threads => throw new NotImplementedException();

        public ICalendarRequestBuilder Calendar => throw new NotImplementedException();

        public IGroupCalendarViewCollectionRequestBuilder CalendarView => throw new NotImplementedException();

        public IGroupEventsCollectionRequestBuilder Events => throw new NotImplementedException();

        public IProfilePhotoRequestBuilder Photo => throw new NotImplementedException();

        public IDriveRequestBuilder Drive => throw new NotImplementedException();

        public IGroupDrivesCollectionRequestBuilder Drives => throw new NotImplementedException();

        public IGroupSitesCollectionRequestBuilder Sites => throw new NotImplementedException();

        public IGroupExtensionsCollectionRequestBuilder Extensions => throw new NotImplementedException();

        public IGroupGroupLifecyclePoliciesCollectionRequestBuilder GroupLifecyclePolicies => throw new NotImplementedException();

        public IPlannerGroupRequestBuilder Planner => throw new NotImplementedException();

        public IOnenoteRequestBuilder Onenote => throw new NotImplementedException();

        public ITeamRequestBuilder Team => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IGroupAddFavoriteRequestBuilder AddFavorite()
        {
            throw new NotImplementedException();
        }

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IGroupAssignLicenseRequestBuilder AssignLicense(IEnumerable<AssignedLicense> addLicenses, IEnumerable<Guid> removeLicenses)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberGroupsRequestBuilder CheckMemberGroups(IEnumerable<string> groupIds)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberObjectsRequestBuilder CheckMemberObjects(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberGroupsRequestBuilder GetMemberGroups(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberObjectsRequestBuilder GetMemberObjects(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IGroupRemoveFavoriteRequestBuilder RemoveFavorite()
        {
            throw new NotImplementedException();
        }

        public IGroupRenewRequestBuilder Renew()
        {
            throw new NotImplementedException();
        }

        public IGroupRequest Request()
        {
            return new TestGroupRequest(id);
        }

        public IGroupRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        public IGroupResetUnseenCountRequestBuilder ResetUnseenCount()
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectRestoreRequestBuilder Restore()
        {
            throw new NotImplementedException();
        }

        public IGroupSubscribeByMailRequestBuilder SubscribeByMail()
        {
            throw new NotImplementedException();
        }

        public IGroupUnsubscribeByMailRequestBuilder UnsubscribeByMail()
        {
            throw new NotImplementedException();
        }

        public IGroupValidatePropertiesRequestBuilder ValidateProperties(string displayName = null, string mailNickname = null, Guid? onBehalfOfUserId = null)
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestGroupRequest : IGroupRequest
    {
        private string id;

        public TestGroupRequest(string id)
        {
            this.id = id;
        }

        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public Task<Group> CreateAsync(Group groupToCreate)
        {
            throw new NotImplementedException();
        }

        public Task<Group> CreateAsync(Group groupToCreate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IGroupRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IGroupRequest Expand(Expression<Func<Group, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public Task<Group> GetAsync()
        {
            Task<Group> ret = Task<Group>.Factory.StartNew(() =>
            {
                if (id.StartsWith("g1"))
                {
                    var page = new Group
                    {
                        Mail = "g1@valid.com",
                        Id = "g1",
                        DisplayName = "g1"
                    };
                    return page;
                }
                else
                {
                    throw new Exception("out of range group");
                }
            });
            return ret;
        }

        public Task<Group> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IGroupRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IGroupRequest Select(Expression<Func<Group, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public Task<Group> UpdateAsync(Group groupToUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<Group> UpdateAsync(Group groupToUpdate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestAppsCollectionBuilder : IGraphServiceApplicationsCollectionRequestBuilder
    {
        public IApplicationRequestBuilder this[string id] => new TestAppRequestBuilder(id);

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IApplicationDeltaRequestBuilder Delta()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Request()
        {
            return new TestAppsCollectionRequest();
        }

        public IGraphServiceApplicationsCollectionRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestAppsCollectionRequest : IGraphServiceApplicationsCollectionRequest
    {
        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public string Id { get; private set; }

        public Task<Application> AddAsync(Application application)
        {
            throw new NotImplementedException();
        }

        public Task<Application> AddAsync(Application application, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Expand(Expression<Func<Application, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Filter(string value)
        {
            Id = value.Substring(value.IndexOf('\'') + 1);
            return this;
        }

        public Task<IGraphServiceApplicationsCollectionPage> GetAsync()
        {
            Task<IGraphServiceApplicationsCollectionPage> ret = Task<IGraphServiceApplicationsCollectionPage>.Factory.StartNew(() =>
            {
                if (Id.StartsWith("a1"))
                {
                    var page = new GraphServiceApplicationsCollectionPage();
                    page.Add(new Application
                    {
                        Id = "a1",
                        DisplayName = "a1"
                    });
                    return page;
                }
                else
                {
                    throw new Exception("out of range app");
                }
            });
            return ret;
        }

        public Task<IGraphServiceApplicationsCollectionPage> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest OrderBy(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Select(Expression<Func<Application, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Skip(int value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceApplicationsCollectionRequest Top(int value)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestAppRequestBuilder : IApplicationRequestBuilder
    {
        private string id;

        public TestAppRequestBuilder(string id)
        {
            this.id = id;
        }

        public IApplicationExtensionPropertiesCollectionRequestBuilder ExtensionProperties => throw new NotImplementedException();

        public IDirectoryObjectWithReferenceRequestBuilder CreatedOnBehalfOf => throw new NotImplementedException();

        public IApplicationOwnersCollectionWithReferencesRequestBuilder Owners => throw new NotImplementedException();

        public IApplicationTokenLifetimePoliciesCollectionWithReferencesRequestBuilder TokenLifetimePolicies => throw new NotImplementedException();

        public IApplicationTokenIssuancePoliciesCollectionWithReferencesRequestBuilder TokenIssuancePolicies => throw new NotImplementedException();

        public IApplicationLogoRequestBuilder Logo => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IApplicationAddKeyRequestBuilder AddKey(KeyCredential keyCredential, string proof, PasswordCredential passwordCredential = null)
        {
            throw new NotImplementedException();
        }

        public IApplicationAddPasswordRequestBuilder AddPassword(PasswordCredential passwordCredential = null)
        {
            throw new NotImplementedException();
        }

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberGroupsRequestBuilder CheckMemberGroups(IEnumerable<string> groupIds)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberObjectsRequestBuilder CheckMemberObjects(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberGroupsRequestBuilder GetMemberGroups(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberObjectsRequestBuilder GetMemberObjects(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IApplicationRemoveKeyRequestBuilder RemoveKey(Guid keyId, string proof)
        {
            throw new NotImplementedException();
        }

        public IApplicationRemovePasswordRequestBuilder RemovePassword(Guid keyId)
        {
            throw new NotImplementedException();
        }

        public IApplicationRequest Request()
        {
            return new TestAppRequest(id);
        }

        public IApplicationRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectRestoreRequestBuilder Restore()
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestAppRequest : IApplicationRequest
    {
        private string id;

        public TestAppRequest(string id)
        {
            this.id = id;
        }

        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public Task<Application> CreateAsync(Application applicationToCreate)
        {
            throw new NotImplementedException();
        }

        public Task<Application> CreateAsync(Application applicationToCreate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IApplicationRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IApplicationRequest Expand(Expression<Func<Application, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public Task<Application> GetAsync()
        {
            Task<Application> ret = Task<Application>.Factory.StartNew(() =>
            {
                if (id.StartsWith("a1"))
                {
                    var page = new Application
                    {
                        Id = "a1",
                        DisplayName = "a1"
                    };
                    return page;
                }
                else
                {
                    throw new Exception("out of range Application");
                }
            });
            return ret;
        }

        public Task<Application> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IApplicationRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IApplicationRequest Select(Expression<Func<Application, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public Task<Application> UpdateAsync(Application applicationToUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<Application> UpdateAsync(Application applicationToUpdate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class TestUsersCollectionBuilder : IGraphServiceUsersCollectionRequestBuilder
    {
        public IUserRequestBuilder this[string id] => new TestUserRequestBuilder(id);

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IUserDeltaRequestBuilder Delta()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Request()
        {
            return new TestUsersCollectionRequest();
        }

        public IGraphServiceUsersCollectionRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }
    public class TestUsersCollectionRequest : IGraphServiceUsersCollectionRequest
    {
        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();
        public string Id { get; set; }

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public Task<User> AddAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<User> AddAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Expand(Expression<Func<User, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Filter(string value)
        {
            Id = value.Substring(value.IndexOf('\'') + 1);
            return this;
        }

        public Task<IGraphServiceUsersCollectionPage> GetAsync()
        {
            Task<IGraphServiceUsersCollectionPage> ret = Task<IGraphServiceUsersCollectionPage>.Factory.StartNew(() =>
            {
                if (Id.StartsWith("ua"))
                {
                    var page = new GraphServiceUsersCollectionPage();
                    page.Add(new User {
                        UserPrincipalName = "ua@valid.com",
                        Id = "ua",
                        DisplayName = "User A"
                    });
                    return page;
                }
                else if (Id.StartsWith("ub"))
                {
                    var page = new GraphServiceUsersCollectionPage();
                    page.Add(new User
                    {
                        UserPrincipalName = "ub@valid.com",
                        Id = "ub",
                        DisplayName = "User B"
                    });
                    return page;
                }
                else if (Id.StartsWith("uc"))
                {
                    var page = new GraphServiceUsersCollectionPage();
                    page.Add(new User
                    {
                        UserPrincipalName = "uc@valid.com",
                        Id = "uc",
                        DisplayName = "User C"
                    });
                    return page;
                }
                else
                {
                    throw new Exception("ResourceNotFound user");
                }
            });
            return ret;
        }

        public Task<IGraphServiceUsersCollectionPage> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest OrderBy(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Select(Expression<Func<User, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Skip(int value)
        {
            throw new NotImplementedException();
        }

        public IGraphServiceUsersCollectionRequest Top(int value)
        {
            throw new NotImplementedException();
        }
    }
    public class TestUserRequestBuilder : IUserRequestBuilder
    {
        public string Id { get; set; }
        public TestUserRequestBuilder(string id)
        {
            Id = id;
        }
        public IUserAppRoleAssignmentsCollectionRequestBuilder AppRoleAssignments => throw new NotImplementedException();

        public IUserOwnedDevicesCollectionWithReferencesRequestBuilder OwnedDevices => throw new NotImplementedException();

        public IUserRegisteredDevicesCollectionWithReferencesRequestBuilder RegisteredDevices => throw new NotImplementedException();

        public IDirectoryObjectWithReferenceRequestBuilder Manager => throw new NotImplementedException();

        public IUserDirectReportsCollectionWithReferencesRequestBuilder DirectReports => throw new NotImplementedException();

        public IUserMemberOfCollectionWithReferencesRequestBuilder MemberOf => throw new NotImplementedException();

        public IUserCreatedObjectsCollectionWithReferencesRequestBuilder CreatedObjects => throw new NotImplementedException();

        public IUserOauth2PermissionGrantsCollectionWithReferencesRequestBuilder Oauth2PermissionGrants => throw new NotImplementedException();

        public IUserOwnedObjectsCollectionWithReferencesRequestBuilder OwnedObjects => throw new NotImplementedException();

        public IUserLicenseDetailsCollectionRequestBuilder LicenseDetails => throw new NotImplementedException();

        public IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder TransitiveMemberOf => throw new NotImplementedException();

        public IOutlookUserRequestBuilder Outlook => throw new NotImplementedException();

        public IUserMessagesCollectionRequestBuilder Messages => throw new NotImplementedException();

        public IUserMailFoldersCollectionRequestBuilder MailFolders => throw new NotImplementedException();

        public ICalendarRequestBuilder Calendar => throw new NotImplementedException();

        public IUserCalendarsCollectionRequestBuilder Calendars => throw new NotImplementedException();

        public IUserCalendarGroupsCollectionRequestBuilder CalendarGroups => throw new NotImplementedException();

        public IUserCalendarViewCollectionRequestBuilder CalendarView => throw new NotImplementedException();

        public IUserEventsCollectionRequestBuilder Events => throw new NotImplementedException();

        public IUserPeopleCollectionRequestBuilder People => throw new NotImplementedException();

        public IUserContactsCollectionRequestBuilder Contacts => throw new NotImplementedException();

        public IUserContactFoldersCollectionRequestBuilder ContactFolders => throw new NotImplementedException();

        public IInferenceClassificationRequestBuilder InferenceClassification => throw new NotImplementedException();

        public IProfilePhotoRequestBuilder Photo => throw new NotImplementedException();

        public IUserPhotosCollectionRequestBuilder Photos => throw new NotImplementedException();

        public IDriveRequestBuilder Drive => throw new NotImplementedException();

        public IUserDrivesCollectionRequestBuilder Drives => throw new NotImplementedException();

        public IUserFollowedSitesCollectionWithReferencesRequestBuilder FollowedSites => throw new NotImplementedException();

        public IUserExtensionsCollectionRequestBuilder Extensions => throw new NotImplementedException();

        public IUserManagedDevicesCollectionRequestBuilder ManagedDevices => throw new NotImplementedException();

        public IUserManagedAppRegistrationsCollectionWithReferencesRequestBuilder ManagedAppRegistrations => throw new NotImplementedException();

        public IUserDeviceManagementTroubleshootingEventsCollectionRequestBuilder DeviceManagementTroubleshootingEvents => throw new NotImplementedException();

        public IPlannerUserRequestBuilder Planner => throw new NotImplementedException();

        public IOfficeGraphInsightsRequestBuilder Insights => throw new NotImplementedException();

        public IUserSettingsRequestBuilder Settings => throw new NotImplementedException();

        public IOnenoteRequestBuilder Onenote => throw new NotImplementedException();

        public IUserActivitiesCollectionRequestBuilder Activities => throw new NotImplementedException();

        public IUserOnlineMeetingsCollectionRequestBuilder OnlineMeetings => throw new NotImplementedException();

        public IUserJoinedTeamsCollectionRequestBuilder JoinedTeams => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public string AppendSegmentToRequestUrl(string urlSegment)
        {
            throw new NotImplementedException();
        }

        public IUserAssignLicenseRequestBuilder AssignLicense(IEnumerable<AssignedLicense> addLicenses, IEnumerable<Guid> removeLicenses)
        {
            throw new NotImplementedException();
        }

        public IUserChangePasswordRequestBuilder ChangePassword(string currentPassword = null, string newPassword = null)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberGroupsRequestBuilder CheckMemberGroups(IEnumerable<string> groupIds)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectCheckMemberObjectsRequestBuilder CheckMemberObjects(IEnumerable<string> ids)
        {
            throw new NotImplementedException();
        }

        public IUserExportPersonalDataRequestBuilder ExportPersonalData(string storageLocation = null)
        {
            throw new NotImplementedException();
        }

        public IUserFindMeetingTimesRequestBuilder FindMeetingTimes(IEnumerable<AttendeeBase> attendees = null, LocationConstraint locationConstraint = null, TimeConstraint timeConstraint = null, Duration meetingDuration = null, int? maxCandidates = null, bool? isOrganizerOptional = null, bool? returnSuggestionReasons = null, double? minimumAttendeePercentage = null)
        {
            throw new NotImplementedException();
        }

        public IUserGetMailTipsRequestBuilder GetMailTips(IEnumerable<string> EmailAddresses, MailTipsType? MailTipsOptions = null)
        {
            throw new NotImplementedException();
        }

        public IUserGetManagedAppDiagnosticStatusesRequestBuilder GetManagedAppDiagnosticStatuses()
        {
            throw new NotImplementedException();
        }

        public IUserGetManagedAppPoliciesRequestBuilder GetManagedAppPolicies()
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberGroupsRequestBuilder GetMemberGroups(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectGetMemberObjectsRequestBuilder GetMemberObjects(bool? securityEnabledOnly = null)
        {
            throw new NotImplementedException();
        }

        public IDriveItemRequestBuilder ItemWithPath(string path)
        {
            throw new NotImplementedException();
        }

        public IUserReminderViewRequestBuilder ReminderView(string StartDateTime, string EndDateTime = null)
        {
            throw new NotImplementedException();
        }

        public IUserRemoveAllDevicesFromManagementRequestBuilder RemoveAllDevicesFromManagement()
        {
            throw new NotImplementedException();
        }

        public IUserReprocessLicenseAssignmentRequestBuilder ReprocessLicenseAssignment()
        {
            throw new NotImplementedException();
        }

        public IUserRequest Request()
        {
            return new TestUserRequest(Id);
        }

        public IUserRequest Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        public IDirectoryObjectRestoreRequestBuilder Restore()
        {
            throw new NotImplementedException();
        }

        public IUserRevokeSignInSessionsRequestBuilder RevokeSignInSessions()
        {
            throw new NotImplementedException();
        }

        public IUserSendMailRequestBuilder SendMail(Message Message, bool? SaveToSentItems = null)
        {
            throw new NotImplementedException();
        }

        public IUserTranslateExchangeIdsRequestBuilder TranslateExchangeIds(IEnumerable<string> InputIds, ExchangeIdFormat TargetIdType, ExchangeIdFormat SourceIdType)
        {
            throw new NotImplementedException();
        }

        public IUserWipeManagedAppRegistrationsByDeviceTagRequestBuilder WipeManagedAppRegistrationsByDeviceTag(string deviceTag = null)
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IDirectoryObjectRequest IDirectoryObjectRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request()
        {
            throw new NotImplementedException();
        }

        IEntityRequest IEntityRequestBuilder.Request(IEnumerable<Option> options)
        {
            throw new NotImplementedException();
        }
    }
    public class TestUserRequest : IUserRequest
    {
        public string Id { get; set; }
        public TestUserRequest(string id)
        {
            Id = id;
        }
        public string ContentType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IList<HeaderOption> Headers => throw new NotImplementedException();

        public IBaseClient Client => throw new NotImplementedException();

        public string Method => throw new NotImplementedException();

        public string RequestUrl => throw new NotImplementedException();

        public IList<QueryOption> QueryOptions => throw new NotImplementedException();

        public IDictionary<string, IMiddlewareOption> MiddlewareOptions => throw new NotImplementedException();

        public Task<User> CreateAsync(User userToCreate)
        {
            throw new NotImplementedException();
        }

        public Task<User> CreateAsync(User userToCreate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IUserRequest Expand(string value)
        {
            throw new NotImplementedException();
        }

        public IUserRequest Expand(Expression<Func<User, object>> expandExpression)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetAsync()
        {
            Task<User> ret = Task<User>.Factory.StartNew(() =>
            {
                if (Id.StartsWith("ua"))
                {
                    var page = new User
                    {
                        UserPrincipalName = "ua@valid.com",
                        Id = "ua",
                        DisplayName = "User A"
                    };
                    return page;
                }
                else if (Id.StartsWith("ub"))
                {
                    var page = new User
                    {
                        UserPrincipalName = "ub@valid.com",
                        Id = "ub",
                        DisplayName = "User B"
                    };
                    return page;
                }
                else if (Id.StartsWith("uc"))
                {
                    var page = new User
                    {
                        UserPrincipalName = "uc@valid.com",
                        Id = "uc",
                        DisplayName = "User C"
                    };
                    return page;
                }
                else
                {
                    throw new Exception("ResourceNotFound user");
                }
            });
            return ret;
        }

        public Task<User> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public HttpRequestMessage GetHttpRequestMessage()
        {
            throw new NotImplementedException();
        }

        public IUserRequest Select(string value)
        {
            throw new NotImplementedException();
        }

        public IUserRequest Select(Expression<Func<User, object>> selectExpression)
        {
            throw new NotImplementedException();
        }

        public Task<User> UpdateAsync(User userToUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<User> UpdateAsync(User userToUpdate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
