using Newtonsoft.Json;

namespace Recognition.Models
{
    public class SlackActionResponse
    {
        public SlackAction[] Actions { get; set; }

        [JsonProperty("callback_id")]
        public string CallbackId { get; set; }

        public SlackUser User { get; set; }

        [JsonProperty("message_ts")]
        public string MessageTs { get; set; }

        [JsonProperty("original_message")]
        public SlackMessage OriginalMessage { get; set; }

        [JsonProperty("response_url")]
        public string ResponseUrl { get; set; }

        public class SlackUser
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class SlackAction
        {
            public string Name { get; set; }
            
            [JsonProperty("selected_options")]
            public SlackSelectedOption[] SelectedOptions { get; set; }
        }

        public class SlackSelectedOption
        {
            public string Value { get; set; }
        }
    }
}
