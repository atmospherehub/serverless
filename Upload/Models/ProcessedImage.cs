using Microsoft.ProjectOxford.Common.Contract;

namespace Upload.Models
{
    public class ProcessedImage
    {
        public string ImageName { get; set; }

        public Emotion[] Rectangles { get; set; }
    }
}
