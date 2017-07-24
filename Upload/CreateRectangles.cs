using Common;
using Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Upload
{
    public static class CreateRectangles
    {
        private static readonly ImageCodecInfo _codecInfo = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);

        [FunctionName(nameof(CreateRectangles))]
        public static void Run(
            [ServiceBusTrigger("atmosphere-images-in-db", "store-rectangles", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(CreateRectangles)}' with message: {message}");

            var face = message.FromJson<Face>();

            using (var inputStream = BlobStorageClient.DownloadBlob(Settings.CONTAINER_FACES, face.ImageName))
            {
                log.Info($"Loaded image from blob in size of {inputStream.Length}");

                using (var outputStream = cutRectangle(face.FaceRectangle, inputStream))
                {
                    var fileName = $"{face.Id.ToString("D")}.jpg";
                    BlobStorageClient.UploadBlob(Settings.CONTAINER_RECTANGLES, fileName, outputStream);
                    log.Info($"Uploaded image {fileName} blob in size of {outputStream.Length}");
                }
            }
        }

        public static Stream cutRectangle(Face.Rectangle faceArea, Stream inputStream)
        {
            using (var image = Image.FromStream(inputStream))
            {
                using (var targetImage = new Bitmap(faceArea.Width, faceArea.Height))
                {
                    using (var graphics = Graphics.FromImage(targetImage))
                    {
                        graphics.DrawImage(
                            image, 0, 0,
                            new Rectangle(faceArea.Left, faceArea.Top, faceArea.Width, faceArea.Height),
                            GraphicsUnit.Pixel);
                    }
                    using (var encoders = new EncoderParameters())
                    {
                        encoders.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                        var outputStream = new MemoryStream();
                        targetImage.Save(outputStream, _codecInfo, encoders);
                        outputStream.Seek(0, SeekOrigin.Begin);
                        return outputStream;
                    }
                }
            }
        }
    }
}