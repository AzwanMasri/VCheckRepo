using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheckListenerWorker.Lib.Object;

namespace VCheckListenerWorker.Lib.Util
{
    public class ResponseRepo
    {
        /// <summary>
        /// Populate Acknowledge Message
        /// </summary>
        /// <param name="sRU_R01"></param>
        /// <returns></returns>
        public static String CreateResponseMessage(NHapi.Model.V26.Message.ORU_R01 sRU_R01)
        {
            try
            {
                Message response = new Message();
                
                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, sRU_R01.MSH.FieldSeparator.Value);
                msh.Field(2, sRU_R01.MSH.EncodingCharacters.Value);
                msh.Field(3, sRU_R01.MSH.SendingApplication.NamespaceID.Value + "^" +
                             sRU_R01.MSH.SendingApplication.UniversalID.Value + "^" +
                             sRU_R01.MSH.SendingApplication.UniversalIDType.Value);
                msh.Field(4, sRU_R01.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(5, sRU_R01.MSH.ReceivingApplication.NamespaceID.Value);
                msh.Field(6, sRU_R01.MSH.ReceivingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmsszzz"));
                msh.Field(9, "ACK^R01^ACK");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, sRU_R01.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sRU_R01.MSH.VersionID.VersionID.Value);
                response.Add(msh);

                // ------------- Message Acknowledgement ---------------------//
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.CA.ToString());
                msa.Field(2, sRU_R01.MSH.MessageControlID.Value.ToString());
                response.Add(msa);

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                return frame.ToString();
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }

        public static String CreateResponseMessage2(NHapi.Model.V251.Message.OUL_R22 sRU_R01)
        {
            try
            {
                Message response = new Message();

                // ------------- Message Header ------------//
                Segment msh = new Segment("MSH");
                msh.Field(1, sRU_R01.MSH.FieldSeparator.Value);
                msh.Field(2, sRU_R01.MSH.EncodingCharacters.Value);
                msh.Field(3, sRU_R01.MSH.SendingApplication.NamespaceID.Value + "^" +
                             sRU_R01.MSH.SendingApplication.UniversalID.Value + "^" +
                             sRU_R01.MSH.SendingApplication.UniversalIDType.Value);
                msh.Field(4, sRU_R01.MSH.SendingFacility.NamespaceID.Value);
                msh.Field(5, sRU_R01.MSH.ReceivingApplication.NamespaceID.Value);
                msh.Field(6, sRU_R01.MSH.ReceivingFacility.NamespaceID.Value);
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmsszzz"));
                //msh.Field(9, "ACK^R01^ACK");
                msh.Field(9, "ACK^OUL_R22^ACK");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, sRU_R01.MSH.ProcessingID.ProcessingID.Value);
                msh.Field(12, sRU_R01.MSH.VersionID.VersionID.Value);
                response.Add(msh);

                // ------------- Message Acknowledgement ---------------------//
                Segment msa = new Segment("MSA");
                msa.Field(1, NHapi.Base.AcknowledgmentCode.AA.ToString());
                msa.Field(2, sRU_R01.MSH.MessageControlID.Value.ToString());
                response.Add(msa);

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(response.SerializeMessage());
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
