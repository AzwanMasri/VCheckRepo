using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheck.Interface.API.Lib.General
{
    public class LocationDataRequest
    {
        public HeaderModel Header { get; set; }
        public GetLocationDataRequestBody Body { get; set; }
    }

    public class GetLocationDataRequestBody
    {
        public string? ClinicID { get; set; }
    }
}
