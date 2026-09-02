using Newtonsoft.Json;
using NHapi.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheck.Lib.Data.Models;
using VCheckListenerWorker.Lib.Object;

namespace VCheckListenerWorker.Lib.Logic.HL7.V26
{
    public class OrderRepository
    {
        public static VCheckAPI vCheckAPI = new VCheckAPI();

        public async static Task<string> ProcessMessageAsync(string[] HL7)
        {
            var order = "";
            List<(string, string)> testCodeName = new List<(string, string)>();
            var mshLine = HL7.First(x => x.StartsWith("MSH"));
            var qpdLine = HL7.First(x => x.StartsWith("QPD"));

            var qpdLineArray = qpdLine.Split('|');
            var mshLineArray = mshLine.Split('|');


            var queryId = qpdLineArray[2];
            var messageID = mshLineArray[9].Replace("{", "").Replace("}", "");

            //return await GenerateMultiOrderMessageRSPDummy(queryId, messageID);

            var clinicID = TestResultRepository.GetConfigurationByKey("ClinicID");
            var schedulesString = await vCheckAPI.GetScheduleListNotSent(clinicID.ConfigurationValue);
            var schedulesExtended = string.IsNullOrEmpty(schedulesString) ? new List<ScheduledTestModelExtended>() : JsonConvert.DeserializeObject<List<ScheduledTestModelExtended>>(schedulesString);

            if (schedulesExtended.Count > 0)
            {
                schedulesExtended = schedulesExtended.Where(x => x.IDAnalyzers.Where(y => y.Analyzers.Split(", ").Contains("V200")).Count() != 0).ToList();

                //order = GenerateOrderMessageRSP(schedulesExtended, queryId);

                //foreach (var schedule in schedulesExtended)
                //{
                //    await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                //    await vCheckAPI.UpdateScheduleAnalyzer("V200", schedule.Schedule.ScheduleUniqueID);
                //}

                //foreach (var schedule in schedulesExtended)
                //{
                //    testCodeName.Clear();
                //    var testID = schedule.IDAnalyzers.Where(x => x.Analyzers.Contains("V200")).ToList();

                //    foreach (var testInfo in testID)
                //    {
                //        var testResponseString = await vCheckAPI.GetTestByNameOrCode(null, testInfo.TestID);
                //        var TestName = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testname.Replace(" (C10)", "");

                //        testCodeName.Add((testInfo.TestID, TestName));
                //    }

                //    order += GenerateOrderMessageRSP(testCodeName, queryId, schedule);

                //await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                //await vCheckAPI.UpdateScheduleAnalyzer("V200", schedule.Schedule.ScheduleUniqueID);
                //}

                order = await GenerateMultiOrderMessageRSP(queryId, messageID, schedulesExtended);
            }
            else
            {
                order = GenerateNoOrderMessageRSP(queryId);
            }

            return order;
        }

