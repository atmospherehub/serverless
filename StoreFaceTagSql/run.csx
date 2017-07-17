using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
public static async Task Run(string mySbMsg, TraceWriter log, ICollector<string> outputFaceTag)
{
    log.Info($"Triggered StoreFaceTagSql by: {mySbMsg}");
    var now = DateTime.UtcNow;
    var slackInput = JsonConvert.DeserializeObject<SlackNotification>(mySbMsg);
    slackInput.Time = now;

    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            @"
            UPDATE [dbo].[FaceTags] set FaceUserId=@FaceUserId, Time=@Time where FaceId=@FaceId and TaggedById=@TaggedById
            IF @@ROWCOUNT=0
                INSERT INTO [dbo].[FaceTags] (
                    [FaceId],
                    [FaceUserId], 
                    [TaggedById], 
                    [TaggedByName],
                    [Time]) VALUES (
                    @FaceId,
                    @FaceUserId,
                    @TaggedById,
                    @TaggedByName,
                    @Time)",
            slackInput);
    }   
    outputFaceTag.Add(JsonConvert.SerializeObject(slackInput));
}

public class SlackNotification
{
    public string FaceId { get; set; }
    public string FaceUserId { get; set; }
    public string TaggedById { get; set; }
    public string TaggedByName { get; set; }
    public DateTimeOffset Time { get; set; }
}