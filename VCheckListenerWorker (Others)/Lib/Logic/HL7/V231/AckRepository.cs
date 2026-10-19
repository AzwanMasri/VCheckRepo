using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using NHapi.Model.V231.Datatype;
using NHapi.Model.V231.Segment;
using NHapiTools.Model.V231.Segment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheck.Lib.Data;
using VCheckListenerWorker_Others.Lib.Models;
using VCheckListenerWorker_Others.Lib.Object;

namespace VCheckListenerWorker_Others.Lib.Logic.HL7.V231
{
    public class AckRepository
    {
        public static class Species
        {
            public static string Category { get; set; }
            public static string SubCategory => Category switch
            {
                "Canine" => "0",
                "Feline" => "1",
                "Cust1" => "21",
                "Cust2" => "22",
                _ => "22"
            };

        }
        /// <summary>
        /// Generate Ack Message for ORU_R01
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <returns></returns>
        public static String GenerateAcknowlegeMessageORU(NHapi.Base.Model.IMessage sIMessage)
        {
            try
            {
                NHapi.Model.V231.Message.ORU_R01 sORU_R01 = (NHapi.Model.V231.Message.ORU_R01)sIMessage;

                //var SoftwareVersion = (NHapi.Base.Model.Varies)sORU_R01.MSH.GetField(21)[0];
                //var CharacterSet = sORU_R01.MSH.GetField(18)[0].ToString();

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0B);
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, sORU_R01.MSH.FieldSeparator.Value);
                msh.Field(2, sORU_R01.MSH.EncodingCharacters.Value);
                msh.Field(3, "VCheck Listener");
                msh.Field(4, "LIS");
                msh.Field(5, sORU_R01.MSH.SendingApplication.NamespaceID.Value);
                msh.Field(6, sORU_R01.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "ACK^R01");
                msh.Field(10, sORU_R01.MSH.MessageControlID.Value.ToString());
                msh.Field(11, sORU_R01.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sORU_R01.MSH.VersionID.VersionID.Value);
                msh.Field(16, "0");
                msh.Field(18, "UTF-8");
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement (No error) ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, sORU_R01.MSH.MessageControlID.Value.ToString());
                msa.Field(6, "0");
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                //// ------------- Message Acknowledgement (error) ---------------------//
                //response = new Message();
                //Segment msa = new Segment("MSA");
                //msa.Field(1, NHapi.Base.AcknowledgmentCode.AE.ToString());
                //msa.Field(2, sORU_R01.MSH.MessageControlID.Value.ToString());
                //msa.Field(3, "Error message");
                //msa.Field(6, "100");
                //response.Add(msa);
                //frame.Append(response.SerializeMessage());
                //frame.Append((char)0x0d);

                frame.Append((char)0x1C);
                frame.Append((char)0x0D);

                var ackMessage = frame.ToString();

