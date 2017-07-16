using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
public static async Task Run(string mySbMsg, TraceWriter log)
{
    var now = DateTime.UtcNow;
    var SlackInput = JsonConvert.DeserializeObject<SlackNotification>(mySbMsg);
    SlackInput.Time = now;
    log.Info($"Triggered StoreFaceTagSql by: {SlackInput.FaceId}");
        using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            @"
            UPDATE [dbo].[FaceTags] set FaceUserId=@FaceUserId where FaceId=@FaceId and TaggedById=@TaggedById
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
            SlackInput);
    }   
}

public class SlackNotification
{
    public string FaceId { get; set; }
    public string FaceUserId { get; set; }
    public string TaggedById { get; set; }
    public string TaggedByName { get; set; }
    public DateTime Time { get; set; }
}