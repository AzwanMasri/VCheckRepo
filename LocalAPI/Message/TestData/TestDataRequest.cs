using VCheck.Lib.Data.Models;

namespace LocalAPI.Message.TestData
{
    public class TestDataRequest
    {
        public HeaderModel Header { get; set; }
        public TestDataRequestBody Body { get; set; }
    }

    public class TestDataRequestBody
    {
        public string? TestCode { get; set; }
        public string? TestName { get; set; }
        public string? ScheduleUniqueKey { get; set; }
    }

    public class TestResultListing
    {
        public string? TestName { get; set; }

        public List<TestResultDetailsModel>? TestResultDetails { get; set; }
    }
}
