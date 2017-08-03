using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Tagging.Models;

namespace Tagging
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
                      WHERE FaceId = @FaceId AND TaggedById = @TaggedById
                      IF @@ROWCOUNT = 0
                        INSERT INTO [dbo].[FaceTags] ([FaceId], [UserId],  [TaggedById], [TaggedByName],[Time]) 
                        VALUES (@FaceId, @UserId, @TaggedById, @TaggedByName, @Time)",
                    new
                    {
                        UserId = tagging.UserId,
                        Time = DateTimeOffset.UtcNow,
                        FaceId = tagging.FaceId,
                        TaggedById = tagging.TaggedById,
                        TaggedByName = tagging.TaggedByName
                    });
            }

            outputQueue.Add(message);
        }
    }
}