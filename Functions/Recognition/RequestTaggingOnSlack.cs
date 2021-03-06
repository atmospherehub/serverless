using Common;
using Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Functions.Recognition.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Functions.Recognition
{
    public static class RequestTaggingOnSlack
    {
        private const int MIN_WIDTH = 50;
        private const int MIN_HEIGHT = 50;

        [FunctionName(nameof(RequestTaggingOnSlack))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-not-identified", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(RequestTaggingOnSlack)}' with message: {message}");
            var face = message.FromJson<Face>();

            // check image size
            using (var inputStream = BlobStorageClient.DownloadBlob(Settings.CONTAINER_RECTANGLES, $"{face.Id}.jpg"))
            using (var sourceImage = Image.FromStream(inputStream))
            {
                log.Info($"The size of face thumbnail is {sourceImage.Size}");
                if (sourceImage.Size.Width < MIN_WIDTH || sourceImage.Size.Height < MIN_HEIGHT)
                    await SlackClient.SendMessage(getMessageImageTooSmall(face.Id, sourceImage.Size), log);
                else
                    await SlackClient.SendMessage(getMessageForTagging(face.Id), log);
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
                        Color = "#3aa3e3"
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
                        Color = "#3aa3e3"
                    }
                }
        };

        private static SlackMessage getMessageImageTooSmall(Guid faceId, Size imageSize) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = "Image cannot be used",
                        Text = $"The face that appears on the image is too small {imageSize.Width}x{imageSize.Height}px",
                        ThumbnailUrl = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{faceId}.jpg",
                        Color = "#f1828c"
                    }
                }
        };
    }
}