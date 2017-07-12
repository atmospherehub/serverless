using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public static JsonSerializerSettings settings = new JsonSerializerSettings()
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, ICollector<string> outputTopic)
{
    var nameValue = await req.Content.ReadAsFormDataAsync();
    log.Info($"Triggered TagFace with {nameValue["payload"]}");

    var slackMessage = JsonConvert.DeserializeObject<SlackResponse>(nameValue["payload"]);
    var faceUserId = slackMessage.Actions[0].SelectedOptions[0].Value;

    outputTopic.Add(JsonConvert.SerializeObject(new TaggingMessage
    {
        FaceId = slackMessage.CallbackId,
        FaceUserId = faceUserId,
        TaggedByName = slackMessage.User.Name,
        TaggedById = slackMessage.User.Id,
        OriginalMessage = slackMessage.OriginalMessage,
        ResponseUrl = slackMessage.ResponseUrl,
        MessageTs = slackMessage.MessageTs
    }));   

    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonConvert.SerializeObject(createResponseMessage(
                slackMessage.OriginalMessage, 
                faceUserId, 
                slackMessage.User.Name), settings), 
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
            CallbackId = String.Empty, // list of users who did tag
            Color = "#3AA3E3",
            Fields = new List<SlackMessage.Field>()
        });
    }

    var att = original.Attachments[2];

    // preserve a list of users who performed tagging
    var usersWhoTagged = att.CallbackId.Split(',').Select(u => u.Trim()).ToList();
    usersWhoTagged.Add(taggedBy);
    att.CallbackId = String.Join(", ", usersWhoTagged.Distinct());
    att.Text = $"Person tagged by {att.CallbackId} as:";

    // show which persons were picked for tagging
    var field = att.Fields.FirstOrDefault(f => f.Title == faceUserId);
    if(field == null)
    {
        field = new SlackMessage.Field
        {
            Title = faceUserId,
            Value = "0"
        };
         att.Fields.Add(field);
    }
    field.Value = (Int32.Parse(field.Value) + 1).ToString(); 

    return original;
}


public class TaggingMessage
{
    public string FaceId { get; set; }
    public string FaceUserId { get; set; }
    public string TaggedById { get; set; }
    public string TaggedByName { get; set; }
    public string ResponseUrl { get; set; }
    public SlackMessage OriginalMessage { get; set; }
    public string MessageTs { get; set; }
}

public class SlackResponse
{
    public SlackAction[] Actions { get; set; }

    [JsonProperty("callback_id")]
    public string CallbackId { get; set; }

    public SlackUser User { get; set; }

    [JsonProperty("message_ts")]
    public string MessageTs { get; set; }

    [JsonProperty("original_message")]
    public SlackMessage OriginalMessage { get; set; }

    public class SlackUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class SlackAction
    {
        [JsonProperty("selected_options")]
        public SlackSelectedOption[] SelectedOptions { get; set; }
    }

    public class SlackSelectedOption
    {
        public string Value { get; set; }
    }

    [JsonProperty("response_url")]
    public string ResponseUrl { get; set; }
}

public class SlackMessage
{
    public string Text { get; set;}
    
    public List<Attachment> Attachments { get; set; }
    
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
        public List<Field> Fields { get; set; }
    }

    public class SlackAction
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        [JsonProperty("data_source")]
        public string DataSource { get; internal set; }
    }

    public class Field
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public bool Short { get; set; } = true;
    }
}