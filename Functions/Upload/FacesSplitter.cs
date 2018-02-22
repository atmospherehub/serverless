using Common;
using Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Linq;
using Functions.Upload.Models;

namespace Functions.Upload
{
    public static class FacesSplitter
    {
        [FunctionName(nameof(FacesSplitter))]
        public static void Run(
            [ServiceBusTrigger("atmosphere-processed-images", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [ServiceBus("atmosphere-images-with-faces", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Topic)] ICollector<string> outputTopic,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(FacesSplitter)}' with message: {message}");

            var processedImage = message.FromJson<ProcessedImage>();

            if (processedImage.Faces.Length == 0)
            {
                log.Info($"No faces were detected => deleting blob");
                BlobStorageClient.DeleteBlob(Settings.CONTAINER_FACES, processedImage.ImageName);
            }
            else
            {
                log.Info($"{processedImage.Faces.Length} faces detected => rasing notifications");

                processedImage
                    .Faces
                    .Select(r => new ProcessedFace()
                    {
                        FaceId = r.FaceId,
                        ImageName = processedImage.ImageName,
                        Scores = r.Scores,
                        FaceRectangle = r.FaceRectangle,
                        ClientId = processedImage.ClientId
                    })
                    .ToList()
                    .ForEach(f => outputTopic.Add(f.ToJson()));
            }
        }
    }
}