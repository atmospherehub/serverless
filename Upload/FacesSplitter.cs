using Common;
using Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Linq;
using Upload.Models;

namespace Upload
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

            if (processedImage.Rectangles == null || processedImage.Rectangles.Length == 0)
            {
                log.Info($"No faces were detected => deleting blob");
                BlobStorageClient.DeleteBlob(Settings.CONTAINER_FACES, processedImage.ImageName);
            }
            else
            {
                log.Info($"{processedImage.Rectangles.Length} faces detected => rasing notifications");

                processedImage
                    .Rectangles
                    .Select(r => new ProcessedFace()
                    {
                        FaceId = Guid.NewGuid(),
                        ImageName = processedImage.ImageName,
                        Scores = r.Scores,
                        FaceRectangle = new Face.Rectangle
                        {
                            Height = r.FaceRectangle.Height,
                            Left = r.FaceRectangle.Left,
                            Top = r.FaceRectangle.Top,
                            Width = r.FaceRectangle.Width
                        }
                    })
                    .ToList()
                    .ForEach(f => outputTopic.Add(f.ToJson()));
            }
        }
    }
}