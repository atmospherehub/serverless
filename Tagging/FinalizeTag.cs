using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
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
                var payload = getMessage(slackInput.FaceId).ToJson(camelCasingMembers: true);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                log.Info($"Sent to Slack {payload} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }

        private static SlackMessage getMessage(string faceId) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = "Success",
                        Text = $"Image was successfully added to the model.",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#36a64f"
                    }
                }
        };
    }
}