using System;

namespace Functions.Reports.Models
{
    public class Report
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string ReportName { get; set; }

        public int Total { get; set; }

        public ScoreResult[] Results { get; set; }
    }
}
