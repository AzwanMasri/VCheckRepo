using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using VCheck.Lib.Data.Models;
using VCheckListenerWorker_Others.Lib.Object;

namespace VCheckListenerWorker_Others.Lib.Logic.HL7.V231
{
    public class OrderRepository
    {
        public async static Task<string> GenerateOrderMessageORM(List<ScheduledTestModelExtended> scheduledTestModelExtended)
        {

            try
            {
                StringBuilder mainframe = new StringBuilder();

                foreach (var schedule in scheduledTestModelExtended)
                {
                    AckRepository.Species.Category = schedule.Schedule.Species;
                    var gender = string.Concat(schedule.Schedule.Gender.Split(' ').Where(w => w.Length > 0).Select(w => w[0]));
                    string barcode = schedule.Schedule.ScheduleUniqueID.Split("-")[3];
                    string testType = schedule.Schedule.ScheduledTestType.Contains("CDR") ? "Blood^CBC^DIFF^RET" : "Blood^CBC^DIFF";
                    StringBuilder frame = new StringBuilder();
                    frame.Append((char)0x0B);
                    Message response = new Message();

                    // ------------- Message Header ------------//
                    Segment msh = new Segment("MSH");
                    msh.Field(1, "|");
                    msh.Field(2, "^~\\&");
                    msh.Field(3, "VCheck");
                    msh.Field(4, "VCheck H6");
                    msh.Field(5, "Menarini");
                    msh.Field(6, "LIS");
                    msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    msh.Field(9, "ORM^O01");
                    msh.Field(10, "1");
                    msh.Field(11, "P");
                    msh.Field(12, "2.3.1");
                    msh.Field(16, "0");
                    msh.Field(18, "UTF-8");
                    response.Add(msh);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    // ------------- Patient Identification Segment ------------//
                    response = new Message();
                    Segment pid = new Segment("PID");
                    pid.Field(1, "2");
                    pid.Field(2, "1001");
                    pid.Field(3, schedule.Schedule.PatientID);
                    pid.Field(4, "02");
                    pid.Field(5, schedule.Schedule.PatientName);
                    pid.Field(6, schedule.Schedule.OwnerName);
                    pid.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    pid.Field(8, gender);
                    pid.Field(9, "O");
                    pid.Field(11, "Bionote");
                    pid.Field(12, "541000");
                    pid.Field(13, "12345678900");
                    pid.Field(14, "12");
                    pid.Field(15, "M");
                    pid.Field(18, "General");
                    pid.Field(19, "VCheck");
                    pid.Field(23, AckRepository.Species.SubCategory);
                    pid.Field(26, "12.36");
                    pid.Field(27, "Veter");
                    response.Add(pid);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    // ------------- Common Order Segment ------------//
                    response = new Message();
                    Segment orc = new Segment("ORC");
                    orc.Field(1, "NW");
                    orc.Field(2, "01");
                    orc.Field(3, schedule.Schedule.PatientID);
                    orc.Field(4, barcode);
                    orc.Field(15, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    orc.Field(17, "KESHI");
                    orc.Field(21, "WWW");
                    orc.Field(22, "");
                    orc.Field(23, "1234567777");
                    response.Add(orc);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    // ------------- Observation Request Segment ------------//
                    response = new Message();
                    Segment cti = new Segment("CTI");
                    cti.Field(1, testType);
                    cti.Field(2, "ABC^123");
                    response.Add(cti);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    frame.Append((char)0x1c);
                    frame.Append((char)0x0d);

                    mainframe.Append(frame);
                }

                return mainframe.ToString();
            }
            catch (Exception ex) 
            {
                return "";
            }            
        }
    }
}
