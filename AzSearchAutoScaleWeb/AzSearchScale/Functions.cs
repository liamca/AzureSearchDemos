using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;


namespace AzSearchScale
{
    public class Functions
    {
        // You can obtain this information from the Azure management portal. The instructions in the link above 
        // include details for this as well.
        private const string TenantId = [Enter a Valid Windows Azure Active Directory Tenant ID];
        private const string ClientId = [Enter a Valid Windows Azure Active Directory Client ID];
        private const string SubscriptionId = [Enter a Valid Windows Azure Subscription ID];
        private const string SearchServiceName = [Enter a Valid Azure Search service name - don't include .search.windows.net];
        private const string ResourceGroup = [Enter a Valid Windows Azure Resource Group used for this Azure Search service];
        private static string WinPwd;
        private static string WinUser; 

        private static string _authorizationToken = null;

        // This is the return URL you configure during AD client application setup. For this type of apps (non-web apps)
        // you can set this to virtually anything you like something like http://localhost/testapp. The important thing is that the URL here and the
        // URL in AD configuration match.
        private static readonly Uri RedirectUrl = new Uri("http://azure.microsoft.com/en-us/services/search/");

        // This function will be triggered based on the schedule you have set for this WebJob
        // This function will enqueue a message on an Azure Queue called queue
        [NoAutomaticTrigger]
        public static async void ManualTrigger(TextWriter log, int value)
        {
            WinUser = ConfigurationManager.AppSettings["WinUser"].ToString();
            WinPwd = ConfigurationManager.AppSettings["WinPwd"].ToString();
            HttpResponseMessage response;

            // Set Replica count based on the time of day using Pacifit Standard Time
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime newDateTime = TimeZoneInfo.ConvertTime(DateTime.Now, timeZoneInfo);
            int ChangeReplicaCount = 1;
            if ((newDateTime.Hour > 15) && (newDateTime.Hour <= 22))
                ChangeReplicaCount = 3;
            else if ((newDateTime.Hour > 22) || (newDateTime.Hour <= 6))
                ChangeReplicaCount = 2;
            else if ((newDateTime.Hour > 6) || (newDateTime.Hour <= 15))
                ChangeReplicaCount = 1;

            // Skip if service is still in provisioning state 
            if (await IsProvisioning(SearchServiceName) == false)
            {
                // Get service details to find current replica count
                response = await ExecuteArmRequest(HttpMethod.Get, "resourcegroups/" + ResourceGroup + "/providers/Microsoft.Search/searchServices/" + SearchServiceName + "?api-version=2015-02-28");

                if (response.Content != null)
                {

                    int ReplicaCount = Convert.ToInt32(((dynamic)((JObject)((JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result))))).properties.replicaCount);
                    Console.WriteLine("Replicas: {0}", ReplicaCount.ToString());
                    if (ChangeReplicaCount != ReplicaCount)
                    {
                        // Change service replica/partition count
                        // NOTE: this will fail unless you change the service creation code above to make it a "standard" service 
                        response = await ExecuteArmRequest(HttpMethod.Put,
                            "resourcegroups/" + ResourceGroup + "/providers/Microsoft.Search/searchServices/" + SearchServiceName + "?api-version=2015-02-28",
                            new
                            {
                                type = "Microsoft.Search/searchServices",
                                location = "West US",
                                properties = new
                                {
                                    sku = new { name = "standard" },
                                    partitionCount = 1,
                                    replicaCount = ReplicaCount
                                }
                            });
                        DumpResponse("Dynamically scale Azure Search service", response);
                    }
                }
            }

        }

        private static async Task<bool> IsProvisioning(string serviceName)
        {
            string state = null;

            HttpResponseMessage response = await ExecuteArmRequest(HttpMethod.Get, "resourcegroups/" + ResourceGroup + "/providers/Microsoft.Search/searchServices/" + serviceName + "?api-version=2015-02-28");
            state = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result).properties.provisioningState;
            Console.WriteLine("Service state: {0}", state);

            if (state == "provisioning")
                return true;
            else 
                return false;
        }

        private static void DumpResponse(string title, HttpResponseMessage response)
        {
            Console.WriteLine(title);
            Console.WriteLine("Request: {0} {1}", response.RequestMessage.Method, response.RequestMessage.RequestUri);
            Console.WriteLine("Status: {0}", response.StatusCode);
            Console.WriteLine();

            if (response.Content != null)
            {
                // Round-trip this through a JSON serializer to get good formatting
                string json = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result), Formatting.Indented);
                Console.WriteLine(json);
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
        }

        private async static Task<HttpResponseMessage> ExecuteArmRequest(HttpMethod httpMethod, string relativeUrl, object requestBody = null)
        {
            Uri baseUrl = new Uri("https://management.azure.com/subscriptions/" + SubscriptionId + "/");
            HttpResponseMessage response = new HttpResponseMessage();

            // We assume this application runs for a short period of time and the obtained token won't expire. Refer
            // to documentation for how to detect and refresh expired tokens
            if (_authorizationToken == null)
            {
                //_authorizationToken = await GetAuthorizationHeader();
                var context = new AuthenticationContext("https://login.windows.net/" + TenantId);

                AuthenticationResult result = await context.AcquireTokenAsync("https://management.core.windows.net/", ClientId, new UserCredential(WinUser, WinPwd));

                //AuthenticationResult result = await context.AcquireTokenAsync("https://management.core.windows.net/", new ClientCredential("658287c2-0a60-4bb3-9815-650777047dc9", "thpX8a8V1cw74zPu8N1xyB/dK4aOS1MyO03B3ISz5Do="));

                _authorizationToken = result.CreateAuthorizationHeader().Substring("Bearer ".Length);

            }

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, new Uri(baseUrl, relativeUrl));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authorizationToken);
            if (requestBody != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            }
            response = await client.SendAsync(request);

            return response;
        }

        // This method taken from: http://msdn.microsoft.com/en-us/library/azure/dn790557.aspx
        private async static Task<string> GetAuthorizationHeader()
        {
            AuthenticationResult result = null;

            try
            {
                var context = new AuthenticationContext("https://login.windows.net/" + TenantId);

                result = await context.AcquireTokenAsync("https://management.core.windows.net/", ClientId, new UserCredential(WinUser, WinPwd));
                //result = await context.AcquireTokenAsync("https://management.core.windows.net/", new ClientCredential("658287c2-0a60-4bb3-9815-650777047dc9", "thpX8a8V1cw74zPu8N1xyB/dK4aOS1MyO03B3ISz5Do="));

                var token = result.CreateAuthorizationHeader().Substring("Bearer ".Length);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the JWT token");
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }

            return result.AccessToken;
        }

    }
}
