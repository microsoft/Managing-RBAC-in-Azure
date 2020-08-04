// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RBAC
{
    /// <summary>
    /// This class is an Authentication Provider obtained from the Microsoft Authentication Library (MSAL).
    /// </summary>
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        public MsalAuthenticationProvider() {}
        public MsalAuthenticationProvider(IConfidentialClientApplication clientApp, string[] scopes)
        {
            this.clientApp = clientApp;
            this.scopes = scopes;
        }
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var token = await GetTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        }
        public async Task<string> GetTokenAsync()
        {
            AuthenticationResult authResult = null;
            authResult = await clientApp.AcquireTokenForClient(scopes).ExecuteAsync();
            return authResult.AccessToken;
        }
        private IConfidentialClientApplication clientApp;
        private string[] scopes;
    }
}
