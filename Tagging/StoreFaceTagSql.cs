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
            [ServiceBus("atmosphere-face-tagged", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> outputTopic,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(StoreFaceTagSql)}' with message: {message}");
            var tagging = message.FromJson<TaggingMessage>();

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    @"UPDATE [dbo].[FaceTags] SET FaceUserId = @FaceUserId, Time = @Time 
                      WHERE FaceId = @FaceId AND TaggedById = @TaggedById
                      IF @@ROWCOUNT = 0
                        INSERT INTO [dbo].[FaceTags] ([FaceId], [FaceUserId],  [TaggedById], [TaggedByName],[Time]) 
                        VALUES (@FaceId, @FaceUserId, @TaggedById, @TaggedByName, @Time)",
                    new
                    {
                        FaceUserId = tagging.FaceUserId,
                        Time = DateTimeOffset.UtcNow,
                        FaceId = tagging.FaceId,
                        TaggedById = tagging.TaggedById,
                        TaggedByName = tagging.TaggedByName
                    });
            }

            outputTopic.Add(message);
        }
    }
}