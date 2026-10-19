using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VCheck.Lib.Data;

//using VCheck.Interface.API;
using VCheckListenerWorker_Others.Lib.DBContext;
using VCheckListenerWorker_Others.Lib.Models;
using VCheckListenerWorker_Others.Lib.PMS;

namespace VCheckListenerWorker_Others.Lib.Logic.HL7.V231
{
    public class HL7Repository
    {
        static VCheckListenerWorker_Others.Lib.Util.Logger _logger = new VCheckListenerWorker_Others.Lib.Util.Logger();        

        /// <summary>
        /// Process HL7 Message
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <param name="sSystemName"></param>
        /// <returns></returns>
        public static async Task<bool> ProcessMessageAsync(NHapi.Base.Model.IMessage sIMessage, String sSystemName)
        {
            Boolean isSuccess = false;

            try
            {
                NHapi.Model.V231.Message.ORU_R01 sORU_R01 = (NHapi.Model.V231.Message.ORU_R01)sIMessage;
                String? sResultTestCode = "";
                String? sResultTestType = "";
                String? sOperatorID = "";
                String? sPatientID = "";
                String? sPatientName = "";
                String? strObserveValue = "";
                String? sSerialNo = "";
                String? sUniversalIdentifier = "";
                String? sTestResultStatus = "";
                Boolean isRangeReference = false;
                String? strResultObservStatus = "";
                String? sDoctorName = "";
                Decimal iResultValue = 0;
                DateTime dAnalysisDateTime = DateTime.MinValue;
                DateTime CurrentDatetime = DateTime.Now;
                string age = "";
                string sSpecies = "General";

                //if (sORU_R01.MSH.SendingApplication.NamespaceID != null && sORU_R01.MSH.SendingApplication.NamespaceID.Value != null)
                //{
                //    sSerialNo = sORU_R01.MSH.SendingApplication.NamespaceID.Value.Trim();
                //}

                // --------------- Message Header --------------//
                tbltestanalyze_results_messageheader sMSHObj = new tbltestanalyze_results_messageheader
                {
                    FieldSeparator = sORU_R01.MSH.FieldSeparator.Value,
                    EncodingCharacters = sORU_R01.MSH.EncodingCharacters.Value,
                    SendingApplication = ((sORU_R01.MSH.SendingApplication.NamespaceID.Value != null) ? sORU_R01.MSH.SendingApplication.NamespaceID.Value.Trim() : "") + "^" +
                                         ((sORU_R01.MSH.SendingApplication.UniversalID.Value != null) ? sORU_R01.MSH.SendingApplication.UniversalID.Value.Trim() : "") + "^" +
                                         ((sORU_R01.MSH.SendingApplication.UniversalIDType.Value != null) ? sORU_R01.MSH.SendingApplication.UniversalIDType.Value.Trim() : ""),
                    SendingFacility = sORU_R01.MSH.SendingFacility.NamespaceID.Value,
                    ReceivingApplication = sORU_R01.MSH.ReceivingApplication.NamespaceID.Value,
                    ReceivingFacility = sORU_R01.MSH.ReceivingFacility.NamespaceID.Value,
                    DateTimeMessage = sORU_R01.MSH.DateTimeOfMessage.TimeOfAnEvent.ToString(),
                    MessageType = sORU_R01.MSH.MessageType.MessageType.Value + "^" +
                                  sORU_R01.MSH.MessageType.TriggerEvent.Value + "^" +
                                  sORU_R01.MSH.MessageType.MessageStructure.Value,
                    MessageControlID = sORU_R01.MSH.MessageControlID.Value.ToString(),
                    ProcessingID = sORU_R01.MSH.ProcessingID.ProcessingID.Value,
                    VersionID = sORU_R01.MSH.VersionID.VersionID.Value,
                    AcceptAckmgtType = sORU_R01.MSH.AcceptAcknowledgmentType.Value,
                    AppAckmgtType = sORU_R01.MSH.ApplicationAcknowledgmentType.Value,
                    CountryCode = sORU_R01.MSH.CountryCode.Value,
                    CharacterSet = (sORU_R01.MSH.GetCharacterSet().Length > 0) ? sORU_R01.MSH.GetCharacterSet().FirstOrDefault().Value : null,
                    PrincipalLanguageMsg = sORU_R01.MSH.PrincipalLanguageOfMessage.Identifier.Value + "^" +
                                           sORU_R01.MSH.PrincipalLanguageOfMessage.Text.Value + "^" +
                                           sORU_R01.MSH.PrincipalLanguageOfMessage.NameOfCodingSystem.Value,
                    MessageProfileIdentifier = null
                };

                // ------------ Patient Identification --------------------//
                List<tbltestanalyze_results_patientidentification> sPIDObj = new List<tbltestanalyze_results_patientidentification>();

                tbltestanalyze_results_patientidentification sPID = new tbltestanalyze_results_patientidentification();

                if(sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.PatientID != null)
                {
                    sPID.PatientID = sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.PatientID.ID.Value;
                    sPatientID = sPID.PatientID;
                }

                if(sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetRace().FirstOrDefault() != null && !string.IsNullOrEmpty(sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetRace().FirstOrDefault().Text.ToString()))
                {
                    sSpecies = sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetRace().FirstOrDefault().Text.ToString();
                }

                if (sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.SetIDPID != null)
                {
                    sPID.SetID = sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.SetIDPID.Value;
                }

                if (sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.PatientIdentifierListRepetitionsUsed > 0)
                {                    
                    sPID.AlternatePatientID = (sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetAlternatePatientIDPID().Length > 0) ?
                                               sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetAlternatePatientIDPID().FirstOrDefault().ID.ToString() : null;

                    sPID.PatientIdentifierList = (sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetPatientIdentifierList().Length > 0) ?
                                                 sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetPatientIdentifierList().FirstOrDefault().ID.ToString() : null;


                    if (String.IsNullOrEmpty(sPID.PatientID) && !String.IsNullOrEmpty(sPID.PatientIdentifierList))
                    {
                        sPatientID = sPID.PatientIdentifierList;
                    }
                }

                DateTime birthdate = DateTime.Now;
                var datetimeOfBirth = sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.DateTimeOfBirth.TimeOfAnEvent.ToString();

                if (string.IsNullOrEmpty(datetimeOfBirth))
                {
                    age = "General";
                }
                else if (datetimeOfBirth.Length == 1)
                {
                    if (datetimeOfBirth.ToString() == "C")
                    {
                        age = "Child";
                    }
                    else
                    {
                        age = "General";
                    }
                }
                else if (datetimeOfBirth.Contains("Y") || datetimeOfBirth.Contains("M") || datetimeOfBirth.Contains("D") || datetimeOfBirth.Contains("H"))
                {
                    MatchCollection matches = Regex.Matches(datetimeOfBirth, @"\d+");

                    string ageUnit = datetimeOfBirth.Replace(matches[0].Value, "");

                    if (ageUnit == "Y" && int.Parse(matches[0].Value) > 2) { age = "General"; }
                    else { age = "Child"; }
                }
                else if (datetimeOfBirth.Length > 7)
                {
                    if (datetimeOfBirth.Length == 8)
                    {
                        birthdate = DateTime.ParseExact(datetimeOfBirth, "yyyyMMdd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        DateTime.TryParse(datetimeOfBirth, out birthdate);
                    }

                    if ((DateTime.Now.Year - birthdate.Year) > 2) { age = "General"; }
                    else { age = "Child"; }
                }


                if (sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.PatientNameRepetitionsUsed > 0)
                {
                    var sNameObj = sORU_R01.PATIENT_RESULTs.FirstOrDefault().PATIENT.PID.GetPatientName().FirstOrDefault();

                    if (sNameObj != null)
                    {
                        sPID.PatientName = sNameObj.FamilyLastName.FamilyName.Value + "^" +
                                           sNameObj.GivenName + "^" +
                                           sNameObj.MiddleInitialOrName + "^" +
                                           sNameObj.SuffixEgJRorIII + "^" +
                                           sNameObj.PrefixEgDR + "^" +
                                           sNameObj.DegreeEgMD + "^" +
                                           sNameObj.NameTypeCode;

                        sPatientName = string.IsNullOrEmpty(sNameObj.GivenName.Value) ? sNameObj.FamilyLastName.FamilyName.Value : sNameObj.GivenName.Value;


                        if (sPID.PatientName.Replace("^", "").Length == 0)
                        {

                            sPID.PatientName = "";
                        }
                    }
                    else
                    {
                        using (var ctx = new TestResultDBContext(Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder().Configuration))
                        {
                            var patientInfo = ctx.txn_Testresults.FirstOrDefault(x => x.PatientID == sPatientID);
                            if (patientInfo != null)
                            {
                                sPID.PatientName = patientInfo.PatientName;
                            }
                            else
                            {
                                sPID.PatientName = "";
                            }
                        }
                    }
                }
                sPIDObj.Add(sPID);

                //----------------- Patient Visit Record ---------------------//
                var sPVObj = new tbltestanalyze_results_patientvisit();

                if (sORU_R01.GetPATIENT_RESULT().PATIENT.VISIT.PV1.SetIDPV1.Value != null)
                {
                    var sPatientVisitation1 = sORU_R01.GetPATIENT_RESULT().PATIENT.VISIT.PV1;
                    var sPatientVisitation2 = sORU_R01.GetPATIENT_RESULT().PATIENT.VISIT.PV2;

                    sPVObj.SetID = sPatientVisitation1.SetIDPV1.Value;
                    sPVObj.PatientClass = sPatientVisitation1.PatientClass.Value;
                    sPVObj.AssignedPatientLocation = sPatientVisitation1.AssignedPatientLocation.Room.Value;
                    sPVObj.AdmissionType = sPatientVisitation1.AdmissionType.Value;
                    sPVObj.PreadmitNumber = sPatientVisitation1.PreadmitNumber.ID.Value;
                    sPVObj.PriorPatientLocation = sPatientVisitation1.PriorPatientLocation.LocationDescription.ToString();

                    sPVObj.AttendingDoctor = (sPatientVisitation1.GetAttendingDoctor().Length > 0) ?
                                              sPatientVisitation1.GetAttendingDoctor().FirstOrDefault().IDNumber.Value : null;
                    sPVObj.ReferringDoctor = (sPatientVisitation1.GetReferringDoctor().Length > 0) ?
                                              sPatientVisitation1.GetReferringDoctor().FirstOrDefault().IDNumber.Value : null;
                    sPVObj.ConsultingDoctor = (sPatientVisitation1.GetConsultingDoctor().Length > 0) ?
                                              sPatientVisitation1.GetConsultingDoctor().FirstOrDefault().IDNumber.Value : null;
                    sPVObj.HospitalService = sPatientVisitation1.HospitalService.Value;
                    sPVObj.TemporaryLocation = sPatientVisitation1.TemporaryLocation.LocationDescription.ToString();
                    sPVObj.PreadmitTestIndicator = sPatientVisitation1.PreadmitTestIndicator.Value;
                    sPVObj.ReAdmissionIndicator = sPatientVisitation1.ReAdmissionIndicator.Value;
                    sPVObj.AdmitSource = sPatientVisitation1.AdmitSource.Value;
                    sPVObj.AmbulatoryStatus = (sPatientVisitation1.GetAmbulatoryStatus().Length > 0) ?
                                               sPatientVisitation1.GetAmbulatoryStatus().FirstOrDefault().Value : null;
                    sPVObj.VIPIndicator = sPatientVisitation1.VIPIndicator.Value;
                    sPVObj.AdmittingDoctor = (sPatientVisitation1.GetAdmittingDoctor().Length > 0) ?
                                              sPatientVisitation1.GetAdmittingDoctor().FirstOrDefault().GivenName.Value : null;
                    sPVObj.PatientType = sPatientVisitation1.PatientType.Value;
                    sPVObj.VisitNumber = sPatientVisitation1.VisitNumber.ID.Value;

                    sDoctorName = sPVObj.AttendingDoctor;
                }
                else
                {
                    sPVObj = null;
                }

                //----------------- Observation Request ----------------------//
                var sTestResultLst = new List<txn_testresults>();
                var sTestResultDetails = new List<txn_testresults_details>();
                var sTestResultGraph = new List<txn_testresults_graphsExtended>();
                var sNTEObj = new List<tbltestanalyze_results_notes>();
                var sOBXObjList = new List<tbltestanalyze_results_observationresult>();
                var sOBRObj = new tbltestanalyze_results_observationrequest();
                foreach (var observation in sORU_R01.PATIENT_RESULTs.FirstOrDefault().ORDER_OBSERVATIONs)
                {
                    String sFillerOrdNum = "";
                    sFillerOrdNum = observation.OBR.FillerOrderNumber.EntityIdentifier.Value + "^" +
                                                observation.OBR.FillerOrderNumber.NamespaceID.Value + "^" +
                                                observation.OBR.FillerOrderNumber.UniversalID.Value + "^" +
                                                observation.OBR.FillerOrderNumber.UniversalIDType.Value;

                    sPatientID = string.IsNullOrEmpty(sPatientID) ? observation.OBR.FillerOrderNumber.EntityIdentifier.Value : sPatientID;

                    if (sFillerOrdNum.Replace("^", "").Length == 0)
                    {
                        sFillerOrdNum = "";
                    }

                    var extraComp = observation.OBR.PlacerField1.ExtraComponents;
                    int totalExtraComp = extraComp.NumComponents();
                    //sResultTestType = string.IsNullOrEmpty(sResultTestType) ? observation.OBR.UniversalServiceID.Identifier.Value : observation.OBR.UniversalServiceID.Text.Value;
                    sResultTestType = totalExtraComp > 1 ? extraComp.GetComponent(2).Data.ToString() : observation.OBR.SpecimenSource.SpecimenSourceNameOrCode.Identifier.Value;
                    sResultTestCode = totalExtraComp > 1 ? extraComp.GetComponent(1).Data.ToString() : "";
                    sUniversalIdentifier = observation.OBR.UniversalServiceID.Text.Value;
                    sSerialNo = string.IsNullOrEmpty(observation.OBR.UniversalServiceID.Text.Value) ? observation.OBR.UniversalServiceID.Identifier.Value : observation.OBR.UniversalServiceID.Text.Value;

                    if (string.IsNullOrEmpty(Worker.MainModel.DeviceType) && !string.IsNullOrEmpty(sSerialNo))
                    {
                        string deviceType;
                        var device = TestResultRepository.GetDeviceByIPSerialNo(null, sSerialNo, out deviceType);

                        Worker.MainModel.DeviceType = deviceType;
                    }

                    sOBRObj.SetID = observation.OBR.SetIDOBR.Value;
                    sOBRObj.PlacerOrderNumber = observation.OBR.PlacerOrderNumber.EntityIdentifier.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.NamespaceID.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.UniversalID.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.UniversalIDType.Value;
                    sOBRObj.FillerOrderNumber = sFillerOrdNum;
                    sOBRObj.UniversalServIdentifier = observation.OBR.UniversalServiceID.Identifier.Value + "^" +
                                                      observation.OBR.UniversalServiceID.Text.Value + "^" +
                                                      observation.OBR.UniversalServiceID.NameOfCodingSystem.Value;
                    sOBRObj.Priority = observation.OBR.PriorityOBR.Value;
                    sOBRObj.RequestedDateTime = (observation.OBR.RequestedDateTime.TimeOfAnEvent.Value != null) ?
                                                 observation.OBR.RequestedDateTime.TimeOfAnEvent.Value.ToString().Trim() : null;
                    sOBRObj.ObservationDateTime = (observation.OBR.ObservationDateTime.TimeOfAnEvent.Value != null) ?
                                                  observation.OBR.ObservationDateTime.TimeOfAnEvent.Value.ToString().Trim() : null;
                    sOBRObj.ObservationEndDateTime = (observation.OBR.ObservationEndDateTime.TimeOfAnEvent.Value != null) ?
                                                      observation.OBR.ObservationEndDateTime.TimeOfAnEvent.Value.ToString().Trim() : null;
                    sOBRObj.CollectVolume = observation.OBR.CollectionVolume.Quantity.Value;
                    sOBRObj.CollectorIdentifier = (observation.OBR.GetCollectorIdentifier().Count() > 0) ?
                                                   observation.OBR.GetCollectorIdentifier().FirstOrDefault().IDNumber.Value : null;
                    sOBRObj.SpecimenActionCode = observation.OBR.SpecimenActionCode.Value;

                    if (observation.NTEs.Count() > 0)
                    {
                        sNTEObj.Add(new tbltestanalyze_results_notes
                        {
                            SetID = (observation.NTEs.Count() > 0) ?
                                    observation.NTEs.FirstOrDefault().SetIDNTE.Value : null,
                            Segment = "OBR",
                            SourceComment = (observation.NTEs.Count() > 0) ?
                                             observation.NTEs.FirstOrDefault().SourceOfComment.Value : null,
                            Comment = GenerateNTEComments(observation.NTEs.ToList())
                        });

                        if (sUniversalIdentifier.ToLower().Contains("babesia") || sUniversalIdentifier.ToLower().Contains("8 panel"))
                        {
                            sTestResultStatus = GenerateNTEComments(observation.NTEs.ToList());
                        }
                    }

                    // --------------- Observation Results ----------------//
                    foreach (var observationDetail in observation.OBSERVATIONs)
                    {                        
                        String sInterpretation = "";
                        String? sObservValue = "";
                        strObserveValue = "";
                        if (observationDetail.OBX.GetObservationValue().Count() > 0)
                        {
                            var tempsObservValue = observationDetail.OBX.GetObservationValue().FirstOrDefault().Data;

                            if (tempsObservValue.GetType() == typeof(NHapi.Model.V231.Datatype.NA))
                            {
                                var sNAObject = tempsObservValue;

                                int iTotalComponent = sNAObject.ExtraComponents.NumComponents();
                                List<String> sVal = new List<String>();

                                // --- Get from Component ------ //
                                PropertyInfo[] props = sNAObject.GetType().GetProperties();
                                foreach (PropertyInfo p in props)
                                {
                                    if (p.PropertyType == typeof(NHapi.Model.V231.Datatype.NM))
                                    {
                                        sVal.Add(p.GetValue(sNAObject, null).ToString());
                                    };
                                }

                                // -- Get From Extra Component -----//
                                for (int i = 0; i < iTotalComponent; i++)
                                {
                                    if (sNAObject.ExtraComponents.GetComponent(i).Data.ToString() != null)
                                    {
                                        sVal.Add(sNAObject.ExtraComponents.GetComponent(i).Data.ToString());
                                    }
                                }

                                if (sVal.Count() > 0)
                                {
                                    sObservValue = String.Join("^", sVal);
                                    Decimal.TryParse(sObservValue.Replace("<", ""), CultureInfo.InvariantCulture, out iResultValue);
                                }
                            }
                            else
                            {
                                if (tempsObservValue.GetType() == typeof(NHapi.Model.V231.Datatype.CWE))
                                {
                                    var sCWEObject = tempsObservValue;

                                    List<String> sCWEVal = new List<String>();
                                    PropertyInfo[] propCWE = sCWEObject.GetType().GetProperties();
                                    foreach (PropertyInfo c in propCWE)
                                    {
                                        if (c.PropertyType == typeof(NHapi.Base.Model.IType[]))
                                        {
                                            NHapi.Base.Model.IType[] iTypeObj = (NHapi.Base.Model.IType[])c.GetValue(sCWEObject, null);
                                            for (int i = 0; i < 4; i++)
                                            {
                                                sCWEVal.Add(iTypeObj[i].ToString());
                                            }
                                        }
                                    }

                                    if (sCWEVal.Count() > 0)
                                    {
                                        sObservValue = String.Join("^", sCWEVal);
                                    }
                                }
                                else if (tempsObservValue.GetType() == typeof(NHapi.Model.V231.Datatype.ED))
                                {
                                    var componentArray = (tempsObservValue as NHapi.Model.V231.Datatype.ED).Components;

                                    //var name = (componentArray[0] as NHapi.Model.V231.Datatype.HD).NamespaceID.Value;
                                    var name = observationDetail.OBX.ObservationIdentifier.Identifier.Value;

                                    if (!string.IsNullOrEmpty(name) && (name.Contains("Style1") || name.Contains("IMG")) && !name.Contains("N-Hollow"))
                                    {
                                        var fileName = name + "." + componentArray[1].ToString();
                                        var base64string = componentArray[3].ToString();
                                        sTestResultGraph.Add(new txn_testresults_graphsExtended() { FileName = fileName, Base64String = base64string });                                       
                                    }

                                    continue;
                                }
                                else
                                {
                                    sObservValue = tempsObservValue.ToString();
                                }

                            }

                            strObserveValue = sObservValue;
                        }
                        else if (observationDetail.OBX.GetProbability().Count() > 0)
                        {
                            sObservValue = observationDetail.OBX.GetProbability()[0].Value;
                            Decimal.TryParse(sObservValue.Replace("<", ""), CultureInfo.InvariantCulture, out iResultValue);
                        }
                        else
                        {
                            if (observationDetail.OBX.ObservationResultStatus != null)
                            {
                                if (observationDetail.OBX.ObservationResultStatus.Value == "F")
                                {
                                    strObserveValue = "Valid";
                                }
                                if (observationDetail.OBX.ObservationResultStatus.Value == "X")
                                {
                                    strObserveValue = "Invalid";
                                }
                            }
                        }

                        String sUnitValue = "";
                        sUnitValue = observationDetail.OBX.Units.Identifier.Value + "^" +
                                observationDetail.OBX.Units.Text.Value + "^" +
                                observationDetail.OBX.Units.NameOfCodingSystem;

                        if (sUnitValue.Replace("^", "").Length == 0)
                        {
                            sUnitValue = "";
                        }

                        String sObservIdentifier = "";
                        sObservIdentifier = observationDetail.OBX.ObservationIdentifier.Identifier.Value + "^" +
                                            observationDetail.OBX.ObservationIdentifier.Text.Value + "^" +
                                            observationDetail.OBX.ObservationIdentifier.NameOfCodingSystem.Value + "^" +
                                            observationDetail.OBX.ObservationIdentifier.AlternateIdentifier.Value;
                        if (sObservIdentifier.Replace("^", "").Length == 0)
                        {
                            sObservIdentifier = "";
                        }

                        if (observationDetail.OBX.GetResponsibleObserver().Length > 0)
                        {
                            sOperatorID = observationDetail.OBX.GetResponsibleObserver().FirstOrDefault().IDNumber.Value;
                        }
                        else
                        {
                            sOperatorID = null;
                        }

                        String sEquipmentIdentifier = "";
                        //if (observationDetail.OBX.GetEquipmentInstanceIdentifier().Length > 0)
                        //{
                        //    sEquipmentIdentifier = observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().EntityIdentifier.Value + "^" +
                        //                           observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().NamespaceID.Value + "^" +
                        //                           observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().UniversalID.Value;
                        //    if (sEquipmentIdentifier.Replace("^", "").Length == 0)
                        //    {
                        //        sEquipmentIdentifier = "";
                        //    }
                        //}

                        if (!String.IsNullOrEmpty(observation.OBR.ObservationDateTime.TimeOfAnEvent.ToString()))
                        {
                            if (observation.OBR.ObservationDateTime.TimeOfAnEvent.Value.ToString().Length == 14)
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observation.OBR.ObservationDateTime.TimeOfAnEvent.Value.ToString(), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else if (observation.OBR.ObservationDateTime.TimeOfAnEvent.ToString().Length == 22)
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observation.OBR.ObservationDateTime.TimeOfAnEvent.Value.ToString(), "yyyyMMdd HH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observation.OBR.ObservationDateTime.TimeOfAnEvent.Value.ToString(), "yyyyMMddHHmmss-ffff", System.Globalization.CultureInfo.InvariantCulture);
                            }
                        }

                        sOBXObjList.Add(new tbltestanalyze_results_observationresult
                        {
                            SetID = observationDetail.OBX.SetIDOBX.Value,
                            ValueType = observationDetail.OBX.ValueType.Value,
                            ObservationIdentifier = sObservIdentifier,
                            ObservationSubID = observationDetail.OBX.ObservationSubID.Value,
                            ObservationValue = sObservValue,
                            Units = sUnitValue,
                            ReferencesRange = observationDetail.OBX.ReferencesRange.Value,
                            AbnormalFlag = (observationDetail.OBX.GetAbnormalFlags().Length > 0) ?
                                           observationDetail.OBX.GetAbnormalFlags().FirstOrDefault().Value : null,
                            ObservationResultStatus = observationDetail.OBX.ObservationResultStatus.Value,
                            ObservationDateTime = (observationDetail.OBX.DateTimeOfTheObservation.TimeOfAnEvent.Value != null) ?
                                                   observationDetail.OBX.DateTimeOfTheObservation.TimeOfAnEvent.Value.ToString() : null,
                            ProducerID = "",
                            ResponsibleObserver = sOperatorID,
                            ObservationMethod = (observationDetail.OBX.GetObservationMethod().Length > 0) ?
                                                observationDetail.OBX.GetObservationMethod().FirstOrDefault().Text.ToString() : null,
                            EquipmentInstanceIdentifier = sEquipmentIdentifier,
                            AnalysisDateTime = null

                        });

                        // ------------ Notes -------------------//
                        if (observationDetail.NTEs.Count() > 0)
                        {
                            //String sComment = GenerateNTEComments(observationDetail.NTEs.ToList());
                            String sComment = "";

                            sNTEObj.Add(new tbltestanalyze_results_notes
                            {
                                SetID = (observationDetail.NTEs.Count() > 0) ?
                                        observationDetail.NTEs.FirstOrDefault().SetIDNTE.Value.Trim() : null,
                                Segment = "OBX",
                                SourceComment = (observationDetail.NTEs.Count() > 0) ?
                                                 observationDetail.NTEs.FirstOrDefault().SourceOfComment.Value : null,
                                Comment = sComment
                            });

                            if (sComment.ToLower().Contains("cut off index"))
                            {
                                String sValue = "";
                                String[] strArryValue = sComment.Split(",");
                                if (strArryValue.Length > 0)
                                {
                                    sValue = strArryValue[1].Replace("Value=", "").Trim();
                                }

                                Decimal.TryParse(sValue, CultureInfo.InvariantCulture, out iResultValue);
                                sObservValue = sValue;
                            }

                            if (sComment.ToLower().Contains("interpretation"))
                            {
                                //sInterpretation
                                String[] strArryValue = sComment.Split("=");
                                if (strArryValue.Length > 0)
                                {
                                    sInterpretation = strArryValue[1].Trim();
                                }
                            }
                        }


                        if (observationDetail.OBX.ObservationResultStatus.Value == "F")
                        {
                            strResultObservStatus = "Valid";
                        }
                        else if (observationDetail.OBX.ObservationResultStatus.Value == "X")
                        {
                            strResultObservStatus = "Invalid";
                        }

                        isRangeReference = !String.IsNullOrEmpty(observationDetail.OBX.ReferencesRange.Value);

                        if (!(observationDetail.OBX.ObservationIdentifier.Identifier.Value.ToLower() == "age") &&
                            !(observationDetail.OBX.ObservationIdentifier.Identifier.Value.ToLower() == "weight") &&
                            !(observationDetail.OBX.ObservationIdentifier.Identifier.Value.ToLower().Contains("title")))
                        {
                            String sStatus = "";
                            string referenceRange = observationDetail.OBX.ReferencesRange.Value;
                            string measuringRange = "";
                            var testParamName = (observationDetail.OBX.ObservationIdentifier.Text.Value != null && observationDetail.OBX.ObservationIdentifier.Text.Value != "") ? observationDetail.OBX.ObservationIdentifier.Text.Value : observationDetail.OBX.ObservationIdentifier.Identifier.Value;

                            testParamName = testParamName.Replace("UC_", "").Replace("UD_","");

                            if (testParamName == "WBC") { testParamName = "Leukocytes"; }
                            else if (testParamName == "KET") { testParamName = "Ketone"; }
                            else if (testParamName == "NIT") { testParamName = "Nitrite"; }
                            else if (testParamName == "URO") { testParamName = "Urobilinogen"; }
                            else if (testParamName == "BIL") { testParamName = "Billirubin"; }
                            else if (testParamName == "GLU") { testParamName = "Glucose"; }
                            else if (testParamName == "PRO") { testParamName = "Protein"; }
                            else if (testParamName == "SG") { testParamName = "Specific Gravity"; }
                            else if (testParamName == "PH") { testParamName = "pH"; }
                            else if (testParamName == "BLD") { testParamName = "Blood"; }
                            else if (testParamName == "VC") { testParamName = "Ascorbic Acid"; }
                            else if (testParamName == "MA") { testParamName = "Microalbumin"; }
                            else if (testParamName == "CA") { testParamName = "Calcium"; }
                            else if (testParamName == "CR") { testParamName = "Creatinine"; }
                            else if (testParamName == "PCR") { testParamName = "UPC"; }
                            else if (testParamName == "DRBC-Other") { testParamName = "DRBC"; }
                            else if (testParamName == "REP") { testParamName = "RTEP"; }
                            else if (testParamName == "UNCC") { testParamName = "PAT"; }
                            else if (testParamName == "BACT_R") { testParamName = "BACT"; }
                            else if (testParamName == "IMPURITY") { testParamName = "OTHER"; }
                            else if (testParamName == "Transparency") { testParamName = "TUR"; }
                            else if (testParamName == "R_TATE") { testParamName = "R-RATE"; }
                            else if (testParamName == "MCV_CV") { testParamName = "MCV-CV"; }

                            if (Worker.MainModel.DeviceType == "U3") { sStatus = General.ProcessObservationResultStatusValueU3(isRangeReference, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue); }
                            else 
                            {
                                //sTestResultStatus = General.ProcessObservationResultStatusValueReferenceRange(Worker.MainModel.DeviceType, sObservValue, testParamName.Replace("?", ""), sSpecies, "General", out referenceRange, out measuringRange);

                                //sStatus = string.IsNullOrEmpty(sTestResultStatus) ? General.ProcessObservationResultStatusValue(isRangeReference, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue) : sTestResultStatus;

                                sStatus = General.ProcessObservationResultStatusValue(isRangeReference, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);
                            }

                            int testUnit = 0;
                            var unitExtended = int.TryParse(observationDetail.OBX.Units.Identifier.Value, out testUnit) ? observationDetail.OBX.Units.Identifier.Value + "^" + observationDetail.OBX.Units.Text.Value : observationDetail.OBX.Units.Identifier.Value;

                            sTestResultDetails.Add(new txn_testresults_details
                            {
                                TestParameter = testParamName.Replace("?", ""),
                                SubID = observationDetail.OBX.ObservationSubID.Value,
                                ProceduralControl = strResultObservStatus,
                                TestResultStatus = sStatus,
                                TestResultValue = sObservValue,
                                TestResultUnit = unitExtended,
                                ReferenceRange = string.IsNullOrEmpty(referenceRange) ? observationDetail.OBX.ReferencesRange.Value : referenceRange,
                                MeasuringRange = measuringRange,
                                Interpretation = sInterpretation
                            });

                            //if (testParamName.Contains("RET%")) { RETType = true; }
                        }

                    }

                }

                //if(string.IsNullOrEmpty(sResultTestType))
                //{ 
                //    sResultTestType = RETType ? "Vcheck H CBC+6DIFF+RET" : "Vcheck H CBC+6DIFF"; 
                //}

                String sOverallStatus = "Normal";
                if (sTestResultDetails != null)
                {
                    if ((sTestResultDetails.Where(x => x.TestResultStatus == "Positive").Count() > 0) ||
                        (sTestResultDetails.Where(x => x.TestResultStatus == "Invalid").Count() > 0) ||
                        (sTestResultDetails.Where(x => x.TestResultStatus == "Abnormal").Count() > 0))
                    {
                        sOverallStatus = "Abnormal";
                    }
                }

                var parameters = sOBXObjList.Select(x => x.ObservationIdentifier).ToList();
                var U3TestType = "";

                if (parameters.Any(x => x.Contains("UD_")) && !parameters.Any(x => x.Contains("UC_")))
                {
                    U3TestType = "Vcheck U Sediment 16 Plus";
                }
                else if (parameters.Any(x => x.Contains("UD_")) && parameters.Any(x => x.Contains("UC_")))
                {
                    U3TestType = "Vcheck U Both 16 Plus";
                }
                else if (!parameters.Any(x => x.Contains("UD_")) && parameters.Any(x => x.Contains("UC_")))
                {
                    U3TestType = "Vcheck U UC Vet 13 Plus";
                }

                sResultTestType = string.IsNullOrEmpty(U3TestType) ? sResultTestType : U3TestType;

                txn_testresults sTestResultObj = new txn_testresults();
                sTestResultObj.TestResultDateTime = dAnalysisDateTime;
                sTestResultObj.TestResultType = sResultTestType;
                sTestResultObj.OperatorID = sOperatorID;
                sTestResultObj.PatientID = sPatientID;
                sTestResultObj.PatientName = sPatientName;
                sTestResultObj.InchargePerson = sDoctorName;
                sTestResultObj.OverallStatus = sOverallStatus;
                sTestResultObj.CreatedDate = CurrentDatetime;
                sTestResultObj.CreatedBy = sSystemName;
                sTestResultObj.DeviceSerialNo = Worker.MainModel.DeviceSerialNum != null ? Worker.MainModel.DeviceSerialNum : sSerialNo;
                sTestResultObj.PMSFunction = "Visible";

                tbltestanalyze_results sResultObj = new tbltestanalyze_results
                {
                    MessageType = sORU_R01.MSH.MessageType.MessageStructure.Value,
                    MessageDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    CreatedBy = sSystemName
                };

                ScheduledTestModel? sScheduledTestObj = new ScheduledTestModel();
                var clinic = TestResultRepository.GetConfigurationByKey("ClinicID");
                var PMS = TestResultRepository.GetConfigurationByKey("InterfaceSettingsPMS");
                if (clinic != null && !string.IsNullOrEmpty(clinic.ConfigurationValue) && PMS != null && PMS.ConfigurationValue != "None")
                {
                    ScheduledTestRepository.RefreshData();
                    var tempSchedule = ScheduledTestRepository.GetScheduleByLocationPatientID(Host.CreateApplicationBuilder().Configuration, clinic.ConfigurationValue, sTestResultObj.PatientID, sResultTestType);
                    sScheduledTestObj = JsonConvert.DeserializeObject<ScheduledTestModel>(JsonConvert.SerializeObject(tempSchedule));

                    if (sScheduledTestObj != null)
                    {
                        sTestResultObj.PatientName = string.IsNullOrEmpty(sPatientName) ? sScheduledTestObj.PatientName : sPatientName;
                    }
                }

                Boolean bResult = TestResultRepository.insertTestObservationMessage(sResultObj, sMSHObj, sPIDObj, sOBRObj, sOBXObjList, sNTEObj, sPVObj);
                //Boolean bResult = true;
                if (bResult)
                {
                    // Insert into Test Result table & create notification 
                    sTestResultDetails = TestResultRepository.createTestResultsMultipleParam(sTestResultObj, sTestResultDetails, out sTestResultObj, sTestResultGraph);

                    if (sScheduledTestObj != null && !string.IsNullOrEmpty(sScheduledTestObj.ScheduleUniqueID))
                    {
                        if (string.IsNullOrEmpty(sResultTestCode) || double.TryParse(sResultTestCode, CultureInfo.InvariantCulture, out _))
                        {
                            var sTest = TestResultsRepository.GetTestByName(Host.CreateApplicationBuilder().Configuration, sTestResultObj.TestResultType);
                            sResultTestCode = sTest != null ? sTest.TestID : "VCheck";
                        }

                        PMSHandler pmsHandler = new PMSHandler();
                        var success = false;
                        success = await pmsHandler.SendToPMS(sTestResultObj, sTestResultDetails, sScheduledTestObj, sResultTestCode);

                        ScheduledTestRepository.RefreshData();
                        var data = ScheduledTestRepository.GetScheduledTestByOrderID(Host.CreateApplicationBuilder().Configuration, sScheduledTestObj.ScheduleUniqueID.Split("-")[1], sScheduledTestObj.CreatedBy, "Local");

                        if (data != null && data.ScheduleTestStatus != 3 && !success)
                        {
                            data.ScheduleTestStatus = 3;
                            data.UpdatedBy = sScheduledTestObj.CreatedBy;
                            data.UpdatedDate = DateTime.Now;

                            ScheduledTestRepository.UpdateScheduledTestStatus(Host.CreateApplicationBuilder().Configuration, data, sScheduledTestObj.CreatedBy);
                        }
                        else if (data != null && data.ScheduleTestStatus != 4 && success)
                        {
                            data.ScheduleTestStatus = 4;
                            data.UpdatedBy = sScheduledTestObj.CreatedBy;
                            data.UpdatedDate = DateTime.Now;

                            ScheduledTestRepository.UpdateScheduledTestStatus(Host.CreateApplicationBuilder().Configuration, data, sScheduledTestObj.CreatedBy);
                        }

                        if (!success)
                        {
                            NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientName, sSystemName);
                        }
                    }
                    else if(PMS != null && !string.IsNullOrEmpty(PMS.ConfigurationValue) && PMS.ConfigurationValue != "None")
                    {
                        NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientID, sSystemName);
                    }

                    if(PMS != null && PMS.ConfigurationValue != "None") { NotificationRepository.SendNotification(sTestResultObj.PatientID, sSystemName); }
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.Error("ProcessMessage >>> V231 >>> " + ex.ToString());

                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Get NTE Comment for HL7
        /// </summary>
        /// <param name="nte"></param>
        /// <returns></returns>
        public static String GenerateNTEComments(List<NHapi.Model.V231.Segment.NTE> nte)
        {
            string nteComment = "";
            try
            {
                for (int i = 0; i <= nte.Count() - 1; i++)
                {
                    if (nte[i].GetComment() != null && nte[i].GetComment().FirstOrDefault() != null && nte[i].GetComment().FirstOrDefault().Value != null)
                    {
                        if (nteComment != "")
                        {
                            nteComment += ", ";

                        }
                        nteComment += nte[i].GetComment().FirstOrDefault().Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("GenerateNTEComments >>> V231 >>> " + ex.ToString());
            }

            return nteComment;
        }
    }
}
