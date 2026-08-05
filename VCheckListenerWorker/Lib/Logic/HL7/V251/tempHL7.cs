using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheckListenerWorker.Lib.Models;

namespace VCheckListenerWorker.Lib.Logic.HL7.V251
{
    public class tempHL7
    {
        public static async Task ProcessMessage(NHapi.Base.Model.IMessage sIMessage, string sSystemName)
        {
            NHapi.Model.V251.Message.OUL_R22 sRU_R01 =
                (NHapi.Model.V251.Message.OUL_R22)sIMessage;

            string? sPatientID = "";
            string? sResultTestType = "";

            // Get Patient ID
            if (sRU_R01.PATIENT.PID.PatientIdentifierListRepetitionsUsed > 0)
            {
                var patient = sRU_R01.PATIENT.PID;

                string patientID = patient.PatientID.IDNumber.Value;
                string patientIdentifier =
                    patient.GetPatientIdentifierList().Length > 0
                        ? patient.GetPatientIdentifierList().FirstOrDefault().IDNumber.ToString()
                        : "";

                sPatientID = string.IsNullOrEmpty(patientID)
                    ? patientIdentifier
                    : patientID;
            }

            // Get Test Result Type
            foreach (var observation in sRU_R01.SPECIMENs.FirstOrDefault().ORDERs)
            {
                sResultTestType = observation.OBR.UniversalServiceIdentifier.Text.Value;
                break;
            }

            txn_testresults sTestResultObj = new txn_testresults();

            sTestResultObj.PatientID = sPatientID;
            sTestResultObj.TestResultType = sResultTestType;
        }
    }
}
