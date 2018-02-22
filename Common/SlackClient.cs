using Common;
using Microsoft.Azure.WebJobs.Host;
using Common.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class SlackClient
    {
        private static readonly HttpClient _client = new HttpClient();

        public static async Task SendMessage(SlackMessage message, TraceWriter log, string webHookUrl = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (webHookUrl == null) webHookUrl = Settings.SLACK_WEBHOOK_URL;

            using (var request = new HttpRequestMessage(HttpMethod.Post, webHookUrl))
            {
                var payload = message.ToJson(camelCasingMembers: true);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                log.Info($"Sent to Slack {payload} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}
