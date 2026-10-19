using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheck.Lib.Data.Models
{
    public class TestDataObject
    {
        public string testid { get; set; }
        public string testname { get; set; }
        public string? testdescription { get; set; }
    }

    public class TestDataObjectExtended : TestDataObject
    {
        public string species { get; set; }
        public string analyzers { get; set; }
    }
}
