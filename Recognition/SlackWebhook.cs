using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Recognition.Models;

namespace Recognition
{
    public static class SlackWebhook
    {
        [FunctionName(nameof(SlackWebhook))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequestMessage request,
            [ServiceBus("atmosphere-face-tagging", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> taggingQueue,
            [ServiceBus("atmosphere-face-undo-identify", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> undoIdentifyQueue,
            TraceWriter log)
        {
            log.Info($"Triggered '{nameof(SlackWebhook)}'");

            var nameValue = await request.Content.ReadAsFormDataAsync();
            var rawPayload = nameValue["payload"];

            log.Info($"Received payload {rawPayload}");
            var requestMessage = rawPayload.FromJson<SlackActionResponse>();
            SlackMessage responseMessage = null;

            switch (requestMessage.Actions.FirstOrDefault())
            {
                case SlackActionResponse.SlackAction a when a.Name == "tagged_person" && a.SelectedOptions?.FirstOrDefault()?.Value != null:
                    log.Info($"Received action for tagging person");
                    responseMessage = handleTaggedPerson(
                        taggingQueue, 
                        requestMessage, 
                        a.SelectedOptions.First().Value);
                    break;
                case SlackActionResponse.SlackAction a when a.Name == "wrong_identification":
                    log.Info($"Received action for wrong person identified");
                    responseMessage = handleWrongIdentifyPerson(
                        undoIdentifyQueue,
                        a.Value,
                        requestMessage);
                    break;
                default:
                    throw new InvalidOperationException("Unknown action or missing params"); 
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    responseMessage.ToJson(camelCasingMembers: true),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }

        private static SlackMessage handleWrongIdentifyPerson(ICollector<string> undoIdentifyQueue, string previouslyMarked, SlackActionResponse requestMessage)
        {
            undoIdentifyQueue.Add(new UndoIdentifyMessage
            {
                FaceId = requestMessage.CallbackId,
                RequestedByName = requestMessage.User.Name,
                ResponseUrl = requestMessage.ResponseUrl,
            }.ToJson());

            return createTaggingPersonMessage(
                requestMessage.CallbackId,
                requestMessage.User.Name,
                previouslyMarked);
        }

        private static SlackMessage handleTaggedPerson(ICollector<string> taggingQueue, SlackActionResponse requestMessage, string UserId)
        {
            taggingQueue.Add(new TaggingMessage
            {
                FaceId = requestMessage.CallbackId,
                UserId = UserId,
                TaggedByName = requestMessage.User.Name,
                TaggedByUserId = requestMessage.User.Id,
                ResponseUrl = requestMessage.ResponseUrl,
            }.ToJson());

            return createTaggedPersonMessage(
                requestMessage.OriginalMessage,
                requestMessage.User.Name);
        }

        private static SlackMessage createTaggedPersonMessage(SlackMessage original, string taggedBy)
        {
            if (original.Attachments.Count == 2)
            {
                // no tagging was done before - add an attachment to store the votes
                original.Attachments.Add(new SlackMessage.Attachment()
                {
                    CallbackId = null, // list of users who did tag,
                    Color = "#3aa3e3",
                    Fields = new List<SlackMessage.Attachment.Field>()
                });
            }

            var attachment = original.Attachments[2];

            // preserve a list of users who performed tagging
            var usersWhoTagged = (attachment.CallbackId ?? "[]").FromJson<List<string>>()
                .Union(new string[] { taggedBy })
                .Distinct()
                .ToList();

            // store users who voted till now + the user that voted now
            attachment.CallbackId = usersWhoTagged.ToJson();
            attachment.Text = $"Person tagged by {usersWhoTagged.ToUsersList()}";

            return original;
        }

        private static SlackMessage createTaggingPersonMessage(string faceId, string requestedBy, string previouslyMarked) => new SlackMessage
        {
            Attachments = new List<SlackMessage.Attachment>
                {
                    new SlackMessage.Attachment
                    {
                        Title = "Wrong identification",
                        Text = $"{requestedBy} thinks that the person on the image is not {previouslyMarked}. Avoid tagging if the face on thumb is not clear, blured or too small.",
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
    }
}