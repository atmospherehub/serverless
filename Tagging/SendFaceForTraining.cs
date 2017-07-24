using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;

namespace Tagging
{
    public static class SendFaceForTraining
    {
        [FunctionName(nameof(SendFaceForTraining))]
        public static void Run(
            [ServiceBusTrigger("atmosphere-face-tagged", "face-training", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(SendFaceForTraining)}' with message: {message}");
        }
    }
}