                return ackMessage;

            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }

        public async Task<String> GenerateAcknowlegeMessageQRY(NHapi.Base.Model.IMessage sIMessage)
        {
            try
            {
                NHapi.Model.V231.Message.QRY_Q02 sQRY_Q02 = (NHapi.Model.V231.Message.QRY_Q02)sIMessage;
                var barcode = (sQRY_Q02.QRD.GetAllWhoSubjectFilterRecords()[0] as XCN).IDNumber.Value;

                VCheck.Lib.Data.Models.ScheduledTestModel sScheduledTestObj = new VCheck.Lib.Data.Models.ScheduledTestModel();
                var clinic = TestResultRepository.GetConfigurationByKey("ClinicID");
                if (clinic != null && !string.IsNullOrEmpty(clinic.ConfigurationValue))
                {
                    ScheduledTestRepository.RefreshData();
                    sScheduledTestObj = ScheduledTestRepository.GetScheduledTestByUniqueID(Host.CreateApplicationBuilder().Configuration, barcode, "Unique");
                }

                var gender = string.Concat(sScheduledTestObj.Gender.Split(' ').Where(w => w.Length > 0).Select(w => w[0]));
                Species.Category = sScheduledTestObj.Species;
                var RETExist = sScheduledTestObj.ScheduledTestType == "Whole blood+CDR";

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0B);
                Message response = new Message();

                // DSR message start //
                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, sQRY_Q02.MSH.FieldSeparator.Value);
                msh.Field(2, sQRY_Q02.MSH.EncodingCharacters.Value);
                msh.Field(3, "VCheck Listener");
                msh.Field(4, "LIS");
                msh.Field(5, sQRY_Q02.MSH.SendingApplication.NamespaceID.Value);
                msh.Field(6, sQRY_Q02.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "DSR^Q03");
                msh.Field(10, "12");
                //msh.Field(10, sQRY_Q02.MSH.MessageControlID.Value.ToString());
                msh.Field(11, sQRY_Q02.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sQRY_Q02.MSH.VersionID.VersionID.Value);
                msh.Field(16, "0");
                msh.Field(18, "UTF-8");
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                Segment dsp;
                for (int i = 1; i < 32; i++)
                {
                    string value = "";

                    if (i == 1) { value = "325643"; } // patient current number
                    else if (i == 3) { value = sScheduledTestObj.PatientName; } // patient name
                    else if (i == 5) { value = gender; } // gender （Sex Male=M；FeMale=F; Neutered male=NM; Spayed feMale=SF）
                    else if (i == 7) { value = sScheduledTestObj.OwnerName + "^" + sScheduledTestObj.InchargePerson; } // (Owner^Doctor)
                    else if (i == 10) { value = "12345678901"; } // phone number
                    else if (i == 12) { value = sScheduledTestObj.PatientID; } // sample id
                    else if (i == 13) { value = "2"; } // value of age
                    else if (i == 14) { value = "M"; } // AgeUnit(Year=Y; Month=M)
                    else if (i == 16) { value = "LIS"; } // hospital
                    else if (i == 17) { value = "24.3"; } // weight
                    else if (i == 19) { value = Species.SubCategory; } // Species (Dog=0;Cat=1;Cus1=21;Cus2=22)
                    else if (i == 26) { value = "FullBlood"; } // SampleType(Whole blood=FullBlood; Pre-diluted=Diluent)
                    else if (i == 27) { value = "Bionote"; } // ordering provider
                    else if (i == 29) { value = "CBC^CBC^1"; } // if test mode include CBC
                    else if (i == 30) { value = "DIFF^DIFF^1"; } // if test mode include DIFF
                    else if (i == 31 && RETExist) { value = "RET^RET^1"; } // if test mode include RET

                    response = new Message();
                    dsp = new Segment("DSP");
                    dsp.Field(1, i.ToString());
                    dsp.Field(3, value);
                    response.Add(dsp);
                    frame.Append(response.SerializeMessage());
                    frame.Append((char)0x0d);
                }

                response = new Message();
                var dsc = new Segment("DSC");
                dsc.Field(1, "1");
                response.Add(dsc);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                frame.Append((char)0x1C);
                frame.Append((char)0x0D);
                // DSR message end //

                frame.Append((char)0x0B);

                //ACK message
                // ------------- Message Header ------------//
                response = new Message();
                msh = new Segment("MSH");
                msh.Field(1, sQRY_Q02.MSH.FieldSeparator.Value);
                msh.Field(2, sQRY_Q02.MSH.EncodingCharacters.Value);
                msh.Field(3, "VCheck Listener");
                msh.Field(4, "LIS");
                msh.Field(5, sQRY_Q02.MSH.SendingApplication.NamespaceID.Value);
                msh.Field(6, sQRY_Q02.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "QCK^Q02");
                msh.Field(10, "12");
                msh.Field(11, sQRY_Q02.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sQRY_Q02.MSH.VersionID.VersionID.Value);
                msh.Field(16, "0");
                msh.Field(18, "UTF-8");
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement (No error) ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, "12");
                msa.Field(3, "Message accepted");
                msa.Field(6, "0");
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                frame.Append((char)0x1C);
                frame.Append((char)0x0D);

                var ackMessage = frame.ToString();

                return ackMessage;

            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }
    }
}
