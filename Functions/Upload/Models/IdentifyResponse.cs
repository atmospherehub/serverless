using Common.Models;
using System;
using System.Collections.Generic;

namespace Functions.Upload.Models
{
    public class IdentifyResponse : List<IdentifyResponse.IdentifiedFace>
    {
        public class IdentifiedFace
        {
            public Guid FaceId { get; set; }

            public FaceRectangle FaceRectangle { get; set; }

            public Faceattributes FaceAttributes { get; set; }

        }

        public class Faceattributes
        {
            public EmotionScores Emotion { get; set; }
        }
    }
}
