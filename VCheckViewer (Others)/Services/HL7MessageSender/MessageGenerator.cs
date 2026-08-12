using Newtonsoft.Json;
using System.Text;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;
using VCheckViewerAPI.Lib.Object;
using Message = VCheckViewerAPI.Lib.Object.Message;

namespace VCheckViewer_Others.Services.HL7MessageSender
{
    public class MessageGenerator
    {
        public static ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());
        public static int MessageType = 0;

        public static String GenerateOMLO33Message(List<(string, string)> testCodeName, ScheduledTestModel info)
        {
            MessageType = 1;
            var message = new HL7MessageModel();
            message.MSH = new MSHModel()
            {
                SendingApplication = "LIS",
                SendingFacility = "",
                ReceivingApplication = "VCHECK C10",
                ReceivingFacility = "LAB",
                MessageType = "OML^O33^OML_O33",
                VersionID = "2.5.1",
                AcceptAckType = "NE",
                AppAckType = "AL",
                CharacterSet = "UNICODE UTF-8",
                MessageProfileIdentifier = "LAB-28^IHE"
            };

            
            var sLocation = configDBContext.GetConfigurationData("ClinicName").FirstOrDefault();

            message.PID = new PIDModel()
            {
                PatientID = info.PatientID,
                Hospital = sLocation.ConfigurationValue,
                PetName = info.PatientName,
                BirthDate = DateTime.Parse("2024-01-01"),
                Gender = info.Gender,
                OwnerName = info.OwnerName,
                Species = info.Species,
                Breed = "NA"
            };

            message.PV1 = new PV1Model()
            {
                PatientClass = "E",
                Room = "^3001",
                DoctorName = info.InchargePerson
            };

            message.SPM = new SPMModel()
            {
                SetID = "1",
                SpecimenType = "Serum",
                SpecimentRole = "P^Patient^HL70369"
            };

            message.SAC = new SACModel() { ContainerID = "g55643" };

            message.ORC = new ORCModel()
            {
                OrderControl = "NW",
                PlacerOrderNo = Guid.NewGuid().ToString(),
                PlacerGroupNo = Guid.NewGuid().ToString(),
                TransactionDatetime = DateTime.Now
            };

            //string CartridgeID = "DC001B";
            //string CartridgeName = "Comprehensive 17";
            //string CartridgeID = testIDAnalyzers.FirstOrDefault().TestID;
            //string CartridgeName = info.ScheduledTestType;

            message.OBR = new OBRModel()
            {
                PlacerOrderNo = Guid.NewGuid().ToString(),
                //UniversalServiceID = CartridgeID + "^" + CartridgeName + "^VCHECK"
            };

            List<OBXModel> obx = new List<OBXModel>()
            {
                new OBXModel()
                {
                    SetID = "1", ValueType = "NM", ObservationIdentifier = "^Age", ObservationValue = "24", Units = "Months", ObservationResultStatus = "F"
                },
                new OBXModel()
                {
                    SetID = "2", ValueType = "NM", ObservationIdentifier = "^Weight", ObservationValue = "5.4", Units = "kg", ObservationResultStatus = "F"
                }
            };
            message.OBX = obx;


            List<NTEModel> nte = new List<NTEModel>()
            {
                new NTEModel()
                {
                    SetID = "1", Comment = "24^Months", CommentType = "Age"
                },
                new NTEModel()
                {
                    SetID = "2", Comment = "5.4^Kg", CommentType = "Weight"
                }
            };
            message.NTE = nte;

            var completeHL7 = "";

            foreach(var testInfo in testCodeName)
            {
                completeHL7 += "\n\n" + GenerateMessage(message, testInfo.Item1, testInfo.Item2);
            }

            //return GenerateMessage(message);
            return completeHL7;
        }

