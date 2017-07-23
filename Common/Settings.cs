using System;
using System.Configuration;

namespace Common
{
    public static class Settings
    {
        public const string STORAGE_CONN_NAME = "funcatmosphere_STORAGE";

        public const string SB_CONN_NAME = "dm-common-servicebus";

        public const string CONTAINER_FACES = "faces";
        public const string CONTAINER_RECTANGLES = "rectangles";
        public const string CONTAINER_ZOOMIN = "zoomin";

        public static readonly string SQL_CONN_STRING = Settings.GetConnection("Atmosphere");

        public static string Get(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return ConfigurationManager.AppSettings[name];
        }

        public static string GetConnection(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
        }
    }
}
