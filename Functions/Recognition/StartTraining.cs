using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Common;
using Dapper;
using System.Net.Http;

namespace Functions.Recognition
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
            var result = await FaceAPIClient.Call<dynamic>(
                $"/persongroups/{Settings.FACE_API_GROUP_NAME}/train",
                null,
                log);

            log.Info($"Received result from faces API {result.ToJson()}");
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