        public static String GenerateORMO01Message(List<TestIDAnalyzers> testIDAnalyzers, ScheduledTestModel info)
        {
            MessageType = 2;
            var message = new HL7MessageModel();
            message.MSH = new MSHModel()
            {
                SendingApplication = "LIS",
                SendingFacility = "BIONOTE",
                ReceivingApplication = "VCHECK H6",
                ReceivingFacility = "LAB",
                MessageType = "ORM^O01",
                VersionID = "2.3.1",
                AcceptAckType = "NE",
                AppAckType = "AL",
                CharacterSet = "UTF-8",
                //MessageProfileIdentifier = "LAB-28^IHE"
            };


            var sLocation = configDBContext.GetConfigurationData("ClinicName").FirstOrDefault();
            var gender = info.Gender == "Male" ? "M" : "F";
            message.PID = new PIDModel()
            {
                PatientID = info.PatientID,
                Hospital = sLocation.ConfigurationValue,
                PetName = info.PatientName,
                BirthDate = DateTime.Parse("2024-01-01"),
                Gender = gender,
                OwnerName = info.OwnerName,
                Species = info.Species,
                Breed = "NA"
            };

            message.ORC = new ORCModel()
            {
                OrderControl = "NW",
                PlacerOrderNo = "1",
                PlacerGroupNo = "123456789",
                TransactionDatetime = DateTime.Now
            };

            //string CartridgeID = "DC001B";
            //string CartridgeName = "Comprehensive 17";
            string CartridgeID = testIDAnalyzers.FirstOrDefault().TestID;
            string CartridgeName = info.ScheduledTestType;

            message.OBR = new OBRModel()
            {
                PlacerOrderNo = Guid.NewGuid().ToString(),
                UniversalServiceID = CartridgeID + "^" + CartridgeName + "^VCHECK"
            };

            List<OBXModel> obx = new List<OBXModel>()
            {
                new OBXModel()
                {
                    SetID = "1", ValueType = "NM", ObservationIdentifier = "^Age", ObservationValue = "24", Units = "Months", ObservationResultStatus = "F"
                },
                new OBXModel()
                {
                    SetID = "2", ValueType = "NM", ObservationIdentifier = "^Weight", ObservationValue = "5.4", Units = "kg", ObservationResultStatus = "F"
                }
            };
            message.OBX = obx;


            List<NTEModel> nte = new List<NTEModel>()
            {
                new NTEModel()
                {
                    SetID = "1", Comment = "24^Months", CommentType = "Age"
                },
                new NTEModel()
                {
                    SetID = "2", Comment = "5.4^Kg", CommentType = "Weight"
                }
            };
            message.NTE = nte;

            return GenerateMessageORM(message);
        }

        public static String GenerateMessage(HL7MessageModel message, string CartridgeID, string CartridgeName)
        {
            StringBuilder frame = new StringBuilder();
            frame.Append((char)0x0b);

            // ------------- Message Header ------------//
            Message response = new Message();
            Segment msh = new Segment("MSH");
            msh.Field(1, "|");
            msh.Field(2, "^~\\&");
            msh.Field(3, message.MSH.SendingApplication);
            msh.Field(4, message.MSH.SendingFacility);
            msh.Field(5, message.MSH.ReceivingApplication);
            msh.Field(6, message.MSH.ReceivingFacility);
            msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
            msh.Field(9, message.MSH.MessageType);
            msh.Field(10, Guid.NewGuid().ToString());
            msh.Field(11, "P");
            msh.Field(12, message.MSH.VersionID);
            msh.Field(15, message.MSH.AcceptAckType); //optional
            msh.Field(16, message.MSH.AppAckType); //optional
            msh.Field(18, message.MSH.CharacterSet);
            msh.Field(21, message.MSH.MessageProfileIdentifier); //optional
            response.Add(msh);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Patient Identification Segment ------------//
            response = new Message();
            Segment pid = new Segment("PID");
            pid.Field(3, message.PID.PatientID + "^^^^^" + message.PID.Hospital);
            pid.Field(5, message.PID.PetName);
            pid.Field(7, message.PID.BirthDate.ToString("yyyyMMdd"));
            pid.Field(8, message.PID.Gender);
            pid.Field(21, message.PID.OwnerName);
            pid.Field(35, message.PID.Species);
            pid.Field(36, message.PID.Breed);
            response.Add(pid);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Patient Visit Segment ------------//
            response = new Message();
            Segment pv1 = new Segment("PV1");
            pv1.Field(2, message.PV1.PatientClass); //optional
            pv1.Field(3, message.PV1.Room); //optional
            pv1.Field(7, message.PV1.DoctorName);
            response.Add(pv1);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Speciment Segment ------------//
            response = new Message();
            Segment spm = new Segment("SPM");
            spm.Field(1, message.SPM.SetID);
            spm.Field(4, message.SPM.SpecimenType);
            spm.Field(11, message.SPM.SpecimentRole); //optional
            response.Add(spm);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Speciment Container Detail Segment ------------//
            response = new Message();
            Segment sac = new Segment("SAC");
            sac.Field(3, message.SAC.ContainerID);
            response.Add(sac);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Common Order Segment ------------//
            response = new Message();
            Segment orc = new Segment("ORC");
            orc.Field(1, message.ORC.OrderControl);
            orc.Field(2, message.ORC.PlacerOrderNo);
            orc.Field(4, message.ORC.PlacerGroupNo);
            orc.Field(9, message.ORC.TransactionDatetime.ToString("yyyyMMddHHmmss"));
            response.Add(orc);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Observation Request Segment ------------//
            response = new Message();
            Segment obr = new Segment("OBR");
            obr.Field(2, message.OBR.PlacerOrderNo); //optional
            //obr.Field(4, message.OBR.UniversalServiceID);
            obr.Field(4, CartridgeID + "^" + CartridgeName + "^VCHECK");
            response.Add(obr);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            foreach (var obxValue in message.OBX)
            {
                // ------------- Observation Result Segment ------------//
                response = new Message();
                Segment obx = new Segment("OBX");
                obx.Field(1, obxValue.SetID);
                obx.Field(2, obxValue.ValueType);
                obx.Field(3, obxValue.ObservationIdentifier);
                obx.Field(5, obxValue.ObservationValue);
                obx.Field(6, obxValue.Units);
                obx.Field(11, obxValue.ObservationResultStatus);
                obx.Field(29, "SCI");
                response.Add(obx);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);
            }

