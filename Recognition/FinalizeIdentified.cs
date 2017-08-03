using Common;
using Common.Models;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Recognition.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Recognition
{
    public static class FinalizeIdentified
    {
        [FunctionName(nameof(FinalizeIdentified))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-identified", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(FinalizeIdentified)}' with message: {message}");
            var face = message.FromJson<Face>();
            var firstName = await getUserName(face.Id);
            await SlackClient.SendMessage(getMessage(face.Id.ToString(), firstName), log);
        }

        private static async Task<string> getUserName(Guid faceId)
        {
            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                var result = await connection.QueryAsync(@"
                    SELECT m.FirstName
                    FROM [dbo].[Faces] AS f
                    LEFT JOIN [dbo].[UsersMap] AS m ON m.UserId = f.UserId
                    WHERE f.Id = @FaceId",
                    new { FaceId = faceId });
                return result.FirstOrDefault()?.FirstName;
            }
        }

        private static SlackMessage getMessage(string faceId, string nameOfTagged) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = $"Identified as {nameOfTagged}",
                        Text = $"The person on the image was successfully recognized.",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#36a64f"
                    }
                }
        };
    }
}