using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Upload.Models;

namespace Upload
{
    public static class CreateZoomIn
    {
        private static readonly ImageCodecInfo _codecInfo = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
        private const int MAX_WIDTH = 600;
        private const bool SHOULD_DRAW_GREEN_RECT = false;

        [FunctionName(nameof(CreateZoomIn))]
        public static void Run(
            [ServiceBusTrigger("atmosphere-images-in-db", "store-zoomin", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(CreateZoomIn)}' with message: {message}");

            var face = message.FromJson<Face>();

            using (var inputStream = BlobStorageClient.DownloadBlob(Settings.CONTAINER_FACES, face.ImageName))
            {
                log.Info($"Loaded image from blob in size of {inputStream.Length}");

                using (var outputStream = zoom(face.FaceRectangle, inputStream))
                {
                    var fileName = $"{face.Id.ToString("D")}.jpg";
                    BlobStorageClient.UploadBlob(Settings.CONTAINER_ZOOMIN, fileName, outputStream);
                    log.Info($"Uploaded image {fileName} blob in size of {outputStream.Length}");
                }
            }
        }

        private static Stream zoom(Face.Rectangle faceArea, Stream inputStream)
        {
            var center = new Point(
                faceArea.Left + faceArea.Width / 2,
                faceArea.Top + faceArea.Height / 2);

            using (var sourceImage = Image.FromStream(inputStream))
            {
                if (SHOULD_DRAW_GREEN_RECT)
                {
                    using (var graphics = Graphics.FromImage(sourceImage))
                        graphics.DrawRectangle(Pens.Lime, new Rectangle(faceArea.Left, faceArea.Top, faceArea.Width, faceArea.Height));
                }

                var zoomArea = getZoomArea(center, sourceImage.Size);
                using (var targetImage = new Bitmap(zoomArea.Width, zoomArea.Height))
                {
                    using (var graphics = Graphics.FromImage(targetImage))
                        graphics.DrawImage(
                            sourceImage,
                            new Rectangle(new Point(), zoomArea.Size),
                            zoomArea,
                            GraphicsUnit.Pixel);

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

        private static Rectangle getZoomArea(Point center, Size imageSize)
        {
            var ratio = (double)imageSize.Width / (double)imageSize.Height;
            var zoomSize = new Size(MAX_WIDTH, (int)Math.Round(MAX_WIDTH / ratio, 0));

            if (zoomSize.Width > imageSize.Width || zoomSize.Height > imageSize.Height)
                throw new InvalidOperationException("Image smaller than zoom-in area");

            var leftTop = new Point();
            if (center.X - zoomSize.Width / 2 > 0)
                leftTop.X = center.X - zoomSize.Width / 2;

            if (center.X + zoomSize.Width / 2 > imageSize.Width)
                leftTop.X = imageSize.Width - zoomSize.Width;

            if (center.Y - zoomSize.Height / 2 > 0)
                leftTop.Y = center.Y - zoomSize.Height / 2;

            if (center.Y + zoomSize.Height / 2 > imageSize.Height)
                leftTop.Y = imageSize.Height - zoomSize.Height;
            return new Rectangle(leftTop, zoomSize);
        }
    }
}