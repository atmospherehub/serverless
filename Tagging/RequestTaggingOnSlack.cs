using Common;
using Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tagging.Models;

namespace Tagging
{
    public static class RequestTaggingOnSlack
    {
        private const int MIN_WIDTH = 50;
        private const int MIN_HEIGHT = 50;
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(RequestTaggingOnSlack))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-images-in-db", "tag-slack-sender", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(RequestTaggingOnSlack)}' with message: {message}");
            var face = message.FromJson<Face>();

            // check image size
            SlackMessage requestPayload;
            using (var inputStream = BlobStorageClient.DownloadBlob(Settings.CONTAINER_RECTANGLES, $"{face.Id}.jpg"))
            using (var sourceImage = Image.FromStream(inputStream))
            {
                log.Info($"The size of face thumbnail is {sourceImage.Size}");
                if (sourceImage.Size.Width < MIN_WIDTH || sourceImage.Size.Height < MIN_HEIGHT)
                    requestPayload = getMessageImageTooSmall(face.Id, sourceImage.Size);
                else
                    requestPayload = getMessageForTagging(face.Id);
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, Settings.SLACK_WEBHOOK_URL))
            {
                request.Content = new StringContent(requestPayload.ToJson(camelCasingMembers: true), Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                log.Info($"Sent to Slack {requestPayload} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }

        private static SlackMessage getMessageForTagging(Guid faceId) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = "Please identify user",
                        Text = "The image on the right is a thumbnail version of image below. Avoid tagging if the face on thumb is not clear, blured or too small.",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#36a64f"
                    },
                    new SlackMessage.Attachment
                    {
                        Text = String.Empty,
                        ImageUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_ZOOMIN}/{faceId}.jpg",
                        CallbackId = faceId.ToString(),
                        Actions = new[]
                        {
                            new SlackMessage.SlackAction {
                                Name = "tagged_person",
                                Type = "select",
                                DataSource = "users"
                            }
                        },
                        Color = "#36a64f"
                    }
                }
        };

        private static SlackMessage getMessageImageTooSmall(Guid faceId, Size imageSize) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = "Image won't be used for training",
                        Text = $"The face that appears on image is too small: {imageSize.Width}x{imageSize.Height}px",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#f1828c"
                    }
                }
        };
    }
}