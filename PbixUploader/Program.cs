using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using System.Collections.Generic;


namespace PbixUploader
{
    class Program
    {
        // Power BI REST API URLs
        private static readonly string AuthorityUrl = ConfigurationManager.AppSettings["RestUrl.Authority"];
        private static readonly string ResourceUrl = ConfigurationManager.AppSettings["RestUrl.Resource"];
        private static readonly string ApiUrl = ConfigurationManager.AppSettings["RestUrl.Api"];
        private static readonly string WorkspacesUrl = ConfigurationManager.AppSettings["RestUrl.Workspaces"];
      
        // Global Variables
        private static PowerBIClient Client = null;
        private static string AccessToken = string.Empty;
        private static string GroupId = string.Empty;
        private static string ClientId = string.Empty;
        private static string Workspace = string.Empty;
        private static string Username = string.Empty;
        private static string Password = string.Empty;
        private static string PbixFilePath = string.Empty;
        private static bool newWS = false;

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args)
              .WithParsed(RunProgram)
              .WithNotParsed(LogErrors);
        }

        private static void RunProgram(Options options)
        {
            // Main method variables
            ClientId = options.ClientId;
            Workspace = options.Workspace;
            Username = options.Username;
            Password = options.Password;
            PbixFilePath = options.ReportPath;

            // Get the access token
            GetAccessToken().Wait();

            // Get the PBI client
            GetPbiClient();

            // Get the Group Id
            GetGroupId().Wait();

            // Upload the PBIX report 
            UploadPBIX(PbixFilePath);
        }

        private static async Task GetAccessToken()
        {
            // Create a user password cradentials.
            var credential = new UserPasswordCredential(Username, Password);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(AuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential);

            // Get the access token
            AccessToken = authenticationResult.AccessToken;
        }

        private static void GetPbiClient()
        {
            // Parameters
            var apiUrl = new Uri(ApiUrl);
            var tokenCredentials = new TokenCredentials(AccessToken, "Bearer");

            // Get the PBI client and its groups
            Client = new PowerBIClient(apiUrl, tokenCredentials);
        }

        private static async Task GetGroupId()
        {
            // Get the groups list
            var groups = await Client.Groups.GetGroupsAsync();
            var groupList = groups.Value.ToList();

            // If the given workspace exits then return its id, if not create a new workspace and return its id.
            if (groupList.Exists(g => g.Name == Workspace))
            {
                GroupId = groupList.Find(g => g.Name == Workspace).Id;
            }
            else
            {
                GroupId = Client.Groups.CreateGroupAsync(new GroupCreationRequest(Workspace)).Result.Id;
                newWS = true;
            }
        }

        public static void UploadPBIX(string pbixFilePath)
        {
            // Get report name from file path
            string reportName = Path.GetFileNameWithoutExtension(pbixFilePath);

            // If Workspace is not new, delete the existing report with the same name if on exists
            if (!newWS)
                DeleteReport(reportName).Wait();

            // Create REST URL with report name in query string
            string restUrlPbix = WorkspacesUrl + GroupId + "/imports?datasetDisplayName=" + reportName;

            // Prepare pbix content and load it into body using multi-part form data
            MultipartFormDataContent requestBody = new MultipartFormDataContent(Guid.NewGuid().ToString());
            requestBody.Add(PrepareBodyContent(pbixFilePath));

            // Create and configure HttpClient
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

            // Post the request
            var response = httpClient.PostAsync(restUrlPbix, requestBody).Result;

            // Check for success
            if (response.StatusCode.ToString().Equals("Accepted"))
            {
                Console.WriteLine("Upload process completed with success: " + response.Content.ReadAsStringAsync().Result);
            }
            else
            {
                Console.WriteLine("Upload process completed with errors: " + response.Content.ReadAsStringAsync().Result);
            }
        }

        private static StreamContent PrepareBodyContent(string pbixFilePath)
        {
            // Create the stream content and load the PBIX file into it
            StreamContent pbixBodyContent = null;

            try
            {
                // Open the report
                pbixBodyContent = new StreamContent(File.Open(pbixFilePath, FileMode.Open));
            }
            catch (Exception ex)
            {
                var caption = $"Exception was caught: {ex.Message}";
                var inner = ex.InnerException;
                var msg = "Error";
                if (inner != null)
                {
                    msg += $"{Environment.NewLine}{inner.Message} of type {inner.GetType()} at {Environment.NewLine}{inner.StackTrace}";
                }
                System.Console.WriteLine($"[{DateTime.Now}]:{msg}{Environment.NewLine}");
            }

            // Add headers for request bod content
            pbixBodyContent.Headers.Add("Content-Type", "application/octet-stream");
            pbixBodyContent.Headers.Add("Content-Disposition",
                                         @"form-data; name=""file""; filename=""" + pbixFilePath + @"""");

            return pbixBodyContent;
        }

        public static async Task DeleteReport(string reportName)
        {
            // Get the list of datasets
            var datasets = await Client.Datasets.GetDatasetsInGroupAsync(GroupId);
            var datasetList = datasets.Value.ToList();

            // Loop the datasets list and delete the target dataset if exists
            foreach (var d in datasetList)
            {
                if (reportName.Equals(d.Name))
                {
                    // if dataset name matches, delete it
                    Client.Datasets.DeleteDatasetByIdInGroup(GroupId, d.Id);
                }
            }
        }

        private static void LogErrors(IEnumerable<Error> errors)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("Program wasn't able to parse your input");
            foreach (var error in errors)
            {
                System.Console.WriteLine(error);
            }

            System.Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}