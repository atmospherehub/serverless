using System;
using System.Net;
using System.Data.SqlClient;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
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
    
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var pairs = req.GetQueryNameValuePairs();   
    
    var fromRaw = pairs.FirstOrDefault(q => q.Key.ToLower() == "from").Value;
    var toRaw = pairs.FirstOrDefault(q => q.Key.ToLower() == "to").Value;

    var from = new DateTimeOffset(DateTime.UtcNow.AddDays(-6).Date);
    if(!String.IsNullOrEmpty(fromRaw) && !DateTimeOffset.TryParse(fromRaw, out from))
        return req.CreateResponse(HttpStatusCode.BadRequest, "'from' is not valid date");

    var to = new DateTimeOffset(DateTime.UtcNow.AddDays(1).Date);
    if(!String.IsNullOrEmpty(toRaw) && !DateTimeOffset.TryParse(toRaw, out to))
        return req.CreateResponse(HttpStatusCode.BadRequest, "'to' is not valid date");

    log.Info($"Triggered ApiEmotionsHighlights from: {from} to: {to}");

    var scoresTasks = scores.Select(s => getImageByMaxScore(s, from, to));
    var totalTask = getTotalFaces(from, to);
    await Task.WhenAll(totalTask, Task.WhenAll(scoresTasks));    
            
    return req.CreateResponse(
        HttpStatusCode.OK, new 
        {
            Total = totalTask.Result,
            Emotions = scoresTasks.Select(t => t.Result)
        }, 
        "application/json");
}

private static async Task<ScoreResult> getImageByMaxScore(string scoreName, DateTimeOffset start, DateTimeOffset end)
{
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        var columnName = $"Cognitive{scoreName}";
        return await connection.QuerySingleOrDefaultAsync<ScoreResult>(
            $@"SELECT TOP 1
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

private static async Task<int> getTotalFaces(DateTimeOffset start, DateTimeOffset end)
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
    public string Name { get; set; }
    public float Score { get; set; }
    public DateTimeOffset Time { get; set; }

    private string _image;
    public string Image 
    { 
        set { _image = $"{CM.AppSettings["images_endpoint"]}/faces/{value}"; }
        get { return _image; } 
    }    
}