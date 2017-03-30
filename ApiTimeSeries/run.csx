using System;
using System.Net;
using System.Data.SqlClient;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var pairs = req.GetQueryNameValuePairs();
    
    var fromRaw = pairs.FirstOrDefault(q => q.Key.ToLower() == "from").Value;
    var toRaw = pairs.FirstOrDefault(q => q.Key.ToLower() == "to").Value;
    var groupRaw = pairs.FirstOrDefault(q => q.Key.ToLower() == "group").Value;

    var from = new DateTimeOffset(DateTime.UtcNow.AddDays(-6).Date);
    if(!String.IsNullOrEmpty(fromRaw) && !DateTimeOffset.TryParse(fromRaw, out from))
        return req.CreateResponse(HttpStatusCode.BadRequest, "'from' is not valid date");

    var to = new DateTimeOffset(DateTime.UtcNow.AddDays(1).Date);
    if(!String.IsNullOrEmpty(toRaw) && !DateTimeOffset.TryParse(toRaw, out to))
        return req.CreateResponse(HttpStatusCode.BadRequest, "'to' is not valid date");

    DateParts group = DateParts.Weekday;
    if(!String.IsNullOrEmpty(groupRaw) && !Enum.TryParse<DateParts>(groupRaw, out group))
        return req.CreateResponse(HttpStatusCode.BadRequest, $"'group' must be one of [{String.Join(", ", Enum.GetNames(typeof(DateParts)))}]");

    log.Info($"Triggered ApiTimeSeries from: {from} to: {to} group: {group}");

    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        
        // insert a log to the database
        var result = await connection.QueryAsync(@"
                SELECT 
                    CASE @group 
                        WHEN 'Month' then DATEPART(MONTH, [Time] AT TIME ZONE 'Israel Standard Time')
                        WHEN 'Day' then DATEPART(DAY, [Time] AT TIME ZONE 'Israel Standard Time') 
                        WHEN 'Weekday' then DATEPART(WEEKDAY, [Time] AT TIME ZONE 'Israel Standard Time') 
                        WHEN 'Hour' then DATEPART(HOUR, [Time] AT TIME ZONE 'Israel Standard Time') 
                    END AS [GroupValue]

                    ,CASE @group 
                        WHEN 'Month' then FORMAT(MIN([Time]) AT TIME ZONE 'Israel Standard Time', 'yyyy-MM')
                        WHEN 'Day' then FORMAT(MIN([Time]) AT TIME ZONE 'Israel Standard Time', 'yyyy-MM-dd')
                        WHEN 'Weekday' then FORMAT(MIN([Time]) AT TIME ZONE 'Israel Standard Time', 'yyyy-MM-dd')
                        WHEN 'Hour' then FORMAT(MIN([Time]) AT TIME ZONE 'Israel Standard Time', 'yyyy-MM-dd HH:00')
                    END AS [FormattedDate]

                    ,COUNT(*) AS [GroupCount]       

                    ,AVG([CognitiveAnger]) AS AvgAnger
                    ,MAX([CognitiveAnger]) AS MaxAnger
                    ,MIN([CognitiveAnger]) AS MinAnger
                    
                    ,AVG([CognitiveContempt]) AS AvgContempt
                    ,MAX([CognitiveContempt]) AS MaxContempt
                    ,MIN([CognitiveContempt]) AS MinContempt
                    
                    ,AVG([CognitiveDisgust]) AS AvgDisgust
                    ,MAX([CognitiveDisgust]) AS MaxDisgust
                    ,MIN([CognitiveDisgust]) AS MinDisgust
                    
                    ,AVG([CognitiveFear]) AS AvgFear
                    ,MAX([CognitiveFear]) AS MaxFear
                    ,MIN([CognitiveFear]) AS MinFear
                    
                    ,AVG([CognitiveHappiness]) AS AvgHappiness
                    ,MAX([CognitiveHappiness]) AS MaxHappiness
                    ,MIN([CognitiveHappiness]) AS MinHappiness
                    
                    ,AVG([CognitiveNeutral]) AS AvgNeutral
                    ,MAX([CognitiveNeutral]) AS MaxNeutral
                    ,MIN([CognitiveNeutral]) AS MinNeutral
                    
                    ,AVG([CognitiveSadness]) AS AvgSadness
                    ,MAX([CognitiveSadness]) AS MaxSadness
                    ,MIN([CognitiveSadness]) AS MinSadness
                    
                    ,AVG([CognitiveSurprise]) AS AvgSurprise
                    ,MAX([CognitiveSurprise]) AS MaxSurprise
                    ,MIN([CognitiveSurprise]) AS MinSurprise

                FROM [dbo].[Faces]
                WHERE [Time] >= @start AND [Time] <= @end
                GROUP BY CASE @group 
                    WHEN 'Month' then DATEPART(MONTH, [Time] AT TIME ZONE 'Israel Standard Time') 
                    WHEN 'Day' then DATEPART(DAY, [Time] AT TIME ZONE 'Israel Standard Time') 
                    WHEN 'Weekday' then DATEPART(WEEKDAY, [Time] AT TIME ZONE 'Israel Standard Time') 
                    WHEN 'Hour' then DATEPART(HOUR, [Time] AT TIME ZONE 'Israel Standard Time') 
                END
                ORDER BY [GroupValue]",
            new {
                start = from.UtcDateTime,
                end = to.UtcDateTime,
                group = group.ToString()
            });
        return req.CreateResponse(HttpStatusCode.OK, result, "application/json");
    }    
}

public enum DateParts
{
    Month,
    Day,
    Weekday,
    Hour
}
