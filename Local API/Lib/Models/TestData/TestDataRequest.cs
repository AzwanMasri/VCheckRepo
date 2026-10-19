using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheck.Lib.Data.Models;

namespace Local_API.Lib.Models.TestData
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
