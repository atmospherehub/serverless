using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using CM = System.Configuration.ConfigurationManager;

public static void Run(string message, TraceWriter log)
{
    log.Info($"Triggered NotifyMQTT by: {message}");
    sendMessage(log, message);
}

private static void sendMessage(TraceWriter log, string message)
{
    var client = new MqttClient(CM.AppSettings["mqtt_host"], 16742, false, null, null, MqttSslProtocols.None);
    var clientId = Guid.NewGuid().ToString();

    log.Info($"Client {clientId} connecting to {CM.AppSettings["mqtt_host"]}");
    var connectResponse = client.Connect(
        Guid.NewGuid().ToString(),
        CM.AppSettings["mqtt_user"],
        CM.AppSettings["mqtt_password"]);
    client.MqttMsgPublished += messagePublished;

    log.Info($"Client {clientId} connection result {connectResponse}");

    var messageId = client.Publish(
        CM.AppSettings["mqqt_topic"],
        Encoding.ASCII.GetBytes(message.ToLowerInvariant()),
        MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
        false);
}

private static void messagePublished(object sender, MqttMsgPublishedEventArgs e)
{
    ((MqttClient)sender).Disconnect();
}
