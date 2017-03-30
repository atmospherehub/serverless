using Microsoft.ProjectOxford.Common.Contract;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CM = System.Configuration.ConfigurationManager;

private const string EMOTION_SERVICE = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize";

public static async Task<ProcessedImage> Run(Stream stream, string name, TraceWriter log)
{
    log.Info($"Triggered ProcessImage blob:{name} {stream.Length} Bytes");

    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, EMOTION_SERVICE))
    {
        request.Headers.Add("Ocp-Apim-Subscription-Key", CM.AppSettings["EmotionServiceAPIKey"]);
        request.Content = new StreamContent(stream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await client.SendAsync(request);
        var responseContent = String.Empty;
        if (response.Content != null)
            responseContent = await response.Content.ReadAsStringAsync();

        if(String.IsNullOrEmpty(responseContent))
        {
            log.Error($"Didn't get proper response from service: {response.StatusCode}");
            // TODO: retry?
            return new ProcessedImage { ImageName = name };
        }

        log.Info($"Received from service {response.StatusCode}: {responseContent}");

        var rectangles = JsonConvert.DeserializeObject<Emotion[]>(responseContent);
        if(rectangles.Length == 0)
        {
            log.Error($"Didn't detect faces");
            return new ProcessedImage { ImageName = name };
        }
    
        return new ProcessedImage {
            ImageName = name,
            Rectangles = rectangles
        };
    }
}

public class ProcessedImage
{
    public string ImageName { get; set; }

    public Emotion[] Rectangles { get; set; }
}