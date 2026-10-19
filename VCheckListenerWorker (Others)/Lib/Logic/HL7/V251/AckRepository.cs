using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheckListenerWorker_Others.Lib.Object;

namespace VCheckListenerWorker_Others.Lib.Logic.HL7.V251
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
        /// Generate Ack Message
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <returns></returns>
        public static String GenerateAcknowlegeMessage(NHapi.Base.Model.IMessage sIMessage)
        {
            try
            {
                NHapi.Model.V251.Message.OUL_R22 sRU_R01 = (NHapi.Model.V251.Message.OUL_R22)sIMessage;

                //Message response = new Message();

                //// ------------- Message Header ------------//
                //Segment msh = new Segment("MSH");
                //msh.Field(1, sRU_R01.MSH.FieldSeparator.Value);
                //msh.Field(2, sRU_R01.MSH.EncodingCharacters.Value);
                //msh.Field(3, sRU_R01.MSH.SendingApplication.NamespaceID.Value + "^" +
                //             sRU_R01.MSH.SendingApplication.UniversalID.Value + "^" +
                //             sRU_R01.MSH.SendingApplication.UniversalIDType.Value);
                //msh.Field(4, sRU_R01.MSH.SendingFacility.NamespaceID.Value);
                //msh.Field(5, sRU_R01.MSH.ReceivingApplication.NamespaceID.Value);
                //msh.Field(6, sRU_R01.MSH.ReceivingFacility.NamespaceID.Value);
                //msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmsszzz"));
                ////msh.Field(9, "ACK^R01^ACK");
                //msh.Field(9, "ACK^OUL_R22^ACK");
                //msh.Field(10, Guid.NewGuid().ToString());
                //msh.Field(11, sRU_R01.MSH.ProcessingID.ProcessingID.Value);
                //msh.Field(12, sRU_R01.MSH.VersionID.VersionID.Value);
                //response.Add(msh);

                //// ------------- Message Acknowledgement ---------------------//
                //Segment msa = new Segment("MSA");
                //msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                //msa.Field(2, sRU_R01.MSH.MessageControlID.Value.ToString());
                //response.Add(msa);

                //StringBuilder frame = new StringBuilder();
                //frame.Append((char)0x0b);
                //frame.Append(response.SerializeMessage());
                //frame.Append((char)0x1c);
                //frame.Append((char)0x0d);


                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, sRU_R01.MSH.FieldSeparator.Value);
                msh.Field(2, sRU_R01.MSH.EncodingCharacters.Value);
                msh.Field(3, sRU_R01.MSH.SendingApplication.NamespaceID.Value);
                msh.Field(4, sRU_R01.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(5, sRU_R01.MSH.ReceivingApplication.NamespaceID.Value);
                msh.Field(6, sRU_R01.MSH.ReceivingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "ACK^R22^ACK");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, sRU_R01.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sRU_R01.MSH.VersionID.VersionID.Value);
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);


                response = new Message();

                // ------------- Message Acknowledgement ---------------------//
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, sRU_R01.MSH.MessageControlID.Value.ToString());
                response.Add(msa);                
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                var test = frame.ToString();

                return frame.ToString();
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Generate Ack Message for QBP_Q11
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <returns></returns>
        public static String GenerateAcknowlegeMessageQBP(NHapi.Base.Model.IMessage sIMessage)
        {
            try
            {
                NHapi.Model.V251.Message.QBP_Q11 sQBP_Q11 = (NHapi.Model.V251.Message.QBP_Q11)sIMessage;

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, sQBP_Q11.MSH.FieldSeparator.Value);
                msh.Field(2, sQBP_Q11.MSH.EncodingCharacters.Value);
                msh.Field(3, sQBP_Q11.MSH.SendingApplication.NamespaceID.Value);
                msh.Field(4, sQBP_Q11.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(5, sQBP_Q11.MSH.ReceivingApplication.NamespaceID.Value);
                msh.Field(6, sQBP_Q11.MSH.ReceivingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "RSP^K11^RSP_K11");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, sQBP_Q11.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sQBP_Q11.MSH.VersionID.VersionID.Value);
                response.Add(msh);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Message Acknowledgement ---------------------//
                response = new Message();
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, sQBP_Q11.MSH.MessageControlID.Value.ToString());
                response.Add(msa);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Response Control Parameter ---------------------//
                response = new Message();
                Segment qak = new Segment("QAK");
                qak.Field(1, DateTime.Now.ToString("yyyyMMddHHmmss"));
                qak.Field(2, "OK");
                qak.Field(3, "WOS_ALL^Work Order Step All^IHELAW");
                response.Add(qak);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                // ------------- Query parameter Definition ---------------------//
                response = new Message();
                Segment qpd = new Segment("QPD");
                qpd.Field(1, "WOS_ALL^Work Order Step All^IHELAW");
                qpd.Field(2, DateTime.Now.ToString("yyyyMMddHHmmss"));
                response.Add(qpd);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x0d);

                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                var test = frame.ToString();

                return frame.ToString();
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }
    }
}
