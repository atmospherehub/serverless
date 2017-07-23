using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Reports.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Reports
{
    public static class GenerateReport
    {
        private static readonly string[] _scores = new string[]
        {
            "Happiness",
            "Anger",
            "Contempt",
            "Disgust",
            "Fear",
            "Sadness",
            "Surprise"
        };

        [FunctionName(nameof(GenerateReport))]
        public static async Task Run(
            [TimerTrigger("0 0 3 * * MON,TUE,WED,THU,FRI")]TimerInfo myTimer,
            [ServiceBus("atmosphere-reports", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Topic)] ICollector<string> outputTopic,
            TraceWriter log)
        {
            log.Info($"Timer trigger '{nameof(GenerateReport)}'  at: {DateTime.Now}");

            var utcNow = DateTime.UtcNow;
            var start = TimeZoneInfo.ConvertTimeFromUtc(utcNow, Settings.LOCAL_TIMEZONE).AddDays(-1).Date;
            var end = TimeZoneInfo.ConvertTimeFromUtc(utcNow, Settings.LOCAL_TIMEZONE).Date;
            var total = await getTotalFaces(start, end);
            if (total > 0)
            {
                var scoresTasks = _scores.Select(s => getImageByMaxScore(s, start, end));
                await Task.WhenAll(scoresTasks);

                outputTopic.Add((new Report
                {
                    StartDate = start,
                    EndDate = end,
                    ReportName = "Daily",
                    Results = scoresTasks.Select(t => t.Result).Where(r => r != null).ToArray(),
                    Total = total
                }).ToJson());
                log.Info($"Daily Report was generated for {total} faces");
            }
            else
            {
                log.Info($"No daily report will be generated because there were 0 faces");
            }
        }

        private static async Task<ScoreResult> getImageByMaxScore(string scoreName, DateTime start, DateTime end)
        {
            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                var columnName = $"Cognitive{scoreName}";
                return await connection.QuerySingleOrDefaultAsync<ScoreResult>(
                    $@"SELECT TOP 1
                        [Id],
                        '{scoreName}' AS [Name],
                        [{columnName}] AS [Score],
                        [Time],
                        [Image]
                       FROM [dbo].[Faces]
                       WHERE [Time] >= @start AND [Time] <= @end
                       ORDER BY [{columnName}] DESC",
                    new
                    {
                        start,
                        end
                    });
            }
        }

        private static async Task<int> getTotalFaces(DateTime start, DateTime end)
        {
            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*)
                      FROM [dbo].[Faces]
                      WHERE [Time] >= @start AND [Time] <= @end",
                    new
                    {
                        start,
                        end
                    });
            }
        }
    }
}