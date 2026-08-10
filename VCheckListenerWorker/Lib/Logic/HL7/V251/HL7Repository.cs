using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheckListenerWorker.Lib.Models;
using VCheckListenerWorker.Lib.PMS;

namespace VCheckListenerWorker.Lib.Logic.HL7.V251
{
    public class HL7Repository
    {
        static VCheckListenerWorker.Lib.Util.Logger _logger = new VCheckListenerWorker.Lib.Util.Logger();

        /// <summary>
        /// Process HL7 Message
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <param name="sSystemName"></param>
        /// <returns></returns>
        public async static Task<Boolean> ProcessMessage(NHapi.Base.Model.IMessage sIMessage, String sSystemName)
        {
            Boolean isSuccess = false;

            try
            {
                NHapi.Model.V251.Message.OUL_R22 sRU_R01 = (NHapi.Model.V251.Message.OUL_R22)sIMessage;
                String? sResultTestCode = "";
                String? sResultTestType = "";
                String? sOperatorID = "";
                String? sPatientID = "";
                String? sPatientName = "";
                String? strObserveValue = "";
                bool sSerialNoExist = false;
                String? sSerialNo = "";
                String? sUniversalIdentifier = "";
                String? sTestResultStatus = "";
                Boolean isRangeReference = false;
                String? strResultObservStatus = "";
                String? sDoctorName = "";
                Decimal iResultValue = 0;
                DateTime dAnalysisDateTime = DateTime.MinValue;
                String sSpecies = "General";
                String? sAge = "";
                String? sWeight = "";

                var sSPMObj = new tbltestanalyze_results_specimen();
                var sSACObj = new tbltestanalyze_results_specimencontainer();

                if (sRU_R01.MSH.SendingApplication.NamespaceID != null && sRU_R01.MSH.SendingApplication.NamespaceID.Value != null)
                {
                    sSerialNo = sRU_R01.MSH.SendingApplication.NamespaceID.Value.Trim();
                }

                // --------------- Message Header --------------//
                tbltestanalyze_results_messageheader sMSHObj = new tbltestanalyze_results_messageheader
                {
                    FieldSeparator = sRU_R01.MSH.FieldSeparator.Value,
                    EncodingCharacters = sRU_R01.MSH.EncodingCharacters.Value,
                    SendingApplication = ((sRU_R01.MSH.SendingApplication.NamespaceID.Value != null) ? sRU_R01.MSH.SendingApplication.NamespaceID.Value.Trim() : "") + "^" +
                                         ((sRU_R01.MSH.SendingApplication.UniversalID.Value != null) ? sRU_R01.MSH.SendingApplication.UniversalID.Value.Trim() : "") + "^" +
                                         ((sRU_R01.MSH.SendingApplication.UniversalIDType.Value != null) ? sRU_R01.MSH.SendingApplication.UniversalIDType.Value.Trim() : ""),
                    SendingFacility = sRU_R01.MSH.SendingFacility.NamespaceID.Value,
                    ReceivingApplication = sRU_R01.MSH.ReceivingApplication.NamespaceID.Value,
                    ReceivingFacility = sRU_R01.MSH.ReceivingFacility.NamespaceID.Value,
                    DateTimeMessage = sRU_R01.MSH.DateTimeOfMessage.Time.ToString(),
                    MessageType = sRU_R01.MSH.MessageType.MessageCode.Value + "^" +
                                  sRU_R01.MSH.MessageType.TriggerEvent.Value + "^" +
                                  sRU_R01.MSH.MessageType.MessageStructure.Value,
                    MessageControlID = sRU_R01.MSH.MessageControlID.Value.ToString(),
                    ProcessingID = sRU_R01.MSH.ProcessingID.ProcessingID.Value,
                    VersionID = sRU_R01.MSH.VersionID.VersionID.Value,
                    AcceptAckmgtType = sRU_R01.MSH.AcceptAcknowledgmentType.Value,
                    AppAckmgtType = sRU_R01.MSH.ApplicationAcknowledgmentType.Value,
                    CountryCode = sRU_R01.MSH.CountryCode.Value,
                    CharacterSet = (sRU_R01.MSH.GetCharacterSet().Length > 0) ? sRU_R01.MSH.GetCharacterSet().FirstOrDefault().Value : null,
                    PrincipalLanguageMsg = sRU_R01.MSH.PrincipalLanguageOfMessage.Identifier.Value + "^" +
                                           sRU_R01.MSH.PrincipalLanguageOfMessage.Text.Value + "^" +
                                           sRU_R01.MSH.PrincipalLanguageOfMessage.NameOfCodingSystem.Value,
                    MessageProfileIdentifier = (sRU_R01.MSH.GetMessageProfileIdentifier().Length > 0) ?
                                               sRU_R01.MSH.GetMessageProfileIdentifier().FirstOrDefault().EntityIdentifier.Value + "^" +
                                               sRU_R01.MSH.GetMessageProfileIdentifier().FirstOrDefault().NamespaceID.Value + "^" +
                                               sRU_R01.MSH.GetMessageProfileIdentifier().FirstOrDefault().UniversalID.Value + "^" +
                                               sRU_R01.MSH.GetMessageProfileIdentifier().FirstOrDefault().UniversalIDType.Value : null
                };

                // ------------ Patient Identification --------------------//
                List<tbltestanalyze_results_patientidentification> sPIDObj = new List<tbltestanalyze_results_patientidentification>();

                tbltestanalyze_results_patientidentification sPID = new tbltestanalyze_results_patientidentification();
                if (sRU_R01.PATIENT.PID.PatientIdentifierListRepetitionsUsed > 0)
                {
                    sPID.SetID = sRU_R01.PATIENT.PID.SetIDPID.Value;
                    sPID.PatientID = sRU_R01.PATIENT.PID.PatientID.IDNumber.Value;
                    sPID.AlternatePatientID = (sRU_R01.PATIENT.PID.GetAlternatePatientIDPID().Length > 0) ?
                                               sRU_R01.PATIENT.PID.GetAlternatePatientIDPID().FirstOrDefault().IDNumber.ToString() : null;

                    sPID.PatientIdentifierList = (sRU_R01.PATIENT.PID.GetPatientIdentifierList().Length > 0) ?
                                                 sRU_R01.PATIENT.PID.GetPatientIdentifierList().FirstOrDefault().IDNumber.ToString() : null;

                    /*
                    if (sRU_R01.PATIENT.PID.SpeciesCode != null && string.IsNullOrEmpty(sRU_R01.PATIENT.PID.SpeciesCode.Text.ToString()))
                    {
                        sSpecies = sRU_R01.PATIENT.PID.SpeciesCode.Text.ToString(); 
                    }
                    */

                    if (sRU_R01.PATIENT.PID.SpeciesCode != null && !string.IsNullOrEmpty(sRU_R01.PATIENT.PID.SpeciesCode.Identifier.Value))
                    {
                        sSpecies = sRU_R01.PATIENT.PID.SpeciesCode.Identifier.Value;
                    }


                    if (String.IsNullOrEmpty(sPID.PatientID) && !String.IsNullOrEmpty(sPID.PatientIdentifierList))
                    {
                        sPatientID = sPID.PatientIdentifierList;
                    }
                    else
                    {
                        sPatientID = sPID.PatientID;
                    }
                }


                if (sRU_R01.PATIENT.PID.PatientNameRepetitionsUsed > 0)
                {
                    var sNameObj = sRU_R01.PATIENT.PID.GetPatientName().FirstOrDefault();

                    if (sNameObj != null)
                    {
                        sPID.PatientName = sNameObj.FamilyName.Surname.Value + "^" +
                                           sNameObj.GivenName + "^" +
                                           sNameObj.SecondAndFurtherGivenNamesOrInitialsThereof + "^" +
                                           sNameObj.SuffixEgJRorIII + "^" +
                                           sNameObj.PrefixEgDR + "^" +
                                           sNameObj.DegreeEgMD + "^" +
                                           sNameObj.NameTypeCode;

                        sPatientName = sNameObj.FamilyName.Surname.Value + " " + sNameObj.GivenName.Value;

                        if (sPID.PatientName.Replace("^", "").Length == 0)
                        {
                            sPID.PatientName = "";
                        }
                    }
                    else
                    {
                        sPID.PatientName = "";
                    }
                }
                sPIDObj.Add(sPID);

                //----------------- Patient Visit Record ---------------------//
                var sPVObj = new tbltestanalyze_results_patientvisit();

                if (sRU_R01.VISIT != null)
                {
                    var sPatientVisitation1 = sRU_R01.VISIT.PV1;
                    var sPatientVisitation2 = sRU_R01.VISIT.PV2;

                    sPVObj.SetID = sPatientVisitation1.SetIDPV1.Value;
                    sPVObj.PatientClass = sPatientVisitation1.PatientClass.Value;
                    sPVObj.AssignedPatientLocation = sPatientVisitation1.AssignedPatientLocation.Room.Value;
                    sPVObj.AdmissionType = sPatientVisitation1.AdmissionType.Value;
                    sPVObj.PreadmitNumber = sPatientVisitation1.PreadmitNumber.IDNumber.Value;
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
                    sPVObj.VisitNumber = sPatientVisitation1.VisitNumber.IDNumber.Value;

                    sDoctorName = sPVObj.AttendingDoctor;
                }

                // ------ Specimen --------//
                if (sRU_R01.GetSPECIMEN() != null)
                {
                    var sSpecimenObj = sRU_R01.GetSPECIMEN().SPM;

                    String sSpecimentID = "";
                    sSpecimentID = sSpecimenObj.SpecimenID.PlacerAssignedIdentifier.EntityIdentifier.Value + "^" +
                                   sSpecimenObj.SpecimenID.FillerAssignedIdentifier.EntityIdentifier.Value;
                    if (sSpecimentID.Replace("^", "").Length == 0)
                    {
                        sSpecimentID = "";
                    }

                    String sSpecimentParentID = "";
                    if (sSpecimenObj.GetSpecimenParentIDs().Length > 0)
                    {
                        sSpecimentParentID = sSpecimenObj.GetSpecimenParentIDs().FirstOrDefault().PlacerAssignedIdentifier.EntityIdentifier.Value + "^" +
                                             sSpecimenObj.GetSpecimenParentIDs().FirstOrDefault().FillerAssignedIdentifier.EntityIdentifier.Value;
                        if (sSpecimentParentID.Replace("^", "").Length == 0)
                        {
                            sSpecimentParentID = "";
                        }
                    }

                    sSPMObj.SetID = sSpecimenObj.SetIDSPM.Value;
                    sSPMObj.SpecimenID = sSpecimentID;
                    sSPMObj.SpecimentParentID = sSpecimentParentID;
                    sSPMObj.SpecimenType = sSpecimenObj.SpecimenType.Identifier.Value;
                    sSPMObj.SpecimenTypeModifier = (sSpecimenObj.GetSpecimenTypeModifier().Length > 0) ?
                                                    sSpecimenObj.GetSpecimenTypeModifier().FirstOrDefault().Identifier.Value : null;
                    sSPMObj.SpecimenAdditives = (sSpecimenObj.GetSpecimenAdditives().Length > 0) ?
                                                 sSpecimenObj.GetSpecimenAdditives().FirstOrDefault().Identifier.Value : null;
                    sSPMObj.SpecimenCollectionMethod = (sSpecimenObj.SpecimenCollectionMethod.Identifier.Value != null) ?
                                                        sSpecimenObj.SpecimenCollectionMethod.Identifier.Value : null;
                    sSPMObj.SpecimenSourceSite = (sSpecimenObj.SpecimenSourceSite.Identifier.Value != null) ?
                                                  sSpecimenObj.SpecimenSourceSite.Identifier.Value : null;
                    sSPMObj.SpecimenSourceSiteModifier = (sSpecimenObj.GetSpecimenSourceSiteModifier().Length > 0) ?
                                                          sSpecimenObj.GetSpecimenSourceSiteModifier().FirstOrDefault().Identifier.Value : null;
                    sSPMObj.SpecimenCollectionSite = (sSpecimenObj.SpecimenCollectionSite.Identifier.Value != null) ?
                                                      sSpecimenObj.SpecimenCollectionSite.Identifier.Value : null;
                    sSPMObj.SpecimenRole = (sSpecimenObj.GetSpecimenRole().Length > 0) ?
                                            sSpecimenObj.GetSpecimenRole().FirstOrDefault().Identifier.Value : null;
                }

                if (sRU_R01.GetSPECIMEN().CONTAINERs.Count() > 0)
                {
                    var sContainerObj = sRU_R01.GetSPECIMEN().CONTAINERs.FirstOrDefault().SAC;
                    if (sContainerObj != null)
                    {
                        string sContainerStatus = "";
                        sContainerStatus = sContainerObj.ContainerStatus.Identifier.Value + "^" +
                                           sContainerObj.ContainerStatus.Text.Value + "^" +
                                           sContainerObj.ContainerStatus.NameOfCodingSystem;
                        if (sContainerStatus.Replace("^", "").Length == 0)
                        {
                            sContainerStatus = "";
                        }

                        String sCarrierType = "";
                        sCarrierType = sContainerObj.CarrierType.Identifier.Value + "^" +
                                       sContainerObj.CarrierType.Text.Value + "^" +
                                       sContainerObj.CarrierType.NameOfCodingSystem;
                        if (sCarrierType.Replace("^", "").Length == 0)
                        {
                            sCarrierType = "";
                        }

                        String sCarrierIdentifier = "";
                        sCarrierIdentifier = sContainerObj.CarrierIdentifier.EntityIdentifier + "^" +
                                             sContainerObj.CarrierIdentifier.NamespaceID + "^" +
                                             sContainerObj.CarrierIdentifier.UniversalID + "^" +
                                             sContainerObj.CarrierIdentifier.UniversalIDType;
                        if (sCarrierIdentifier.Replace("^", "").Length == 0)
                        {
                            sCarrierIdentifier = "";
                        }

                        String sAdditive = "";
                        if (sContainerObj.GetAdditive().Length > 0)
                        {
                            sAdditive = sContainerObj.GetAdditive().FirstOrDefault().Identifier + "^" +
                                        sContainerObj.GetAdditive().FirstOrDefault().Text.Value + "^" +
                                        sContainerObj.GetAdditive().FirstOrDefault().NameOfCodingSystem.Value;
                            if (sAdditive.Replace("^", "").Length == 0)
                            {
                                sAdditive = "";
                            }
                        }

                        String sCapType = "";
                        sCapType = sContainerObj.CapType.Identifier + "^" +
                                   sContainerObj.CapType.Text.Value + "^" +
                                   sContainerObj.CapType.NameOfCodingSystem.Value;
                        if (sCapType.Replace("^", "").Length == 0)
                        {
                            sCapType = "";
                        }

                        sSACObj.ExternalAccessionIdentifier = sContainerObj.ExternalAccessionIdentifier.EntityIdentifier.Value;
                        sSACObj.AccessionIdentifier = sContainerObj.AccessionIdentifier.EntityIdentifier.Value;
                        sSACObj.ContainerIdentifier = sContainerObj.ContainerIdentifier.EntityIdentifier.Value;
                        sSACObj.PrimaryContainerIdentifier = sContainerObj.PrimaryParentContainerIdentifier.EntityIdentifier.Value;
                        sSACObj.EquipmentContainerIdentifier = sContainerObj.EquipmentContainerIdentifier.EntityIdentifier.Value;
                        sSACObj.SpecimenSource = sContainerObj.SpecimenSource.SpecimenSourceNameOrCode.Identifier.Value;
                        sSACObj.RegistrationDateTime = sContainerObj.RegistrationDateTime.Time.Value;
                        sSACObj.ContainerStatus = sContainerStatus;
                        sSACObj.CarrierType = sCarrierType;
                        sSACObj.CarrierIdentifier = sCarrierIdentifier;
                        sSACObj.PositionInCarrier = sContainerObj.PositionInCarrier.Value1.ToString();
                        sSACObj.TrayTypeSAC = sContainerObj.TrayTypeSAC.Identifier.Value;
                        sSACObj.TrayIdentifier = sContainerObj.TrayIdentifier.EntityIdentifier.Value;
                        sSACObj.PositionInTray = sContainerObj.PositionInTray.Value1.ToString();
                        sSACObj.Location = (sContainerObj.GetLocation().Length > 0) ?
                                            sContainerObj.GetLocation().FirstOrDefault().Identifier.Value : null;
                        sSACObj.ContainerHeight = sContainerObj.ContainerHeight.Value;
                        sSACObj.ContainerDiameter = sContainerObj.ContainerDiameter.Value;
                        sSACObj.BarrierDelta = sContainerObj.BarrierDelta.Value;
                        sSACObj.BottomDelta = sContainerObj.BottomDelta.Value;
                        sSACObj.ContainerHeightDiamtrUnits = sContainerObj.ContainerHeightDiameterDeltaUnits.Text.ToString();
                        sSACObj.ContainerVolume = sContainerObj.ContainerVolume.Value;
                        sSACObj.AvailableSpecimenVolume = sContainerObj.AvailableSpecimenVolume.Value;
                        sSACObj.volumeUnits = sContainerObj.VolumeUnits.Text.ToString();
                        sSACObj.SeparatorType = sContainerObj.SeparatorType.Identifier.Value;
                        sSACObj.CapType = sCapType;
                        sSACObj.Additive = sAdditive;
                        sSACObj.SpecimenComponent = sContainerObj.SpecimenComponent.Identifier.Value;
                        sSACObj.DilutionFactor = sContainerObj.DilutionFactor.Description;
                        sSACObj.Treatment = sContainerObj.Treatment.Identifier.Value;
                        sSACObj.HemolysisIndex = sContainerObj.HemolysisIndex.Value;
                        sSACObj.HemolysisIndexUnits = sContainerObj.HemolysisIndexUnits.Text.ToString();
                        sSACObj.LipemiaIndex = sContainerObj.LipemiaIndex.Value;
                        sSACObj.LipemiaIndexUnits = sContainerObj.LipemiaIndexUnits.Identifier.Value;
                        sSACObj.IcterusIndex = sContainerObj.IcterusIndex.Value;
                        sSACObj.IcterusIndexUnits = sContainerObj.IcterusIndexUnits.Identifier.Value;
                    }

                }


                //----------------- Observation Request ----------------------//
                var sTestResultLst = new List<txn_testresults>();
                var sTestResultDetails = new List<txn_testresults_details>();
                var sNTEObj = new List<tbltestanalyze_results_notes>();
                var sOBXObjList = new List<tbltestanalyze_results_observationresult>();
                var sOBRObj = new tbltestanalyze_results_observationrequest();
                foreach (var observation in sRU_R01.SPECIMENs.FirstOrDefault().ORDERs)
                {
                    String sFillerOrdNum = "";
                    sFillerOrdNum = observation.OBR.FillerOrderNumber.EntityIdentifier.Value + "^" +
                                                observation.OBR.FillerOrderNumber.NamespaceID.Value + "^" +
                                                observation.OBR.FillerOrderNumber.UniversalID.Value + "^" +
                                                observation.OBR.FillerOrderNumber.UniversalIDType.Value;

                    if (sFillerOrdNum.Replace("^", "").Length == 0)
                    {
                        sFillerOrdNum = "";
                    }

                    sResultTestCode = observation.OBR.UniversalServiceIdentifier.Identifier.Value;
                    sResultTestType = observation.OBR.UniversalServiceIdentifier.Text.Value;
                    sUniversalIdentifier = observation.OBR.UniversalServiceIdentifier.Text.Value;

                    sOBRObj.SetID = observation.OBR.SetIDOBR.Value;
                    sOBRObj.PlacerOrderNumber = observation.OBR.PlacerOrderNumber.EntityIdentifier.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.NamespaceID.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.UniversalID.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.UniversalIDType.Value;
                    sOBRObj.FillerOrderNumber = sFillerOrdNum;
                    sOBRObj.UniversalServIdentifier = observation.OBR.UniversalServiceIdentifier.Identifier.Value + "^" +
                                                      observation.OBR.UniversalServiceIdentifier.Text.Value + "^" +
                                                      observation.OBR.UniversalServiceIdentifier.NameOfCodingSystem.Value;
                    sOBRObj.Priority = observation.OBR.PriorityOBR.Value;
                    sOBRObj.RequestedDateTime = (observation.OBR.RequestedDateTime.Time.Value != null) ?
                                                 observation.OBR.RequestedDateTime.Time.Value.ToString().Trim() : null;
                    sOBRObj.ObservationDateTime = (observation.OBR.ObservationDateTime.Time.Value != null) ?
                                                  observation.OBR.ObservationDateTime.Time.Value.ToString().Trim() : null;
                    sOBRObj.ObservationEndDateTime = (observation.OBR.ObservationEndDateTime.Time.Value != null) ?
                                                      observation.OBR.ObservationEndDateTime.Time.Value.ToString().Trim() : null;
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
                    foreach (var observationDetail in observation.RESULTs)
                    {
                        String sInterpretation = "";
                        String? sObservValue = "";
                        strObserveValue = "";
                        if (observationDetail.OBX.GetObservationValue().Count() > 0)
                        {
                            if (observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V251.Datatype.NA))
                            {
                                var sNAObject = observationDetail.OBX.GetObservationValue().FirstOrDefault().Data;

                                int iTotalComponent = sNAObject.ExtraComponents.NumComponents();
                                List<String> sVal = new List<String>();

                                // --- Get from Component ------ //
                                PropertyInfo[] props = sNAObject.GetType().GetProperties();
                                foreach (PropertyInfo p in props)
                                {
                                    if (p.PropertyType == typeof(NHapi.Model.V251.Datatype.NM))
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
                                }
                            }
                            else
                            {
                                if (observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V251.Datatype.CWE))
                                {
                                    var sCWEObject = observationDetail.OBX.GetObservationValue().FirstOrDefault().Data;

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
                                else
                                {
                                    sObservValue = observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.ToString();
                                }

                            }

                            strObserveValue = sObservValue;
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
                        if (observationDetail.OBX.GetEquipmentInstanceIdentifier().Length > 0)
                        {
                            sEquipmentIdentifier = observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().EntityIdentifier.Value + "^" +
                                                   observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().NamespaceID.Value + "^" +
                                                   observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().UniversalID.Value;
                            if (sEquipmentIdentifier.Replace("^", "").Length == 0)
                            {
                                sEquipmentIdentifier = "";
                            }

                            if (!string.IsNullOrEmpty(observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().NamespaceID.Value) && !sSerialNoExist)
                            {
                                sSerialNoExist = true;
                                sSerialNo = observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().NamespaceID.Value;

                                if (string.IsNullOrEmpty(Worker.MainModel.DeviceType))
                                {
                                    string deviceType;
                                    var device = TestResultRepository.GetDeviceByIPSerialNo(null, sSerialNo, out deviceType);

                                    Worker.MainModel.DeviceType = deviceType;
                                }
                            }
                        }

                        if (!String.IsNullOrEmpty(observation.OBR.ObservationDateTime.Time.ToString()))
                        {
                            if (observation.OBR.ObservationDateTime.Time.Value.ToString().Length == 14)
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observation.OBR.ObservationDateTime.Time.Value.ToString(), "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else if (observation.OBR.ObservationDateTime.Time.ToString().Length == 22)
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observation.OBR.ObservationDateTime.Time.Value.ToString(), "yyyyMMdd HH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observation.OBR.ObservationDateTime.Time.Value.ToString(), "yyyyMMddHHmmss-ffff", System.Globalization.CultureInfo.InvariantCulture);
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
                            ObservationDateTime = (observationDetail.OBX.DateTimeOfTheObservation.Time.Value != null) ?
                                                   observationDetail.OBX.DateTimeOfTheObservation.Time.Value.ToString() : null,
                            ProducerID = "",
                            ResponsibleObserver = sOperatorID,
                            ObservationMethod = (observationDetail.OBX.GetObservationMethod().Length > 0) ?
                                                observationDetail.OBX.GetObservationMethod().FirstOrDefault().Text.ToString() : null,
                            EquipmentInstanceIdentifier = sEquipmentIdentifier,
                            AnalysisDateTime = (observationDetail.OBX.DateTimeOfTheAnalysis.Time.Value != null) ?
                                                observationDetail.OBX.DateTimeOfTheAnalysis.Time.Value.ToString() : null

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

                        if (!String.IsNullOrEmpty(observationDetail.OBX.ReferencesRange.Value))
                        {
                            isRangeReference = true;
                        }

                        /*
                        if (!(observationDetail.OBX.ObservationIdentifier.Text.Value.ToLower() == "age") &&
                            !(observationDetail.OBX.ObservationIdentifier.Text.Value.ToLower() == "weight"))
                        {
                            sObservValue = sObservValue.Replace("*", "").Replace(" ", "");

                            String sStatus = General.ProcessObservationResultStatusValue(isRangeReference, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);

                            sTestResultDetails.Add(new txn_testresults_details
                            {
                                TestParameter = observationDetail.OBX.ObservationIdentifier.Text.Value.Replace("?", ""),
                                SubID = observationDetail.OBX.ObservationSubID.Value,
                                ProceduralControl = strResultObservStatus,
                                TestResultStatus = sStatus,
                                TestResultValue = sObservValue,
                                TestResultUnit = observationDetail.OBX.Units.Identifier.Value,
                                ReferenceRange = observationDetail.OBX.ReferencesRange.Value,
                                Interpretation = sInterpretation
                            });
                        }
                        */

                        string parameter = observationDetail.OBX.ObservationIdentifier.Text.Value;

                        sObservValue = sObservValue.Replace("*", "").Replace(" ", "");

                        if (parameter.Equals("Age", StringComparison.OrdinalIgnoreCase))
                        {
                            sAge = sObservValue;
                        }
                        else if (parameter.Equals("Weight", StringComparison.OrdinalIgnoreCase))
                        {
                            sWeight = sObservValue;
                        }
                        else
                        {
                            String sStatus = General.ProcessObservationResultStatusValue(
                                isRangeReference,
                                sObservValue,
                                observationDetail.OBX.ReferencesRange.Value,
                                iResultValue);

                            sTestResultDetails.Add(new txn_testresults_details
                            {
                                TestParameter = parameter.Replace("?", ""),
                                SubID = observationDetail.OBX.ObservationSubID.Value,
                                ProceduralControl = strResultObservStatus,
                                TestResultStatus = sStatus,
                                TestResultValue = sObservValue,
                                TestResultUnit = observationDetail.OBX.Units.Identifier.Value,
                                ReferenceRange = observationDetail.OBX.ReferencesRange.Value,
                                Interpretation = sInterpretation
                            });
                        }

                    }
                }

                String sOverallStatus = "Normal";
                if (sTestResultDetails != null)
                {
                    if ((sTestResultDetails.Where(x => x.TestResultStatus == "Positive").Count() > 0) ||
                        (sTestResultDetails.Where(x => x.TestResultStatus == "Invalid").Count() > 0))
                    {
                        sOverallStatus = "Abnormal";
                    }
                }

                sSerialNo = string.IsNullOrEmpty(sSerialNo) ? "VCheck C10" : sSerialNo;

                txn_testresults sTestResultObj = new txn_testresults();
                sTestResultObj.TestResultDateTime = dAnalysisDateTime;
                sTestResultObj.TestResultType = sResultTestType;
                sTestResultObj.OperatorID = sOperatorID;
                sTestResultObj.PatientID = sPatientID;
                sTestResultObj.InchargePerson = sDoctorName;
                sTestResultObj.OverallStatus = sOverallStatus;
                sTestResultObj.CreatedDate = DateTime.Now;
                sTestResultObj.CreatedBy = sSystemName;
                sTestResultObj.DeviceSerialNo = Worker.MainModel.DeviceSerialNum != null && sSerialNo == "VCheck C10" ? Worker.MainModel.DeviceSerialNum : sSerialNo;
                sTestResultObj.PMSFunction = "Visible";
                sTestResultObj.PatientName = sPatientName;
                sTestResultObj.Species = sSpecies;
                sTestResultObj.Age = sAge;
                sTestResultObj.Weight = sWeight;

                tbltestanalyze_results sResultObj = new tbltestanalyze_results
                {
                    MessageType = sRU_R01.MSH.MessageType.MessageStructure.Value,
                    MessageDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    CreatedBy = sSystemName
                };

                var parameters = sTestResultDetails.Select(x => x.TestParameter).ToList();

                VCheckAPI VcheckAPI = new VCheckAPI();

                ScheduledTestModel? sScheduledTestObj = new ScheduledTestModel();
                var clinic = TestResultRepository.GetConfigurationByKey("ClinicID");
                var PMS = TestResultRepository.GetConfigurationByKey("InterfaceSettingsPMS");
                if (clinic != null && !string.IsNullOrEmpty(clinic.ConfigurationValue) && PMS != null && PMS.ConfigurationValue != "None")
                {
                    //var scheduleString = await VcheckAPI.GetSchedule(clinic.ConfigurationValue, sTestResultObj.PatientID, parameters);
                    var scheduleString = await VcheckAPI.GetSchedule(clinic.ConfigurationValue, sTestResultObj.PatientID, sResultTestType);
                    sScheduledTestObj = string.IsNullOrEmpty(scheduleString) ? null : JsonConvert.DeserializeObject<ScheduledTestModel>(scheduleString);

                    if (sScheduledTestObj != null)
                    {
                        sTestResultObj.PatientName = string.IsNullOrEmpty(sPatientName) ? sScheduledTestObj.PatientName : sPatientName;
                        //sTestResultObj.PatientName = sScheduledTestObj.PatientName;
                    }
                }

                Boolean bResult = TestResultRepository.insertTestObservationMessage(sResultObj, sMSHObj, sPIDObj, sOBRObj, sOBXObjList, sNTEObj, sPVObj, sSPMObj, sSACObj);
                if (bResult)
                {
                    sTestResultObj.Analyze_TableRowID = sResultObj.ResultRowID;
                    // Insert into Test Result table & create notification 
                    sTestResultDetails = TestResultRepository.createTestResultsMultipleParam(sTestResultObj, sTestResultDetails, out sTestResultObj);

                    if (sScheduledTestObj != null && sScheduledTestObj.ID != 0)
                    {
                        if (string.IsNullOrEmpty(sResultTestCode) || double.TryParse(sResultTestCode, CultureInfo.InvariantCulture, out _))
                        {
                            var testResponseString = await VcheckAPI.GetTestByNameOrCode(sTestResultObj.TestResultType, null);
                            sResultTestCode = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testid;
                        }

                        PMSHandler pmsHandler = new PMSHandler();
                        var success = false;
                        success = await pmsHandler.SendToPMS(sTestResultObj, sTestResultDetails, sScheduledTestObj, sResultTestCode);

                        await VcheckAPI.UpdateScheduleStatus(sScheduledTestObj.LocationID, sScheduledTestObj.PatientID, sScheduledTestObj.ScheduleUniqueID.Split("-")[1], sScheduledTestObj.CreatedBy, success ? 4 : 3, sTestResultObj.TestResultType);

                        if (!success)
                        {
                            //txn_notification sNotificationSend = new txn_notification()
                            //{
                            //    NotificationType = "Schedule Error",
                            //    NotificationTitle = "Send to PMS Error",
                            //    NotificationContent = "Order test " + sResultTestType + " for " + sScheduledTestObj.PatientName + " failed to be sent to PIMS. Please send the order test manually to PIMS.",
                            //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            //    CreatedBy = sSystemName
                            //};

                            //TestResultRepository.insertNotification(sNotificationSend);

                            NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientName, sSystemName);
                        }
                    }
                    else if (PMS != null && !string.IsNullOrEmpty(PMS.ConfigurationValue) && PMS.ConfigurationValue != "None")
                    {
                        //txn_notification sNotificationSend = new txn_notification()
                        //{
                        //    NotificationType = "Schedule Error",
                        //    NotificationTitle = "Result Mismatch",
                        //    NotificationContent = "Order test " + sResultTestType + " for Patient ID : " + sPatientID + " failed to match with any schedule. Please match the order test manually.",
                        //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        //    CreatedBy = sSystemName
                        //};

                        //TestResultRepository.insertNotification(sNotificationSend);


                        NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientID, sSystemName);
                    }

                    if (PMS != null && PMS.ConfigurationValue != "None") { NotificationRepository.SendNotification(sTestResultObj.PatientID, sSystemName); }
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.Error("ProcessMessage >>> V251 >>> " + ex.ToString());

                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Get NTE Comment for HL7
        /// </summary>
        /// <param name="nte"></param>
        /// <returns></returns>
        public static String GenerateNTEComments(List<NHapi.Model.V251.Segment.NTE> nte)
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
                _logger.Error("GenerateNTEComments >>> V251 >>> " + ex.ToString());
            }

            return nteComment;
        }
    }
}
