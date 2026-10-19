using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vcheck_Listener.Lib.Models
{
    public class TestDataObject
    {
        public string testid { get; set; }
        public string testname { get; set; }
        public string? testdescription { get; set; }
    }

    public class TestResultReferenceRangeModel
    {
        public long ID { get; set; }
        public string? Parameter { get; set; }
        public string? Analyzer { get; set; }
        public string? Species { get; set; }
        public string? AgeGroup { get; set; }
        public string? NormalGrayZoneAbnormal { get; set; }
        public string? LowNormalHigh { get; set; }
        public string? MeasuringRange { get; set; }
    }

    public class ListenerConfig
    {
        public int Id { get; set; }
        public int IsScheduleAutomationActive { get; set; } = 0;
        public string AnalyzerIP { get; set; } = string.Empty;
        public string AnalyzerPort { get; set; } = string.Empty;
        public string AnalyzerType { get; set; } = string.Empty;
        public string PIMSIP { get; set; } = string.Empty;
        public string PIMSPort { get; set; } = string.Empty;
        public string PIMSUsername { get; set; } = string.Empty;
        public string PIMSPassword { get; set; } = string.Empty;
        public string ConversionChart { get; set; } = string.Empty;
        public string ClinicID { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
    }
}
