using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ProjectOxford.Common.Contract;
using System.Globalization;
using CM = System.Configuration.ConfigurationManager;

public static JsonSerializerSettings settings = new JsonSerializerSettings()
{
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

public static async Task Run(string message, TraceWriter log)
{
    log.Info($"Triggered NotifySlack by: {message}");
    var processedImage = JsonConvert.DeserializeObject<ProcessedImage>(message);
    
    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, CM.AppSettings["slack_incoming_url"]))
    {
        var payload = JsonConvert.SerializeObject(getMessage(
            getPayload(processedImage), 
            processedImage.ImageName), settings);
        log.Info($"Will send to Slack: {payload}");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);

        log.Info($"Received from service {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}

private static string getPayload(ProcessedImage processedImage)
{
    var jsonSerializer = JsonSerializer.Create();
    var sb = new StringBuilder(256);
    var sw = new StringWriter(sb, CultureInfo.InvariantCulture);
    using (var jsonWriter = new JsonTextWriterRoundedDecimal(sw))
    {
        jsonWriter.Formatting = Formatting.Indented;
        jsonSerializer.Serialize(jsonWriter, processedImage.Rectangles.Select(r => r.Scores));
    }
    return sb.ToString();
}

private static SlackMessage getMessage(string payload, string imageUrl)
{
    var message = new SlackMessage
    {
        Text = "Faces detected"
    };
    message.Attachments = new SlackMessage.Attachment[]{ new SlackMessage.Attachment
    {
        Fallback = $"{CM.AppSettings["images_endpoint"]}/faces/{imageUrl}",
        ImageUrl = $"{CM.AppSettings["images_endpoint"]}/faces/{imageUrl}"
    }};
    message.Attachments[0].Fields = new SlackMessage.Field[]{ new SlackMessage.Field
    {
        Value = payload
    }};
    return message;
}

public class SlackMessage
{
    public string Text { get; set;}

    public Attachment[] Attachments { get; set;}

    public class Attachment
    {
        public string Fallback { get; set;}

        [JsonProperty("image_url")]
        public string ImageUrl { get; set;}

        public Field[] Fields { get; set;}
        
    }

    public class Field
    {
        public string Value { get; set;}

        public bool Short { get; set;}
    }
}

public class ProcessedImage
{
    public string ImageName { get; set; }

    public Emotion[] Rectangles { get; set; }
}

public class JsonTextWriterRoundedDecimal : JsonTextWriter
{
    public JsonTextWriterRoundedDecimal(TextWriter textWriter)
        : base(textWriter)
    {
    }
    public override void WriteValue(decimal value)
    {
        base.WriteValue(Math.Round(value, 4));
    }
}