using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckListenerWorker_Others.Lib.Models
{
    public class MainModel
    {
        string _deviceSerialNum;

        public string DeviceSerialNum
        {
            get { return _deviceSerialNum; }
            set { _deviceSerialNum = value; }
        }

        string _deviceType;

        public string DeviceType
        {
            get { return _deviceType; }
            set { _deviceType = value; }
        }
    }
}
