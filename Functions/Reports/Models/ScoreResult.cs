using System;

namespace Functions.Reports.Models
{
    public class ScoreResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public float Score { get; set; }

        public DateTimeOffset Time { get; set; }

        public string Image { get; set; }
    }
}
