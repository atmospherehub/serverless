using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Recognition.Models;

namespace Recognition
{
    public static class StoreFaceTagSql
    {
        [FunctionName(nameof(StoreFaceTagSql))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-tagging", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [ServiceBus("atmosphere-face-tagged", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> outputQueue,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(StoreFaceTagSql)}' with message: {message}");
            var tagging = message.FromJson<TaggingMessage>();

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    @"UPDATE [dbo].[FaceTags] SET UserId = @UserId, Time = @Time 
                      WHERE FaceId = @FaceId AND TaggedByUserId = @TaggedByUserId
                      IF @@ROWCOUNT = 0
                        INSERT INTO [dbo].[FaceTags] ([FaceId], [UserId],  [TaggedByUserId], [TaggedByName],[Time]) 
                        VALUES (@FaceId, @UserId, @TaggedByUserId, @TaggedByName, @Time)",
                    new
                    {
                        UserId = tagging.UserId,
                        Time = DateTimeOffset.UtcNow,
                        FaceId = tagging.FaceId,
                        TaggedByUserId = tagging.TaggedByUserId,
                        TaggedByName = tagging.TaggedByName
                    });
            }

            outputQueue.Add(message);
        }
    }
}