using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace ProTeamsDev.GraphGeneric
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

            var tenantId = "YOUR TENANT ID";
            var clientId = "YOUR CLIENT ID";
            var clientSecret = "YOUR CLIENT SECRET";



            // Get Application permissions
            var authority = $"https://login.microsoftonline.com/{tenantId}";
            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            #region original authentication
            // Get bearer token
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var authenticationResult = app.AcquireTokenForClient(scopes).ExecuteAsync().Result;

            // Create GraphClient
            var originalGraphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(requestMessage =>
                {
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("bearer", authenticationResult.AccessToken);
                    return Task.FromResult(0);
                }));
            #endregion

            #region preview authentication
            // Create an authentication provider by passing in a client application and graph scopes.
            ClientCredentialProvider authProvider = new ClientCredentialProvider(app);
            // Create a new instance of GraphServiceClient with the authentication provider.
            GraphServiceClient newGraphClient = new GraphServiceClient(authProvider);


            #endregion

            // Call the Graph with the SDK
            var groups = originalGraphClient.Groups.Request().GetAsync().Result;
            var users = GetUsersAsync(newGraphClient).Result;

            //Call the Graph with HTTP Client GET
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
            HttpResponseMessage response = client.GetAsync(new Uri("https://graph.microsoft.com/v1.0/users/rick@proteamsdev.onmicrosoft.com")).ConfigureAwait(false).GetAwaiter().GetResult();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var res = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                Console.Write($"Error in statuscode {response.StatusCode}");
            }

            //Call the Graph with HTTP Client POST
            var payload = "{\r\n    \"displayName\": \"Architecture Discussion\",\r\n    \"description\": \"This channel is where we debate all future architecture plans\"\r\n}";
            HttpContent c = new StringContent(payload, Encoding.UTF8, "application/json"); 
            response = client.PostAsync(("https://graph.microsoft.com/v1.0/teams/9a792aa1-489d-4485-b4c1-d6e9662bc854/channels"), c).ConfigureAwait(false).GetAwaiter().GetResult();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var res = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
            {
                Console.Write($"Error in statuscode {response.StatusCode}");
            }


            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static async Task<IEnumerable<User>> GetUsersAsync(GraphServiceClient graphClient)
        {
            List<User> allUsers = new List<User>();
            var users = await graphClient.Users.Request().Top(500)
                .Select("displayName,mail,givenName,surname,id")
                .Filter("startswith(city,'b')")
                .GetAsync();

            while (users.Count > 0)
            {
                allUsers.AddRange(users);
                if (users.NextPageRequest != null)
                {
                    users = await users.NextPageRequest
                        .GetAsync();
                }
                else
                {
                    break;
                }
            }
            return allUsers;
        }

    }
}
