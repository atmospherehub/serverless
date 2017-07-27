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
            [ServiceBus("atmosphere-face-tagging", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> outputTopic,
            TraceWriter log)
        {
            log.Info($"Triggered '{nameof(FaceTag)}'");

            var nameValue = await request.Content.ReadAsFormDataAsync();
            var rawPayload = nameValue["payload"];

            log.Info($"Received payload {rawPayload}");

            var slackMessage = rawPayload.FromJson<SlackResponse>();
            var faceUserId = slackMessage.Actions?.FirstOrDefault()?.SelectedOptions?.FirstOrDefault()?.Value;

            if (String.IsNullOrEmpty(faceUserId)) throw new InvalidOperationException("No user was selected");

            outputTopic.Add(new TaggingMessage
            {
                FaceId = slackMessage.CallbackId,
                FaceUserId = faceUserId,
                TaggedByName = slackMessage.User.Name,
                TaggedById = slackMessage.User.Id,
                OriginalMessage = slackMessage.OriginalMessage,
                ResponseUrl = slackMessage.ResponseUrl,
                MessageTs = slackMessage.MessageTs
            }.ToJson());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    createResponseMessage(
                        slackMessage.OriginalMessage,
                        faceUserId,
                        slackMessage.User.Name).ToJson(camelCasingMembers: true),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }

        private static SlackMessage createResponseMessage(SlackMessage original, string faceUserId, string taggedBy)
        {
            if (original.Attachments.Count == 2)
            {
                // no tagging was done before - add an attachment to store the votes
                original.Attachments.Add(new SlackMessage.Attachment()
                {
                    CallbackId = String.Empty, // list of users who did tag,
                    Color = "#36a64f",
                    Fields = new List<SlackMessage.Attachment.Field>()
                });
            }

            var attachment = original.Attachments[2];

            // preserve a list of users who performed tagging
            var usersWhoTaggedTillNow = attachment.CallbackId
                .Split(',')
                .Select(u => u.Trim())
                .Where(u => !String.IsNullOrEmpty(u))
                .ToList();

            // store users who voted till now + the user that voted now
            attachment.CallbackId = String.Join(
                ", ",
                usersWhoTaggedTillNow.Union(new string[] { taggedBy }).Distinct());
            attachment.Text = $"Person tagged by {attachment.CallbackId} as:";

            // show which persons were picked for tagging
            var field = attachment.Fields.FirstOrDefault(f => f.Title == faceUserId);
            if (field == null)
            {
                field = new SlackMessage.Attachment.Field
                {
                    Title = faceUserId,
                    Value = "0"
                };
                attachment.Fields.Add(field);
            }

            field.Value = (Int32.Parse(field.Value) + 1).ToString();

            return original;
        }
    }
}