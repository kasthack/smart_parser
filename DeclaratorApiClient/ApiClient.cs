using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using TI.Declarator.JsonSerialization;
using TI.Declarator.ParserCommon;

namespace TI.Declarator.DeclaratorApiClient
{
    public static class ApiClient
    {
        private const string ReportUnknownEntryUrl = "https://declarator.org/api/unknown_entry/";
        private const string ValidateOutputUrl = "https://declarator.org/api/jsonfile/validate/";

        private static HttpClient HttpClient { get; }

        private static string Username { get; }
        private static string Password { get; }

        static ApiClient()
        {
            var authLines = File.ReadAllLines("auth.txt");
            Username = authLines[0];
            Password = authLines[1];

            HttpClient = new HttpClient();

            var authBytes = Encoding.ASCII.GetBytes($"{Username}:{Password}");
            var basicAuthInfo = Convert.ToBase64String(authBytes);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthInfo);
        }
        public static void ReportUnknownEntry(UnknownEntry ue)
        {
            var jsonContents = MiscSerializer.Serialize(ue);
            var contents = new StringContent(jsonContents, Encoding.UTF8, "application/json");
            var httpResponse = HttpClient.PostAsync(ReportUnknownEntryUrl, contents).Result;

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new DeclaratorApiException(httpResponse, "Could not report unknown entry to Declarator API");
            }
        }

        public static string ValidateParserOutput(string jsonOutput)
        {
            var contents = new StringContent(jsonOutput, Encoding.UTF8, "application/json");
            var httpResponse = HttpClient.PostAsync(ValidateOutputUrl, contents).Result;

            return !httpResponse.IsSuccessStatusCode
                ? throw new DeclaratorApiException(httpResponse, "Could not validate parser output")
                : httpResponse.Content.ReadAsStringAsync().Result;
        }
    }
}
