using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AutoKeyVaultToYaml
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
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
