using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ServiceBus.Messaging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Upload.Models;

namespace Upload
{
    public static class SendToEmotion
    {
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(SendToEmotion))]
        public static async Task Run(
            [BlobTrigger("faces/{name}", Connection = Settings.STORAGE_CONN_NAME)]Stream inputStream,
            string name,
            [ServiceBus("atmosphere-processed-images", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> outputQueue,
            TraceWriter log)
        {
            log.Info($"Blob trigger '{nameof(SendToEmotion)}' with {name}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, Settings.EMOTION_API_URL))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.EMOTION_API_TOKEN);
                request.Content = new StreamContent(inputStream);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var response = await _client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Failed when calling API {response.StatusCode}: {content}");

                log.Info($"Received from service {response.StatusCode}: {content}");

                var rectangles = content.FromJson<Emotion[]>();
                if (rectangles.Length == 0)
                {
                    log.Error($"Didn't detect faces => image will be delete down the pipe");
                }

                outputQueue.Add((new ProcessedImage
                {
                    ImageName = name,
                    Rectangles = rectangles
                }).ToJson());
            }
        }
    }
}