using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Upload.Models;

namespace Upload
{
    public static class CleanupBlob
    {
        [FunctionName(nameof(CleanupBlob))]                    
        public static void Run(
            [ServiceBusTrigger("atmosphere-processed-images", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [ServiceBus("atmosphere-images-with-faces", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Topic)] ICollector<string> outputTopic,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(CleanupBlob)}' with message: {message}");

            var processedImage = message.FromJson<ProcessedImage>();

            if (processedImage.Rectangles == null || processedImage.Rectangles.Length == 0)
            {
                log.Info($"No faces were detected => deleting blob");
                deleteBlob(processedImage.ImageName);
            }
            else
            {
                log.Info($"{processedImage.Rectangles.Length} faces detected => rasing notifications");
                outputTopic.Add(message);
            }
        }

        private static void deleteBlob(string fileName)
        {
            var storageAccount = CloudStorageAccount.Parse(Settings.Get("funcatmosphere_STORAGE"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(Settings.CONTAINER_FACES);
            var blockBlob = container.GetBlockBlobReference(fileName);
            blockBlob.DeleteIfExists();
        }
    }
}