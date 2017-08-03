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
using Tagging.Models;

namespace Tagging
{
    public static class FaceTag
    {
        [FunctionName(nameof(FaceTag))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequestMessage request,
            [ServiceBus("atmosphere-face-tagging", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> outputQueue,
            TraceWriter log)
        {
            log.Info($"Triggered '{nameof(FaceTag)}'");

            var nameValue = await request.Content.ReadAsFormDataAsync();
            var rawPayload = nameValue["payload"];

            log.Info($"Received payload {rawPayload}");

            var slackMessage = rawPayload.FromJson<SlackActionResponse>();
            var userId = slackMessage.Actions?.FirstOrDefault()?.SelectedOptions?.FirstOrDefault()?.Value;

            if (String.IsNullOrEmpty(userId)) throw new InvalidOperationException("No user was selected");

            outputQueue.Add(new TaggingMessage
            {
                FaceId = slackMessage.CallbackId,
                UserId = userId,
                TaggedByName = slackMessage.User.Name,
                TaggedByUserId = slackMessage.User.Id,
                OriginalMessage = slackMessage.OriginalMessage,
                ResponseUrl = slackMessage.ResponseUrl,
                MessageTs = slackMessage.MessageTs
            }.ToJson());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    createResponseMessage(
                        slackMessage.OriginalMessage,
                        slackMessage.User.Name).ToJson(camelCasingMembers: true),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }

        private static SlackMessage createResponseMessage(SlackMessage original, string taggedBy)
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
    }
}