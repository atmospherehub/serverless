using Common;
using Common.Models;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data;
using System.Data.SqlClient;
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

            var processedFace = message.FromJson<ProcessedFace>();
            var dataModel = new
            {
                Id = processedFace.FaceId,
                Time = DateTime.UtcNow,
                processedFace.ImageName,
                processedFace.FaceRectangle,
                processedFace.Scores.Anger,
                processedFace.Scores.Contempt,
                processedFace.Scores.Disgust,
                processedFace.Scores.Fear,
                processedFace.Scores.Happiness,
                processedFace.Scores.Neutral,
                processedFace.Scores.Sadness,
                processedFace.Scores.Surprise
            };

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    @"INSERT INTO [dbo].[Faces] (
                        [Id], [Time], [Image], [Rectangle], [CognitiveAnger], [CognitiveContempt], [CognitiveDisgust], [CognitiveFear], [CognitiveHappiness], [CognitiveNeutral], [CognitiveSadness], [CognitiveSurprise]) 
                      VALUES (
                        @Id, @Time, @ImageName, @FaceRectangle, @Anger, @Contempt, @Disgust, @Fear, @Happiness, @Neutral, @Sadness, @Surprise)",
                    dataModel);
            }

            log.Info($"Stored records in DB");
            outputTopic.Add(dataModel.ToJson());
        }
    }

    public class RectangleHandler : SqlMapper.TypeHandler<Face.Rectangle>
    {
        public override Face.Rectangle Parse(object value) => throw new NotImplementedException();

        public override void SetValue(IDbDataParameter parameter, Face.Rectangle value)
        {
            parameter.Value = value.ToJson();
            parameter.DbType = DbType.String;
        }
    }
}