using System.Collections.Generic;

namespace Recognition.Models
{
    public class CognitiveIdentifyResponse : List<CognitiveIdentifyResponse.Face>
    {
        public class Face
        {
            public string FaceId { get; set; }

            public Candidate[] Candidates { get; set; }
        }

        public class Candidate
        {
            public string PersonId { get; set; }

            public double Confidence { get; set; }
        }
    }
}
