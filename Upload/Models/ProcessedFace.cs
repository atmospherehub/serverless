using Common.Models;
using Microsoft.ProjectOxford.Common.Contract;
using System;

namespace Upload.Models
{
    class ProcessedFace
    {
        public Guid FaceId { get; set; }

        public string ImageName { get; set; }

        public Face.Rectangle FaceRectangle { get; set; }

        public EmotionScores Scores { get; set; }

        public int ClientId { get; set; }
    }
}
