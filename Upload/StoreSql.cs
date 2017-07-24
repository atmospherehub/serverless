using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ProjectOxford.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Upload.Models;

namespace Upload
{
    public static class StoreSql
    {
        static StoreSql()
        {
            SqlMapper.AddTypeHandler(new RectangleHandler());
        }

        [FunctionName(nameof(StoreSql))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-images-with-faces", "store-sql", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [ServiceBus("atmosphere-images-in-db", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Topic)] ICollector<string> outputTopic,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(StoreSql)}' with message: {message}");

            var processedImage = message.FromJson<ProcessedImage>();
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

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
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

            foreach (var dbRecord in dbRecords)
                outputTopic.Add(dbRecord.ToJson());
        }
    }

    public class RectangleHandler : SqlMapper.TypeHandler<Rectangle>
    {
        public override Rectangle Parse(object value) => throw new NotImplementedException();

        public override void SetValue(IDbDataParameter parameter, Rectangle value)
        {
            parameter.Value = value.ToJson();
            parameter.DbType = DbType.String;
        }
    }
}