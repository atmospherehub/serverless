using System;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Text;
using CM = System.Configuration.ConfigurationManager;

public static JsonSerializerSettings settings = new JsonSerializerSettings()
{
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

public static async Task Run(string message, TraceWriter log)
{
    log.Info($"Triggered SendFaceTaggingSlack  by: {message}");
    var face = JsonConvert.DeserializeObject<Face>(message);
    await sendSlackPersonIdentificationMessage(face, log);
}

private static async Task sendSlackPersonIdentificationMessage(Face face, TraceWriter log)
{
    string url = CM.AppSettings["slack_webhook_url"];

    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
    {
        var slackMsg = JsonConvert.SerializeObject(
            getMessage(face.Id.ToString()), 
            Formatting.None,
            settings);
                    
        request.Content = new StringContent(slackMsg, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);

        log.Info($"Sent to Slack {slackMsg} and received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}

private static SlackMessage getMessage(string faceId)
{
    return new SlackMessage
    {
        Attachments = new SlackMessage.Attachment[] {
            new SlackMessage.Attachment {
                Title = "Please identify user in the center of the image",
                ImageUrl = $"{CM.AppSettings["images_endpoint"]}/zoomin/{faceId}.jpg"
            },
            new SlackMessage.Attachment {
                AttachmentType = "default",
                Color = "#3AA3E3",
                CallbackId = faceId,
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


public class SlackMessage
{
    public string Text { get; set;}
    
    public Attachment[] Attachments { get; set; }
    
    public class Attachment
    {
        internal string Title { get; set; }
        [JsonProperty("attachment_type")]
        internal string AttachmentType { get; set; }
        [JsonProperty("callback_id")]
        internal string CallbackId { get; set; }
        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }
        public SlackAction[] Actions { get; set; }
        public string Text { get; set; }
        public string Color { get; set; }
    }

    public class SlackAction
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        [JsonProperty("data_source")]
        public string DataSource { get; internal set; }
    }
}

public class Face
{
    public Guid Id { get; set; }
    public string ImageName { get; set; }
    public Rectangle FaceRectangle { get; set; }

    public class Rectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}