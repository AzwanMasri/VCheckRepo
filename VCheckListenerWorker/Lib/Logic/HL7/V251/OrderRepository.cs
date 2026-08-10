using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheck.Lib.Data.Models;
using VCheckListenerWorker.Lib.Object;

namespace VCheckListenerWorker.Lib.Logic.HL7.V251
{
    public class OrderRepository
    {
        public static string firstUIDpart = "";

        public async static Task<string> ProcessMessageAsync(string messageCount)
        {
            if(messageCount == "1") { return GenerateOMLO33Message2(); }
            else { return GenerateEndMarker(messageCount); }


            var order = "";
            List<(string, string)> testCodeName = new List<(string, string)>();
            

            var clinicID = TestResultRepository.GetConfigurationByKey("ClinicID");
            VCheckAPI vCheckAPI = new VCheckAPI();
            var schedulesString = await vCheckAPI.GetScheduleListNotSent(clinicID.ConfigurationValue);
            var schedulesExtended = string.IsNullOrEmpty(schedulesString) ? new List<ScheduledTestModelExtended>() : JsonConvert.DeserializeObject<List<ScheduledTestModelExtended>>(schedulesString);

            if (schedulesExtended.Count > 0)
            {
                schedulesExtended = schedulesExtended.Where(x => x.IDAnalyzers.Where(y => y.Analyzers.Split(", ").Contains("C1")).Count() != 0).ToList();

                var schedule = schedulesExtended.FirstOrDefault();

                if (schedule != null)
                {
                    var testID = schedule.IDAnalyzers.Where(x => x.Analyzers.Contains("C1") && !x.Analyzers.Contains("C10")).ToList();

                    foreach (var testInfo in testID)
                    {
                        var testResponseString = await vCheckAPI.GetTestByNameOrCode(null, testInfo.TestID);
                        var TestName = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testname.Replace(" (C10)", "");

                        testCodeName.Add((testInfo.TestID, TestName));
                    }

                    order = GenerateOMLO33Message(testCodeName, schedule.Schedule);

                    await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                    await vCheckAPI.UpdateScheduleAnalyzer("C1", schedule.Schedule.ScheduleUniqueID);
                }
                else
                {
                    order = GenerateEndMarker(messageCount);
                }

                //foreach (var schedule in schedulesExtended)
                //{
                //    testCodeName.Clear();
                //    var testID = schedule.IDAnalyzers.Where(x => x.Analyzers.Contains("C1") && !x.Analyzers.Contains("C10")).ToList();

                //    foreach (var testInfo in testID)
                //    {
                //        var testResponseString = await vCheckAPI.GetTestByNameOrCode(null, testInfo.TestID);
                //        var TestName = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testname.Replace(" (C10)", "");

                //        testCodeName.Add((testInfo.TestID, TestName));
                //    }

                //    order += GenerateOMLO33Message(testCodeName, schedule.Schedule);

                //    await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                //    await vCheckAPI.UpdateScheduleAnalyzer("C1", schedule.Schedule.ScheduleUniqueID);
                //}

                //order += GenerateEndMarker(messageCount);
            }
            else
            {
                order = GenerateEndMarker(messageCount);
            }

            return order;
        }

