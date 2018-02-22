namespace Common.Models
{
    public class EmotionScores
    {
        public float Anger { get; set; }
        
        public float Contempt { get; set; }
        
        public float Disgust { get; set; }
        
        public float Fear { get; set; }
        
        public float Happiness { get; set; }
        
        public float Neutral { get; set; }
        
        public float Sadness { get; set; }
        
        public float Surprise { get; set; }

        public override bool Equals(object o)
        {
            if (o == null) return false;

            var other = o as EmotionScores;
            if (other == null) return false;

            return this.Anger == other.Anger &&
                this.Disgust == other.Disgust &&
                this.Fear == other.Fear &&
                this.Happiness == other.Happiness &&
                this.Neutral == other.Neutral &&
                this.Sadness == other.Sadness &&
                this.Surprise == other.Surprise;
        }

        public override int GetHashCode()
        {
            return Anger.GetHashCode() ^
                Disgust.GetHashCode() ^
                Fear.GetHashCode() ^
                Happiness.GetHashCode() ^
                Neutral.GetHashCode() ^
                Sadness.GetHashCode() ^
                Surprise.GetHashCode();
        }
    }
}
