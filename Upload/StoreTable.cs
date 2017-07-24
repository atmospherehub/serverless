using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.IO;
using Upload.Models;

namespace Upload
{
    public static class StoreTable
    {
        [FunctionName(nameof(StoreTable))]
        public static void Run(
            [ServiceBusTrigger("atmosphere-images-with-faces", "store-table", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [Table("faces", Connection = Settings.STORAGE_CONN_NAME)]ICollector<ProcessedImageInTable> facesTable,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(StoreTable)}' with message: {message}");

            var processedImage = message.FromJson<ProcessedImageInTable>();
            processedImage.PartitionKey = DateTime.UtcNow.ToString("yy-MM-dd");
            processedImage.RowKey = Path.GetFileNameWithoutExtension(processedImage.ImageName);
            facesTable.Add(processedImage);
        }
    }
}