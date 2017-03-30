using System;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using CM = System.Configuration.ConfigurationManager;   

private static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
private static readonly TimeZoneInfo ilTimezone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
private static readonly string[] scores = new string[] 
    { 
        "Happiness", 
        "Anger", 
        "Contempt", 
        "Disgust", 
        "Fear", 
        "Sadness", 
        "Surprise" 
    };

public static async Task run(TimerInfo dayAfterWorkDay, TraceWriter log, ICollector<string> outputTopic)
{    
    log.Info($"Triggered GenerateReport");
    if(dayAfterWorkDay != null)
    {
        log.Info($"GenerateReport triggered 'dayAfterWorkDay' timer");
        var utcNow = DateTime.UtcNow;
        var start = TimeZoneInfo.ConvertTimeFromUtc(utcNow, ilTimezone).AddDays(-1).Date;
        var end = TimeZoneInfo.ConvertTimeFromUtc(utcNow, ilTimezone).Date;
        var total = await getTotalFaces(start, end);
        if(total > 0)
        {
            var scoresTasks = scores.Select(s => getImageByMaxScore(s, start, end));
            await Task.WhenAll(scoresTasks);
        
            outputTopic.Add(JsonConvert.SerializeObject(new Report
            {
                StartDate = start,
                EndDate = end,
                ReportName = "Daily",
                Results = scoresTasks.Select(t => t.Result).Where(r => r != null).ToArray(),
                Total = total
            }));
            log.Info($"Daily Report was generated for {total} faces");    
        }
        else
        {
            log.Info($"No daily report will be generated because there were 0 faces");    
        }
    }    
}

private static async Task<ScoreResult> getImageByMaxScore(string scoreName, DateTime start, DateTime end)
{
    using (var connection = new SqlConnection(connectionString))
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
            new {
                start,
                end
            });
    }
}

private static async Task<int> getTotalFaces(DateTime start, DateTime end)
{
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            $@"SELECT COUNT(*)
            FROM [dbo].[Faces]
            WHERE [Time] >= @start AND [Time] <= @end",
            new {
                start,
                end
            });
    }
}

public class ScoreResult
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float Score { get; set; }
    public DateTimeOffset Time { get; set; }
    public string Image { get; set; }    
}

public class Report
{
    public DateTime StartDate { get; set; }     
    public DateTime EndDate { get; set; }      
    public string ReportName { get; set; }      
    public int Total { get; set; }   
    public ScoreResult[] Results { get; set; }
}