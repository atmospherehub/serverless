using System;
using System.Configuration;

namespace Common
{
    public static class Settings
    {
        public const string STORAGE_CONN_NAME = "AzureWebJobsStorage";
        public static readonly string STORAGE_CONN_STRING = Settings.Get(STORAGE_CONN_NAME);
        public const string SB_CONN_NAME = "ServicebusConnection";
        public static readonly string SQL_CONN_STRING = Settings.GetConnection("Atmosphere");

        public const string CONTAINER_FACES = "faces";
        public const string CONTAINER_RECTANGLES = "rectangles";
        public const string CONTAINER_ZOOMIN = "zoomin";
        public static readonly string IMAGES_ENDPOINT = Settings.Get("ImagesEndpoint");

        public static readonly TimeZoneInfo LOCAL_TIMEZONE = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

        public static readonly string SLACK_WEBHOOK_URL = Settings.Get("SlackWebhookUrl");
        public static readonly string SLACK_API_TOKEN = Settings.Get("SlackApiToken");

        public static readonly string FACE_API_URL = Settings.Get("FaceAPIService");
        public static readonly string FACE_API_TOKEN = Settings.Get("FaceServiceAPIKey");
        public const string FACE_API_GROUP_NANE = "dm-atmosphere";

        public static readonly string EMOTION_API_URL = Settings.Get("EmotionAPIService");
        public static readonly string EMOTION_API_TOKEN = Settings.Get("EmotionServiceAPIKey");

        public static readonly string SENDGRID_API_TOKEN = Settings.Get("SendgreedApiKey");
        public static readonly string SENDER_EMAIL_ADDRESS = Settings.Get("ReportsSenderAddress");

        public static string Get(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return ConfigurationManager.AppSettings[name];
        }

        public static string GetConnection(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return ConfigurationManager.ConnectionStrings[name]?.ConnectionString ?? Get($"ConnectionStrings_{name}");
        }
    }
}
