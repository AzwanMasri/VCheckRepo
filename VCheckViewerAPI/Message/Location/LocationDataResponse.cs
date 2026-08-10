using VCheck.Lib.Data.Models;

namespace VCheckViewerAPI.Message.Location
{
    public class LocationDataResponse
    {
    }

    public class LocationDataResponseBody 
    {
        public String? responsecode { get; set; }
        public String? responsestatus { get; set; }
        public String? responsemessage { get; set; }
        public List<LocationResultObject>? results { get; set; }
    }

    //public class LocationResultObject
    //{
    //    public String? locationid { get; set; }
    //    public String? name { get; set; }
    //    public String? address { get; set; }
    //    public String? contactname { get; set; }
    //    public String? phonenum { get; set; }
    //    public String? email { get; set; }
    //    public String? status { get; set; }
    //    public String? createddate { get; set; }
    //    public String? modifieddate { get; set; }
    //}
}
