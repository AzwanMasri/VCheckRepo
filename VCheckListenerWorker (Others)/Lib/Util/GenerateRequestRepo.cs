using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheckListenerWorker_Others.Lib.Object;

namespace VCheckListenerWorker_Others.Lib.Util
{
    public class GenerateRequestRepo
    {
        /// <summary>
        /// Generate Query AWOS Message request
        /// </summary>
        /// <returns></returns>
        public static String RequestMessageQueryAWOS()
        {
            try
            {
                Message request = new Message();
                String sCurrentDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                // ------------- Message Header -----------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "");
                msh.Field(4, "");
                msh.Field(5, "");
                msh.Field(6, "LAB");
                msh.Field(7, sCurrentDateTime);
                msh.Field(9, "QBP^Q11^QBP_Q11");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, "P");
                msh.Field(12, "2.5.1");
                msh.Field(15, "NE");
                msh.Field(16, "AL");
                msh.Field(18, "UNICODE UTF-8");
                msh.Field(21, "LAB-27^IHE");
                request.Add(msh);

                // ----------- Query Parameter ------//
                Segment qpd = new Segment("QPD");
                qpd.Field(1, "WOS_ALL^Work Order Step All^IHELAW");
                qpd.Field(2, sCurrentDateTime);
                request.Add(qpd);

                // ------------ Response Control -------//
                Segment rcp = new Segment("RCP");
                rcp.Field(1, "");
                rcp.Field(3, "R^Real Time^HL70394");
                request.Add(rcp);

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(request.SerializeMessage());
                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                return frame.ToString();
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Generate AWOS Broadcast Message Request
        /// </summary>
        /// <returns></returns>
        public static String RequestMessageAWOSBroadcast()
        {
            try
            {
                Message request = new Message();

                // ------------- Message Header -----------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "");
                msh.Field(4, "");
                msh.Field(5, "");
                msh.Field(6, "LAB");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "OML^O33^OML_O33");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, "P");
                msh.Field(12, "2.5.1");
                msh.Field(15, "NE");
                msh.Field(16, "AL");
                msh.Field(18, "");
                msh.Field(21, "LAB-28^IHE");
                request.Add(msh);

                // ----------- Patient Identification ------//
                Segment pid = new Segment("PID");
                pid.Field(3, "");
                pid.Field(5, "");
                pid.Field(7, "");
                pid.Field(8, "");
                pid.Field(21, "");
                pid.Field(35, "");
                pid.Field(36, "");
                request.Add(pid);

                // ------------ Patient Visit -------//
                Segment pv1 = new Segment("PV1");
                pv1.Field(2, "");
                pv1.Field(3, "");
                pv1.Field(7, "");
                request.Add(pv1);
                
                // ------------- Specimen -----------//
                Segment spm = new Segment("SPM");
                spm.Field(1, "1");
                spm.Field(4, "Serum");
                spm.Field(11, "P^Patient^HL70369");
                request.Add(spm);

                // -------------- SAC ---------------//
                Segment sac = new Segment("SAC");
                sac.Field(3, "");
                request.Add(sac);

                // -------------- ORC ---------------//
                Segment orc = new Segment("ORC");
                orc.Field(1, "NW");
                orc.Field(2, "");
                orc.Field(4, "");
                orc.Field(5, "IP");
                orc.Field(9, DateTime.Now.ToString("yyyyMMddHHmmss"));
                request.Add(orc);

                // -------------- OBR ---------------//
                Segment obr = new Segment("OBR");
                obr.Field(2, "");
                obr.Field(4, "DC001B^Comprehensive 17^VCHECK");
                request.Add(obr);

                // -------------- OBX ---------------//
                Segment obx = new Segment("OBX");
                obx.Field(1, "1");
                obx.Field(2, "");
                obx.Field(3, "");
                obx.Field(5, "");
                obx.Field(6, "");
                obx.Field(11, "");
                obx.Field(29, "");
                request.Add(obx);

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(request.SerializeMessage());
                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                return frame.ToString();
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Generate AWOS Status changes Message Request
        /// </summary>
        /// <returns></returns>
        public static String RequestMessageAWOSStatusChanges()
        {
            try
            {
                Message request = new Message();

                // ------------- Message Header -----------//
                Segment msh = new Segment("MSH");
                msh.Field(1, "|");
                msh.Field(2, "^~\\&");
                msh.Field(3, "");
                msh.Field(4, "");
                msh.Field(5, "");
                msh.Field(6, "LAB");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddHHmmss"));
                msh.Field(9, "OUL^R22^OUL_R22");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, "P");
                msh.Field(12, "2.5.1");
                msh.Field(15, "NE");
                msh.Field(16, "AL");
                msh.Field(18, "");
                msh.Field(21, "LAB-28^IHE");
                request.Add(msh);

                // ----------- Patient Identification ------//
                Segment pid = new Segment("PID");
                pid.Field(3, "");
                pid.Field(5, "");
                pid.Field(7, "");
                pid.Field(8, "");
                pid.Field(21, "");
                pid.Field(35, "");
                pid.Field(36, "");
                request.Add(pid);

                // ------------ Patient Visit -------//
                Segment pv1 = new Segment("PV1");
                pv1.Field(2, "");
                pv1.Field(3, "");
                pv1.Field(7, "");
                request.Add(pv1);

                // ---------------- Specimen --------------//
                Segment spm = new Segment("SPM");
                spm.Field(1, "1");
                spm.Field(4, "Serum");
                spm.Field(11, "P^Patient^HL70369");
                request.Add(spm);

                // ------------------ SAC -----------------//
                Segment sac = new Segment("SAC");
                sac.Field(3, "");
                request.Add(sac);

                // ------------------ OBR -----------------//
                Segment obr = new Segment("OBR");
                obr.Field(2, "");
                obr.Field(4, "DC001B^Comprehensive 17^VCHECK");
                obr.Field(7, "");
                request.Add(obr);

                // ------------------- OBX ----------------//
                Segment obx = new Segment("OBX");
                obx.Field(1, "");
                obx.Field(2, "");
                obx.Field(3, "");
                obx.Field(4, "");
                obx.Field(5, "");
                obx.Field(6, "");
                obx.Field(7, "");
                obx.Field(11, "");
                obx.Field(16, "");
                obx.Field(18, "");
                obx.Field(19, "");
                obx.Field(29, "");
                request.Add(obx);

                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(request.SerializeMessage());
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
