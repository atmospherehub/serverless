using System;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Text;
using CM = System.Configuration.ConfigurationManager;


public static async Task Run(string message, TraceWriter log)
{
    log.Info($"Triggered SendSlackPersonIdentificationMessage  by: {message}");
    var face = JsonConvert.DeserializeObject<Face>(message);

    await sendSlackPersonIdentificationMessage(face, log);
}
private static async Task sendSlackPersonIdentificationMessage(Face face, TraceWriter log)
{
    string url = CM.AppSettings["slack_incoming_app_hook_url"];

    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
    {
        var slackMsg = JsonConvert.SerializeObject(getMessage(face.Id.ToString()),
          Formatting.None, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        log.Info($"Will send to Slack: {slackMsg}");
        request.Content = new StringContent(slackMsg, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);

        log.Info($"Received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}

private static SlackMessage getMessage(string imageUrl)
{
    var titleAttachment = new SlackMessage.Attachment
    {
        Title = "Can you identify the person in the center of the picture?",
        ImageUrl = $"{CM.AppSettings["images_endpoint"]}/faces/{imageUrl}.jpg"
    };
    var actionsAttachment = new SlackMessage.Attachment
    {
        CallbackId = imageUrl,
        AttachmentType = "default",
        Actions = new[] { new SlackAction { Name = "tagged_person", Text = "Choose one..", Type = "select", DataSource = "users" } }

    };

    var message = new SlackMessage
    {
        Text = "Please Help us get better",
        Attachments = new SlackMessage.Attachment[] { titleAttachment, actionsAttachment }
    };

    return message;
}


public class SlackMessage
{
    public string Text { get; set; }

    public Attachment[] Attachments { get; set; }

    public class Attachment
    {

        [JsonProperty("attachment_type")]
        internal string AttachmentType { get; set; }

        [JsonProperty("callback_id")]
        internal string CallbackId { get; set; }

        public string Fallback { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        public Field[] Fields { get; set; }
        public SlackAction[] Actions { get; set; }
        public string Title { get; internal set; }
    }
    public class Field
    {
        public string Value { get; set; }

        public bool Short { get; set; }
    }
}

public class SlackAction
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    [JsonProperty("data_source")]
    public string DataSource { get; internal set; }
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