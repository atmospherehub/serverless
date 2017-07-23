using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;

namespace Common
{
    public static class BlobStorageClient
    {
        private static readonly CloudBlobClient _blobClient = CloudStorageAccount.Parse(Settings.STORAGE_CONN_NAME).CreateCloudBlobClient();

        public static Stream DownloadBlob(string containerName, string fileName)
        {
            if (String.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            var container = _blobClient.GetContainerReference(containerName);
            var block = container.GetBlockBlobReference(fileName);
            var stream = new MemoryStream();
            block.DownloadToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static void UploadBlob(string containerName, string fileName, Stream stream)
        {
            if (String.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            
            var container = _blobClient.GetContainerReference(containerName);
            var block = container.GetBlockBlobReference(fileName);
            block.UploadFromStream(stream);
        }
    }
}
