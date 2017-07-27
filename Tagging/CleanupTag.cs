using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tagging.Models;
using System;
using System.Data.SqlClient;
using Dapper;

namespace Tagging
{
    public static class CleanupTag
    {
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(CleanupTag))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-cleanup", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(CleanupTag)}' with message: {message}");
            var slackInput = message.FromJson<TaggingMessage>();

            var rowsDeleted = await cleanupDb(slackInput.FaceId);
            log.Info($"{rowsDeleted} rows cleaned up from DB for faceId {slackInput.FaceId}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, slackInput.ResponseUrl))
            {
                var payload = getMessage(slackInput.FaceId).ToJson(camelCasingMembers: true);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                log.Info($"Sent to Slack {payload} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
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
                        Title = "Please identify user",
                        Text = $"Something went wrong - we cannot user this image for training.",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#f1828c"
                    }
                }
        };
    }
}