            frame.Append((char)0x1c);
            frame.Append((char)0x0d);

            return frame.ToString();
        }

        public static String GenerateMessageORM(HL7MessageModel message)
        {
            StringBuilder frame = new StringBuilder();
            frame.Append((char)0x0b);

            // ------------- Message Header ------------//
            Message response = new Message();
            Segment msh = new Segment("MSH");
            msh.Field(1, "|");
            msh.Field(2, "^~\\&");
            msh.Field(3, message.MSH.SendingApplication);
            msh.Field(4, message.MSH.SendingFacility);
            msh.Field(5, message.MSH.ReceivingApplication);
            msh.Field(6, message.MSH.ReceivingFacility);
            msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
            msh.Field(9, message.MSH.MessageType);
            msh.Field(10, "9");
            msh.Field(11, "P");
            msh.Field(18, message.MSH.CharacterSet);
            response.Add(msh);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Patient Identification Segment ------------//
            response = new Message();
            Segment pid = new Segment("PID");
            pid.Field(1, "1");
            pid.Field(2, "1001");
            pid.Field(3, "1901");
            pid.Field(4, "01");
            pid.Field(5, message.PID.PetName);
            pid.Field(6, "Kevin");
            pid.Field(7, message.PID.BirthDate.ToString("yyyyMMddHHmmss"));
            pid.Field(8, "M");
            pid.Field(9, "O");
            pid.Field(11, "GUILIN");
            pid.Field(12, "541000");
            pid.Field(13, "12345678900");
            pid.Field(14, "18");
            pid.Field(15, "Y");
            pid.Field(18, "breed");
            pid.Field(19, "hospital");
            pid.Field(23, "0");
            pid.Field(26, "Veter");
            pid.Field(27, "CHINA");
            pid.Field(28, "2");
            response.Add(pid);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Common Order Segment ------------//
            response = new Message();
            Segment orc = new Segment("ORC");
            orc.Field(1, message.ORC.OrderControl);
            orc.Field(2, message.ORC.PlacerOrderNo);
            orc.Field(3, "123456789");
            orc.Field(4, "Barcode");
            orc.Field(11, "Blood");
            orc.Field(15, DateTime.Now.ToString("yyyyMMddHHmmss"));
            orc.Field(17, "Department");
            orc.Field(21, "OrderingProvider");
            orc.Field(22, "1234567777");
            response.Add(orc);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Observation Request Segment ------------//
            response = new Message();
            Segment cti = new Segment("CTI");
            cti.Field(1, "Blood^CBC^DIFF");
            cti.Field(2, "ABC^1");
            response.Add(cti);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            frame.Append((char)0x1c);
            frame.Append((char)0x0d);

            return frame.ToString();
        }
    }
}
