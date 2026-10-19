using VCheck.Lib.Data.Models;

namespace LocalAPI.Message.ScheduleData
{
    public class ScheduleDataRequest
    {
        public HeaderModel Header { get; set; }
        public ScheduleDataRequestBody Body { get; set; }
    }

    public class ScheduleDataRequestBody
    {
        public bool ExtendDateTime { get; set; } = false;
        public string? ScheduledUniqueID { get; set; }
        public string? ScheduledDatetime { get; set; }
        public string? InchargePerson { get; set; }
        public string? OrderID { get; set; }
        public string? LocationID { get; set; }
        public string? PatientID { get; set; }
        public string? ClientName { get; set; }
        public List<string>? Parameters { get; set; }
        public string? AnalyzerName { get; set; }
        public string? TestName { get; set; }
        public bool IgnoreOrderStatus { get; set; } = false;
    }

    public class ScheduleDataResponse
    {
        public HeaderModel Header { get; set; }
        public ScheduleDataResponseBody Body { get; set; }
    }

    public class ScheduleDataResponseBody
    {
        public String? responsecode { get; set; }
        public String? responsestatus { get; set; }
        public String? responsemessage { get; set; }
        public List<ScheduleResultObject> results { get; set; }
    }

    public class ScheduleResultObject
    {
        public String? scheduleduniqueid { get; set; }
        public String? scheduledtesttype { get; set; }
        public String? scheduleddatetime { get; set; }
        public String? scheduledby { get; set; }
        public String? patientid { get; set; }
        public String? inchargeperson { get; set; }
    }
}
