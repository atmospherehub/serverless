using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Common.Contract;
using System.Data;
using System.Data.SqlClient;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
private static readonly Lazy<byte> register = new Lazy<byte>(() =>
{
    SqlMapper.AddTypeHandler(new RectangleHandler());
    return 0;
}, false);

public static async Task Run(string message, TraceWriter log, ICollector<string> outputTopic)
{
    log.Info($"Triggered StoreSql by: {message}");
    var touch = register.Value;

    var processedImage = JsonConvert.DeserializeObject<Image>(message);
    var now = DateTime.UtcNow;
    var dbRecords = processedImage.Rectangles
        .Select(i => new {
            Id = Guid.NewGuid(),
            Time = now,
            processedImage.ImageName,
            i.FaceRectangle,
            i.Scores.Anger,
            i.Scores.Contempt,
            i.Scores.Disgust,
            i.Scores.Fear,
            i.Scores.Happiness,
            i.Scores.Neutral,
            i.Scores.Sadness,
            i.Scores.Surprise
        })
        .ToList();

    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[Faces] (
                [Id],
                [Time], 
                [Image], 
                [Rectangle], 
                [CognitiveAnger], 
                [CognitiveContempt],
                [CognitiveDisgust],
                [CognitiveFear], 
                [CognitiveHappiness], 
                [CognitiveNeutral], 
                [CognitiveSadness], 
                [CognitiveSurprise]) VALUES (
                @Id,
                @Time,
                @ImageName,
                @FaceRectangle,
                @Anger,
                @Contempt,
                @Disgust,
                @Fear,
                @Happiness,
                @Neutral,
                @Sadness,
                @Surprise)",
            dbRecords);
    }    

    log.Info($"Stored {dbRecords.Count} rows");

    foreach(var dbRecord in dbRecords)
        outputTopic.Add(JsonConvert.SerializeObject(dbRecord));
}

public class Image
{
    public string ImageName { get; set; }
    public Emotion[] Rectangles { get; set; }
}

public class RectangleHandler : SqlMapper.TypeHandler<Rectangle>
{
    public override Rectangle Parse(object value)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(IDbDataParameter parameter, Rectangle value)
    {
        parameter.Value = JsonConvert.SerializeObject(value);
        parameter.DbType = DbType.String;
    }
}