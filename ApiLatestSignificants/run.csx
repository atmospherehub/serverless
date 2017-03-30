using System;
using System.Net;
using System.Data.SqlClient;
using Newtonsoft.Json;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
    
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var pairs = req.GetQueryNameValuePairs();   
    
    var amountRaw = pairs.FirstOrDefault(q => q.Key.ToLower() == "amount").Value;
    var amount = 3;
    if(!String.IsNullOrEmpty(amountRaw) && !Int32.TryParse(amountRaw, out amount))
        return req.CreateResponse(HttpStatusCode.BadRequest, "'amount' is not valid integer");

    log.Info($"Triggered ApiLatestSignificant");

    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        var result = await connection.QueryAsync<ScoreResult>(
            $@"
            SELECT TOP {amount}
            	*
            FROM [dbo].[Faces]
            WHERE 
            	[CognitiveHappiness] > [CognitiveNeutral] OR
            	[CognitiveAnger] > [CognitiveNeutral] OR
            	[CognitiveContempt] > [CognitiveNeutral] OR
            	[CognitiveDisgust] > [CognitiveNeutral] OR
            	[CognitiveFear] > [CognitiveNeutral] OR
            	[CognitiveSadness] > [CognitiveNeutral] OR
            	[CognitiveSurprise] > [CognitiveNeutral]
            ORDER BY [Time] DESC");
            
        return req.CreateResponse(
            HttpStatusCode.OK, 
            result, 
            "application/json");
    }            
}

public class ScoreResult
{  
    public string Name 
    { 
        get
        {
            if(CognitiveHappiness > CognitiveAnger)
                return "Happiness";
            if(CognitiveAnger > CognitiveContempt)
                return "Anger";
            if(CognitiveContempt > CognitiveDisgust)
                return "Contempt";
            if(CognitiveDisgust > CognitiveFear)
                return "Disgust";
            if(CognitiveFear > CognitiveSurprise)
                return "Surprise";
            return "";
        }
    }

    public float Score 
    { 
        get
        {
            return (new float[] 
            { 
                CognitiveHappiness, 
                CognitiveAnger, 
                CognitiveContempt, 
                CognitiveDisgust, 
                CognitiveDisgust, 
                CognitiveFear, 
                CognitiveSurprise
            }).Max();
        }
    }

    private string _image;
    public string Image 
    { 
        set { _image = $"{CM.AppSettings["images_endpoint"]}/faces/{value}"; }
        get { return _image; } 
    } 
    
    public DateTimeOffset Time { get; set; }
    
    [JsonIgnore]
    public virtual float CognitiveHappiness { get; set; }
    [JsonIgnore]    
    public virtual float CognitiveAnger { get; set; }
    [JsonIgnore]
    public virtual float CognitiveContempt { get; set; }
    [JsonIgnore]
    public virtual float CognitiveDisgust { get; set; }
    [JsonIgnore]
    public virtual float CognitiveFear { get; set; }
    [JsonIgnore]
    public virtual float CognitiveSurprise { get; set; }
}