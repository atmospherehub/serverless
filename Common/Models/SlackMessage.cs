using Newtonsoft.Json;
using System.Collections.Generic;

namespace Common.Models
{
    public class SlackMessage
    {
        public string Text { get; set; }

        public List<Attachment> Attachments { get; set; }

        public class Attachment
        {
            public string Title { get; set; }

            [JsonProperty("attachment_type")]
            public string AttachmentType { get; set; }

            [JsonProperty("callback_id")]
            public string CallbackId { get; set; }

            [JsonProperty("image_url")]
            public string ImageUrl { get; set; }

            [JsonProperty("thumb_url")]
            public string ThumbnailUrl { get; set; }

            public SlackAction[] Actions { get; set; }

            public string Text { get; set; }

            public string Color { get; set; }

            public List<Field> Fields { get; set; }

            public class Field
            {
                public string Title { get; set; }

                public string Value { get; set; }

                public bool Short { get; set; } = true;
            }
        }

        public class SlackAction
        {
            public string Name { get; set; }

            public string Text { get; set; }

            public string Type { get; set; }

            [JsonProperty("data_source")]
            public string DataSource { get; set; }

            public string Style { get; set; }

            public string Value { get; set; }
        }
    }
}