        public static string GenerateOrderMessageRSP(List<(string, string)> testCodeName, string sControlID, ScheduledTestModelExtended schedule)
        {
            string barcode = schedule.Schedule.ScheduleUniqueID.Split("-")[3];

            try
            {
                StringBuilder mainframe = new StringBuilder();

                string testType = schedule.Schedule.ScheduledTestType;
                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0B);
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "VCheck");
                msh.Field(5, "V200");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "RSP^Z02^RSP_Z02");
                msh.Field(10, "{" + Guid.NewGuid().ToString() + "}");
                msh.Field(11, "P");
                msh.Field(12, "2.6");
                msh.Field(15, "NE");
                msh.Field(16, "NE");
                msh.Field(18, "UNICODE UTF-8");
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement Segment ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, sControlID);
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query Acknowledgement Segment ---------------------//
                response = new Message();
                Segment qak = new Segment("QAK");
                qak.Field(1, sControlID);
                qak.Field(2, "OK");
                qak.Field(3, "Z01^Query Orders");
                response.Add(qak);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query Parameter Definition Segment ---------------------//
                response = new Message();
                Segment qpd = new Segment("QPD");
                qpd.Field(1, "Z01^Query Orders");
                qpd.Field(2, sControlID);
                qpd.Field(3, "ALL");
                response.Add(qpd);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Patient Identification Segment ------------//
                response = new Message();
                Segment pid = new Segment("PID");
                pid.Field(1, "1");
                pid.Field(3, schedule.Schedule.PatientID);
                response.Add(pid);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                foreach (var testCode in testCodeName)
                {
                    // ------------- Common Order Segment ------------//
                    response = new Message();
                    Segment orc = new Segment("ORC");
                    orc.Field(1, "NW");
                    orc.Field(2, barcode);
                    orc.Field(8, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    response.Add(orc);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    // ------------- Observation Request Segment ------------//
                    response = new Message();
                    Segment obr = new Segment("OBR");
                    obr.Field(1, "1");
                    obr.Field(2, barcode);
                    obr.Field(4, testCode.Item1 + "^" + testCode.Item2);
                    response.Add(obr);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    // ------------- Specimen Segment ------------//
                    response = new Message();
                    Segment spm = new Segment("SPM");
                    spm.Field(1, "1");
                    spm.Field(2, "S12345678");
                    spm.Field(4, "Serum/Plasma");
                    spm.Field(11, "P");
                    response.Add(spm);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);
                }

                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                mainframe.Append(frame);

                return mainframe.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static async Task<string> GenerateMultiOrderMessageRSP(string sControlID, string MessageControlID, List<ScheduledTestModelExtended> schedules)
        {
            List<(string, string)> testCodeName = new List<(string, string)>();
            DateTime now = DateTime.UtcNow;
            int unixTimestamp = (int)(now - new DateTime(1970, 1, 1)).TotalSeconds;
            var firstUIDpart = "T" + unixTimestamp.ToString().Substring(2);

            var SACUID = Guid.NewGuid().ToString().Split("-");


            try
            {
                StringBuilder mainframe = new StringBuilder();
                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0B);
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "VCheck");
                msh.Field(5, "V200");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmss"));
                msh.Field(9, "RSP^Z02^RSP_Z02");
                msh.Field(10, "{" + Guid.NewGuid().ToString() + "}");
                msh.Field(11, "P");
                msh.Field(12, "2.6");
                msh.Field(15, "NE");
                msh.Field(16, "NE");
                msh.Field(18, "UNICODE UTF-8");
                msh.Field(19, "");
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement Segment ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, MessageControlID);
                msa.Field(3, "");
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query Acknowledgement Segment ---------------------//
                response = new Message();
                Segment qak = new Segment("QAK");
                qak.Field(1, sControlID);
                qak.Field(2, "OK");
                qak.Field(3, "Z01^Query Orders");
                qak.Field(4, "");
                response.Add(qak);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query Parameter Definition Segment ---------------------//
                response = new Message();
                Segment qpd = new Segment("QPD");
                qpd.Field(1, "Z01^Query Orders");
                qpd.Field(2, sControlID);
                qpd.Field(3, "ALL");
                qpd.Field(4, "");
                response.Add(qpd);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                int count = 0;

                foreach (var schedule in schedules)
                {
                    count++;
                    //string barcode = schedule.Schedule.ScheduleUniqueID.Split("-")[3];
                    string barcode = firstUIDpart + SACUID[1];
                    string testType = schedule.Schedule.ScheduledTestType;

                    testCodeName.Clear();
                    var testID = schedule.IDAnalyzers.Where(x => x.Analyzers.Contains("V200")).ToList();

                    foreach (var testInfo in testID)
                    {
                        var testResponseString = await vCheckAPI.GetTestByNameOrCode(null, testInfo.TestID);
                        var TestName = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testname.Replace(" (C10)", "");

                        testCodeName.Add((testInfo.TestID, TestName));
                    }

                    // ------------- Patient Identification Segment ------------//
                    response = new Message();
                    Segment pid = new Segment("PID");
                    pid.Field(1, count.ToString());
                    pid.Field(3, schedule.Schedule.PatientID);
                    pid.Field(4, "");
                    response.Add(pid);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);

                    foreach (var testCode in testCodeName)
                    {
                        // ------------- Common Order Segment ------------//
                        response = new Message();
                        Segment orc = new Segment("ORC");
                        orc.Field(1, "NW");
                        orc.Field(2, barcode);
                        orc.Field(8, DateTime.Now.ToString("yyyyMMddhhmmss"));
                        orc.Field(19, "");
                        response.Add(orc);
                        frame.Append(response.SerializeMessage());
                        frame.Append((char)0x0d);

                        // ------------- Observation Request Segment ------------//
                        response = new Message();
                        Segment obr = new Segment("OBR");
                        obr.Field(1, "1");
                        obr.Field(2, barcode);
                        obr.Field(4, testCode.Item1 + "^" + testCode.Item2 + "^VCHECK");
                        obr.Field(5, "");
                        response.Add(obr);
                        frame.Append(response.SerializeMessage());
                        frame.Append((char)0x0d);

                        // ------------- Specimen Segment ------------//
                        response = new Message();
                        Segment spm = new Segment("SPM");
                        spm.Field(1, "1");
                        spm.Field(2, "S12345678");
                        spm.Field(4, "Serum/Plasma");
                        spm.Field(11, "P");
                        spm.Field(12, "");
                        response.Add(spm);
                        frame.Append(response.SerializeMessage());
                        frame.Append((char)0x0d);
                    }

                    await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                    await vCheckAPI.UpdateScheduleAnalyzer("V200", schedule.Schedule.ScheduleUniqueID);
                }                

                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                mainframe.Append(frame);

                return mainframe.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static async Task<string> GenerateMultiOrderMessageRSPDummy(string sControlID, string MessageControlID)
        {
            List<(string, string)> testCodeName = new List<(string, string)>();

            DateTime now = DateTime.UtcNow;
            int unixTimestamp = (int)(now - new DateTime(1970, 1, 1)).TotalSeconds;
            string firstUIDpart = "T" + unixTimestamp.ToString().Substring(2);

            try
            {
                StringBuilder mainframe = new StringBuilder();
                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0B);
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "VCheck");
                msh.Field(5, "V200");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmss"));
                msh.Field(9, "RSP^Z02^RSP_Z02");
                msh.Field(10, "{" + Guid.NewGuid().ToString() + "}");
                msh.Field(11, "P");
                msh.Field(12, "2.6");
                msh.Field(15, "NE");
                msh.Field(16, "NE");
                msh.Field(18, "UNICODE UTF-8");
                msh.Field(19, "");
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement Segment ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, MessageControlID);
                msa.Field(3, "");
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query Acknowledgement Segment ---------------------//
                response = new Message();
                Segment qak = new Segment("QAK");
                qak.Field(1, sControlID);
                qak.Field(2, "OK");
                qak.Field(3, "Z01^Query Orders");
                qak.Field(4, "");
                response.Add(qak);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query Parameter Definition Segment ---------------------//
                response = new Message();
                Segment qpd = new Segment("QPD");
                qpd.Field(1, "Z01^Query Orders");
                qpd.Field(2, sControlID);
                qpd.Field(3, "ALL");
                qpd.Field(4, "");
                response.Add(qpd);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                var PIDUID = Guid.NewGuid().ToString().Split("-");
                // ------------- Patient Identification Segment ------------//
                response = new Message();
                Segment pid = new Segment("PID");
                pid.Field(1, "1");
                pid.Field(3, PIDUID[1] + "1111");
                pid.Field(4, "");
                response.Add(pid);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                var ORCUID = Guid.NewGuid().ToString().Split("-");
                // ------------- Common Order Segment ------------//
                response = new Message();
                Segment orc = new Segment("ORC");
                orc.Field(1, "NW");
                orc.Field(2, firstUIDpart + ORCUID[1]);
                orc.Field(8, DateTime.Now.ToString("yyyyMMddhhmmss"));
                orc.Field(19, "");
                response.Add(orc);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Observation Request Segment ------------//
                response = new Message();
                Segment obr = new Segment("OBR");
                obr.Field(1, "1");
                obr.Field(2, firstUIDpart + ORCUID[1]);
                obr.Field(4, "BN0030^cCortisol^VCHECK");
                obr.Field(5, "");
                response.Add(obr);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Specimen Segment ------------//
                response = new Message();
                Segment spm = new Segment("SPM");
                spm.Field(1, "1");
                spm.Field(2, "S12345678");
                spm.Field(4, "Serum/Plasma");
                spm.Field(11, "P");
                spm.Field(12, "");
                response.Add(spm);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                mainframe.Append(frame);

                return mainframe.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static string GenerateNoOrderMessageRSP(string sControlID)
        {
            StringBuilder mainframe = new StringBuilder();

            StringBuilder frame = new StringBuilder();
            frame.Append((char)0x0B);
            Message response = new Message();

            // ------------- Message Header ------------//
            Segment msh = new Segment("MSH");
            msh.Field(1, "|");
            msh.Field(2, "^~\\&");
            msh.Field(3, "LIS Host");
            msh.Field(5, "V200 LIS Simulator");
            msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmss"));
            msh.Field(9, "RSP^Z02^RSP_Z02");
            msh.Field(10, "{" + Guid.NewGuid().ToString() + "}");
            msh.Field(11, "P");
            msh.Field(12, "2.6");
            msh.Field(15, "NE");
            msh.Field(16, "NE");
            msh.Field(18, "UNICODE UTF-8");
            response.Add(msh);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Message Acknowledgement Segment ---------------------//
            response = new Message();
            Segment msa = new Segment("MSA");
            msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
            msa.Field(2, sControlID);
            response.Add(msa);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Query Acknowledgement Segment ---------------------//
            response = new Message();
            Segment qak = new Segment("QAK");
            qak.Field(1, sControlID);
            qak.Field(2, "NF");
            response.Add(qak);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Query Parameter Definition Segment ---------------------//
            response = new Message();
            Segment qpd = new Segment("QPD");
            qpd.Field(2, sControlID);
            response.Add(qpd);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Patient Identification Segment ------------//
            response = new Message();
            Segment pid = new Segment("PID");
            pid.Field(3, "0");
            response.Add(pid);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Common Order Segment ------------//
            response = new Message();
            Segment orc = new Segment("ORC");
            orc.Field(2, "O38296401");
            response.Add(orc);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            // ------------- Observation Request Segment ------------//
            response = new Message();
            Segment obr = new Segment("OBR");
            obr.Field(4, "None");
            response.Add(obr);
            frame.Append(response.SerializeMessage());
            frame.Append((char)0x0d);

            frame.Append((char)0x1c);
            frame.Append((char)0x0d);

            mainframe.Append(frame);

            return mainframe.ToString();
        }
    }
}
