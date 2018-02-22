using Common.Models;
using System;

namespace Functions.Upload.Models
{
    class ProcessedFace
    {
        public Guid FaceId { get; set; }

        public string ImageName { get; set; }

        public FaceRectangle FaceRectangle { get; set; }

        public EmotionScores Scores { get; set; }

        public int ClientId { get; set; }
    }
}
