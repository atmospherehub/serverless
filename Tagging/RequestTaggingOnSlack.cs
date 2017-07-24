using Common;
using Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tagging.Models;

namespace Tagging
{
    public static class RequestTaggingOnSlack
    {
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(RequestTaggingOnSlack))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-images-in-db", "tag-slack-sender", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(RequestTaggingOnSlack)}' with message: {message}");
            var face = message.FromJson<Face>();

            using (var request = new HttpRequestMessage(HttpMethod.Post, Settings.Get("slack_webhook_url")))
            {
                var payload = getMessage(face.Id).ToJson(camelCasingMembers: true);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                log.Info($"Sent to Slack {payload} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }

        private static SlackMessage getMessage(Guid faceId)
        {
            return new SlackMessage
            {
                Attachments = new SlackMessage.Attachment[] {
                new SlackMessage.Attachment {
                    Title = "Please identify user in the center of the image",
                    ImageUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_ZOOMIN}/{faceId}.jpg"
                },
                new SlackMessage.Attachment {
                    AttachmentType = "default",
                    Color = "#3AA3E3",
                    CallbackId = faceId.ToString(),
                    Actions = new[] {
                        new SlackMessage.SlackAction {
                            Name = "tagged_person",
                            Type = "select",
                            DataSource = "users"
                        }
                    }
                }
                }
            };
        }
    }
}