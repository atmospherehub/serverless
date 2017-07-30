using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tagging.Models;

namespace Tagging
{
    public static class FinalizeTag
    {
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(FinalizeTag))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-training-sent", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(CleanupTag)}' with message: {message}");
            var slackInput = message.FromJson<TaggingMessage>();

            using (var request = new HttpRequestMessage(HttpMethod.Post, slackInput.ResponseUrl))
            {
                var payload = getMessage(
                    slackInput.FaceId,
                    (await getName(slackInput.FaceUserId))?.FirstName,
                    .ToJson(camelCasingMembers: true);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                log.Info($"Sent to Slack {payload} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }

        private static async Task<UserMap> getName(string faceUserId)
        {
            if (String.IsNullOrEmpty(faceUserId)) throw new ArgumentNullException(nameof(faceUserId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<UserMap>(
                    "SELECT * FROM [dbo].[UsersMap] WHERE SlackUid = @FaceUserId",
                    new { FaceUserId = faceUserId });
            }
        }

        private static SlackMessage getMessage(string faceId, string nameOfTagged) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = $"Tagged as {nameOfTagged}",
                        Text = $"Image was successfully added to the model.",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#36a64f"
                    }
                }
        };
    }
}