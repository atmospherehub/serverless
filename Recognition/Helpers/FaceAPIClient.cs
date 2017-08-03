using Common;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Recognition
{
    internal static class FaceAPIClient
    {
        private static readonly HttpClient _client = new HttpClient();        

        public static async Task<T> Call<T>(string apiPath, dynamic requestObject, TraceWriter log)
            where T : class
        {
            var payload = ((object)(requestObject ?? new { })).ToJson();
            log.Info($"Calling API at path [{apiPath}] with payload {payload}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{Settings.FACE_API_URL}{apiPath}"))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.FACE_API_TOKEN);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Received result from faces API {response.StatusCode}: {contents}");

                log.Info($"Received result from faces API {response.StatusCode}: {contents}");
                return contents.FromJson<T>();
            }
        }
    }
}