        public static String GenerateOMLO33Message(List<(string, string)> testCodeName, ScheduledTestModel info)
        {
            //string barcode = info.ScheduleUniqueID.Split("-")[3];
            DateTime now = DateTime.UtcNow;
            int unixTimestamp = (int)(now - new DateTime(1970, 1, 1)).TotalSeconds;
            firstUIDpart = "T" + unixTimestamp.ToString().Substring(2);

            var message = new HL7MessageModel();
            message.MSH = new MSHModel()
            {
                SendingApplication = "",
                SendingFacility = "LAB",
                ReceivingApplication = "VCHECK C10",
                ReceivingFacility = "",
                MessageType = "OML^O33^OML_O33",
                VersionID = "2.5.1",
                AcceptAckType = "NE",
                AppAckType = "AL",
                CharacterSet = "UNICODE UTF-8",
                MessageProfileIdentifier = "LAB-28^IHE"
            };

            var sLocation = TestResultRepository.GetConfigurationByKey("ClinicName");

            message.PID = new PIDModel()
            {
                PatientID = info.PatientID,
                Hospital = sLocation != null ? sLocation.ConfigurationValue : "LIS",
                PetName = info.PatientName,
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

            var SACUID = Guid.NewGuid().ToString().Split("-");
            message.SAC = new SACModel() { ContainerID = firstUIDpart + SACUID[1] };

            var ORCUID = Guid.NewGuid().ToString().Split("-");
            message.ORC = new ORCModel()
            {
                OrderControl = "NW",
                //PlacerOrderNo = Guid.NewGuid().ToString(),
                PlacerOrderNo = firstUIDpart + ORCUID[1],
                TransactionDatetime = DateTime.Now
            };

            message.OBR = new OBRModel()
            {
                PlacerOrderNo = firstUIDpart + ORCUID[1],
            };

            List<OBXModel> obx = new List<OBXModel>()
            {
                new OBXModel()
                {
                    SetID = "1", ValueType = "NM", ObservationIdentifier = "^Age", ObservationValue = "7777", Units = "Months", ObservationResultStatus = "F"
                },
                new OBXModel()
                {
                    SetID = "2", ValueType = "NM", ObservationIdentifier = "^Weight", ObservationValue = "8888", Units = "kg", ObservationResultStatus = "F"
                }
            };
            message.OBX = obx;


            var completeHL7 = "";

            foreach (var testInfo in testCodeName)
            {
                completeHL7 += GenerateMessage(message, testInfo.Item1, testInfo.Item2);
            }

            return completeHL7;
        }

        public static String GenerateOMLO33Message2()
        {
            //string barcode = info.ScheduleUniqueID.Split("-")[3];
            DateTime now = DateTime.UtcNow;
            int unixTimestamp = (int)(now - new DateTime(1970, 1, 1)).TotalSeconds;
            firstUIDpart = "T" + unixTimestamp.ToString().Substring(2);

            var message = new HL7MessageModel();
            message.MSH = new MSHModel()
            {
                SendingApplication = "",
                SendingFacility = "LAB",
                ReceivingApplication = "VCHECK C10",
                ReceivingFacility = "",
                MessageType = "OML^O33^OML_O33",
                VersionID = "2.5.1",
                AcceptAckType = "NE",
                AppAckType = "AL",
                CharacterSet = "UNICODE UTF-8",
                MessageProfileIdentifier = "LAB-28^IHE"
            };

            var sLocation = TestResultRepository.GetConfigurationByKey("ClinicName");

            message.PID = new PIDModel()
            {
                PatientID = "1111",
                Hospital = "2222",
                PetName = "3333",
                Gender = "Male",
                OwnerName = "4444",
                Species = "Canine",
                Breed = "5555"
            };

            message.PV1 = new PV1Model()
            {
                PatientClass = "E",
                Room = "^3001",
                DoctorName = "6666"
            };

            message.SPM = new SPMModel()
            {
                SetID = "1",
                SpecimenType = "Serum",
                SpecimentRole = "P^Patient^HL70369"
            };

            var SACUID = Guid.NewGuid().ToString().Split("-");
            message.SAC = new SACModel() { ContainerID = firstUIDpart + SACUID[1] };

            var ORCUID = Guid.NewGuid().ToString().Split("-");
            message.ORC = new ORCModel()
            {
                OrderControl = "NW",
                //PlacerOrderNo = Guid.NewGuid().ToString(),
                PlacerOrderNo = firstUIDpart + ORCUID[1],
                TransactionDatetime = DateTime.Now
            };

            message.OBR = new OBRModel()
            {
                PlacerOrderNo = firstUIDpart + ORCUID[1],
            };

            List<OBXModel> obx = new List<OBXModel>()
            {
                new OBXModel()
                {
                    SetID = "1", ValueType = "NM", ObservationIdentifier = "^Age", ObservationValue = "7777", Units = "Months", ObservationResultStatus = "F"
                },
                new OBXModel()
                {
                    SetID = "2", ValueType = "NM", ObservationIdentifier = "^Weight", ObservationValue = "8888", Units = "kg", ObservationResultStatus = "F"
                }
            };
            message.OBX = obx;


            var completeHL7 = GenerateMessage(message, "BNCA001", "Rotor Comprehensive 28");

            return completeHL7;
        }

        public static String GenerateMessage(HL7MessageModel message, string CartridgeID, string CartridgeName)
        {
            StringBuilder frame = new StringBuilder();
            frame.Append((char)0x0b);
            var messageUID = Guid.NewGuid().ToString().Split("-");

            // ------------- Message Header ------------//
            Message response = new Message();
            Segment msh = new Segment("MSH");
            msh.Field(1, "|");
            msh.Field(2, "^~\\&");
            msh.Field(3, message.MSH.SendingApplication);
            msh.Field(4, message.MSH.SendingFacility);
            msh.Field(5, message.MSH.ReceivingApplication);
            msh.Field(6, message.MSH.ReceivingFacility);
            msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmss"));
            msh.Field(9, message.MSH.MessageType);
            msh.Field(10, firstUIDpart + messageUID[1]);
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
            pid.Field(1, "1");
            pid.Field(3, message.PID.PatientID + "^^^^^" + message.PID.Hospital);
            pid.Field(5, message.PID.PetName + "^^^^^^L");
            pid.Field(8, message.PID.Gender);
            pid.Field(21, message.PID.OwnerName);
            pid.Field(36, message.PID.Species);
            pid.Field(37, message.PID.Breed);
            //pid.Field(3, "1111^^^^^2222");
            //pid.Field(5, "3333^^^^^^L");
            //pid.Field(8, "Male");
            //pid.Field(21, "4444");
            //pid.Field(36, "Canine");
            //pid.Field(37, "5555");
            response.Add(pid);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Patient Visit Segment ------------//
            response = new Message();
            Segment pv1 = new Segment("PV1");
            pv1.Field(2, message.PV1.PatientClass); //optional
            pv1.Field(3, message.PV1.Room); //optional
            pv1.Field(7, message.PV1.DoctorName);
            //pv1.Field(7, "6666");
            response.Add(pv1);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Speciment Segment ------------//
            response = new Message();
            Segment spm = new Segment("SPM");
            spm.Field(1, "1");
            //spm.Field(4, "Serum^Respiratory^HL70487");
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
            orc.Field(4, "12345^IHE_OM_OP^1.3.6.1.4.1.12559.11.1.2.2.4.4^ISO");
            //orc.Field(5, "IP");
            orc.Field(9, message.ORC.TransactionDatetime.ToString("yyyyMMddhhmmsszzz").Replace(":", ""));
            response.Add(orc);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Observation Request Segment ------------//
            response = new Message();
            Segment obr = new Segment("OBR");
            obr.Field(1, "1"); //optional
            obr.Field(2, message.ORC.PlacerOrderNo); //optional
            obr.Field(4, CartridgeID + "BNCA001^" + CartridgeName + "^VCHECK");
            //obr.Field(4, "BNCA001^Rotor Comprehensive 28^VCHECK");
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

        public static String GenerateMessage2(HL7MessageModel message, string CartridgeID, string CartridgeName)
        {
            StringBuilder frame = new StringBuilder();
            frame.Append((char)0x0b);
            var messageUID = Guid.NewGuid().ToString().Split("-");

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
            msh.Field(10, firstUIDpart + messageUID[1]);
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
            //pid.Field(1, "1");
            //pid.Field(3, message.PID.PatientID + "^^^^^" + message.PID.Hospital);
            //pid.Field(5, message.PID.PetName + "^^^^^^L");
            //pid.Field(8, message.PID.Gender);
            //pid.Field(21, message.PID.OwnerName);
            //pid.Field(36, message.PID.Species);
            //pid.Field(37, message.PID.Breed);
            pid.Field(3, "1111^^^^^2222");
            pid.Field(5, "3333^^^^^^L");
            pid.Field(8, "Male");
            pid.Field(21, "4444");
            pid.Field(36, "Canine");
            pid.Field(37, "5555");
            response.Add(pid);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Patient Visit Segment ------------//
            response = new Message();
            Segment pv1 = new Segment("PV1");
            pv1.Field(2, message.PV1.PatientClass); //optional
            pv1.Field(3, message.PV1.Room); //optional
            //pv1.Field(7, message.PV1.DoctorName);
            pv1.Field(7, "6666");
            response.Add(pv1);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Speciment Segment ------------//
            response = new Message();
            Segment spm = new Segment("SPM");
            spm.Field(1, "1");
            //spm.Field(4, "Serum^Respiratory^HL70487");
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
            orc.Field(4, "12345^IHE_OM_OP^1.3.6.1.4.1.12559.11.1.2.2.4.4^ISO");
            //orc.Field(5, "IP");
            orc.Field(9, message.ORC.TransactionDatetime.ToString("yyyyMMddHHmmsszzz").Replace(":", ""));
            response.Add(orc);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Observation Request Segment ------------//
            response = new Message();
            Segment obr = new Segment("OBR");
            //obr.Field(1, "1"); //optional
            obr.Field(2, message.ORC.PlacerOrderNo); //optional
            //obr.Field(4, CartridgeID + "^" + CartridgeName + "^VCHECK");
            obr.Field(4, "BNCA001^Rotor Comprehensive 28^VCHECK");
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

        public static String GenerateEndMarker(string messageCount)
        {
            StringBuilder frame = new StringBuilder();
            frame.Append((char)0x0b);

            var messageUID = Guid.NewGuid().ToString().Split("-");
            try
            {
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "");
                msh.Field(4, "LAB");
                msh.Field(5, "VCHECK C10");
                msh.Field(6, "");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmss"));
                msh.Field(9, "OML^O33^OML_O33");
                msh.Field(10, messageUID[messageUID.Length - 1]);
                msh.Field(11, "P");
                msh.Field(12, "2.5.1");
                msh.Field(15, "NE"); //optional
                msh.Field(16, "AL"); //optional
                msh.Field(18, "UNICODE UTF-8");
                msh.Field(21, "LAB-28^IHE"); //optional
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, messageCount);
                msa.Field(6, "0");
                msa.Field(7, "");
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                return frame.ToString();
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }
    }
}
