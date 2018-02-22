using Common.Models;
using System;

namespace Functions.Upload.Models
{
    public class DetectedFace
    {
        public Guid FaceId { get; set; }

        public FaceRectangle FaceRectangle { get; set; }

        public EmotionScores Scores { get; set; }
    }
}
