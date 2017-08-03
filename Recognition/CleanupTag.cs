using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Recognition.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Recognition
{
    public static class CleanupTag
    {
        [FunctionName(nameof(CleanupTag))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-cleanup", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(CleanupTag)}' with message: {message}");
            var slackInput = message.FromJson<TaggingMessage>();

            var rowsDeleted = await cleanupDb(slackInput.FaceId);
            log.Info($"{rowsDeleted} rows cleaned up from DB for faceId {slackInput.FaceId}");

            await SlackClient.SendMessage(getMessage(slackInput.FaceId), log, slackInput.ResponseUrl);
        }

        private static async Task<int> cleanupDb(string faceId)
        {
            if (String.IsNullOrEmpty(faceId)) throw new ArgumentNullException(nameof(faceId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "DELETE FROM [dbo].[FaceTags] WHERE FaceId = @FaceId",
                    new { FaceId = faceId });
            }
        }

        private static SlackMessage getMessage(string faceId) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = "Something went wrong",
                        Text = $"The image cannot be used for model. Either it is too small, the face is blured or not visible.",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#f1828c"
                    }
                }
        };
    }
}