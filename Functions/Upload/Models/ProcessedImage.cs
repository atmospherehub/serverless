using Common.Models;

namespace Functions.Upload.Models
{
    public class ProcessedImage
    {
        public string ImageName { get; set; }

        public Emotion[] Rectangles { get; set; }

        public int ClientId { get; set; }
    }
}
