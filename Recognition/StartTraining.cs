using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Common;
using Dapper;
using System.Net.Http;

namespace Recognition
{
    public static class StartTraining
    {
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(StartTraining))]
        public static async Task Run([TimerTrigger("0 0 3 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"Timer trigger '{nameof(StartTraining)}' at: {DateTime.Now}");

            var newTagsExists = (await getNewTagsCount(DateTimeOffset.UtcNow.AddHours(-24))) > 0;
            if (!newTagsExists)
            {
                log.Info($"No new faces were tagged => exiting");
                return;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{Settings.FACE_API_URL}/train"))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.FACE_API_TOKEN);
                var response = await _client.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();
                log.Info($"Received result from faces API {response.StatusCode}: {contents}");
            }
        }

        private static async Task<int> getNewTagsCount(DateTimeOffset startFrom)
        {
            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [dbo].[FaceTags] WHERE Time > @Time",
                    new { Time = startFrom });
            }
        }
    }
}