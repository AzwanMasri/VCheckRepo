using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHapi.Base.Model;
using NHapi.Model.V26.Segment;
using NHapiTools.Base;
using NHapiTools.Model.V26.Message;
using ScottPlot;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheckListenerWorker.Lib.Models;
using VCheckListenerWorker.Lib.PMS;

namespace VCheckListenerWorker.Lib.Logic.HL7.V26
{
    public class HL7Repository
    {
        static VCheckListenerWorker.Lib.Util.Logger _logger = new VCheckListenerWorker.Lib.Util.Logger();

        public static VCheckAPI VcheckAPI = new VCheckAPI();
        public static PMSHandler pmsHandler = new PMSHandler();

        /// <summary>
        /// Process HL7 Message
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <param name="sSystemName"></param>
        /// <returns></returns>
        public static async Task<bool> ProcessMessageAsync(NHapi.Base.Model.IMessage sIMessage, String sSystemName)
        {
            Boolean isSuccess = false;
            String? sResultRule = "";
            String? sResultTestType = "";
            String? sResultTestCode = "";
            String? sOperatorID = "";
            String? sPatientID = "";
            String? sSerialNo = "";
            String? sUniversalIdentifier = "";
            String? sTestResultStatus = "";
            String? strResultObservStatus = "";
            Decimal iResultValue = 0;
            DateTime dAnalysisDateTime = DateTime.MinValue;
            string notes = "";
            String? sOverallStatus = "Normal";
            List<string> parameters;
            mst_configuration clinic;
            mst_configuration PMS;

            var sTestResultDetails = new List<txn_testresults_details>();
            tbltestanalyze_results_messageheader sMSHObj = new tbltestanalyze_results_messageheader();
            List<tbltestanalyze_results_patientidentification> sPIDObj = new List<tbltestanalyze_results_patientidentification>();
            var sOBRObj = new tbltestanalyze_results_observationrequest();
            var sNTEObj = new List<tbltestanalyze_results_notes>();
            var sOBXObjList = new List<tbltestanalyze_results_observationresult>();
            var sTestResultGraph = new List<txn_testresults_graphsExtended>();
            tbltestanalyze_results_patientidentification sPID = new tbltestanalyze_results_patientidentification();
            txn_testresults sTestResultObj = new txn_testresults();
            tbltestanalyze_results sResultObj;
            ScheduledTestModel? sScheduledTestObj = new ScheduledTestModel();

            NHapi.Model.V26.Message.ORU_R01 sRU_R01 = new NHapi.Model.V26.Message.ORU_R01();

            string[] StructureList;
            IStructure[] OBXStructure;
            IStructure[] OBRStructure;
            IStructure[] NTEStructure;

            try
            {
                sRU_R01 = (NHapi.Model.V26.Message.ORU_R01)sIMessage;

                StructureList = sRU_R01.Names;
                OBXStructure = StructureList.FirstOrDefault(x => x == "OBX") != null ? sRU_R01.GetAll("OBX") : null;
                OBRStructure = StructureList.FirstOrDefault(x => x == "OBR") != null ? sRU_R01.GetAll("OBR") : null;
                NTEStructure = StructureList.FirstOrDefault(x => x == "NTE") != null ? sRU_R01.GetAll("NTE") : null;


                if (sRU_R01.MSH.SendingApplication.NamespaceID != null && sRU_R01.MSH.SendingApplication.NamespaceID.Value != null)
                {
                    sSerialNo = sRU_R01.MSH.SendingApplication.NamespaceID.Value.Trim();
                }

                // --------------- Message Header --------------//
                sMSHObj = new tbltestanalyze_results_messageheader
                {
                    FieldSeparator = sRU_R01.MSH.FieldSeparator.Value,
                    EncodingCharacters = sRU_R01.MSH.EncodingCharacters.Value,
                    SendingApplication = ((sRU_R01.MSH.SendingApplication.NamespaceID.Value != null) ? sRU_R01.MSH.SendingApplication.NamespaceID.Value.Trim() : "") + "^" +
                                         ((sRU_R01.MSH.SendingApplication.UniversalID.Value != null) ? sRU_R01.MSH.SendingApplication.UniversalID.Value.Trim() : "") + "^" +
                                         ((sRU_R01.MSH.SendingApplication.UniversalIDType.Value != null) ? sRU_R01.MSH.SendingApplication.UniversalIDType.Value.Trim() : ""),
                    SendingFacility = sRU_R01.MSH.SendingFacility.NamespaceID.Value,
                    ReceivingApplication = sRU_R01.MSH.ReceivingApplication.NamespaceID.Value,
                    ReceivingFacility = sRU_R01.MSH.ReceivingFacility.NamespaceID.Value,
                    DateTimeMessage = sRU_R01.MSH.DateTimeOfMessage.Value,
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



                sPID = new tbltestanalyze_results_patientidentification();
                if (sRU_R01.GetPATIENT_RESULT().PATIENT.PID.PatientIdentifierListRepetitionsUsed > 0)
                {
                    sPID.SetID = sRU_R01.GetPATIENT_RESULT().PATIENT.PID.SetIDPID.Value;
                    sPID.PatientID = sRU_R01.GetPATIENT_RESULT().PATIENT.PID.PatientID.IDNumber.Value;
                    sPID.AlternatePatientID = (sRU_R01.GetPATIENT_RESULT().PATIENT.PID.GetAlternatePatientIDPID().Length > 0) ?
                                               sRU_R01.GetPATIENT_RESULT().PATIENT.PID.GetAlternatePatientIDPID().FirstOrDefault().IDNumber.ToString() : null;

                    sPID.PatientIdentifierList = (sRU_R01.GetPATIENT_RESULT().PATIENT.PID.GetPatientIdentifierList().Length > 0) ?
                                                 sRU_R01.GetPATIENT_RESULT().PATIENT.PID.GetPatientIdentifierList().FirstOrDefault().IDNumber.ToString() : null;


                    if (String.IsNullOrEmpty(sPID.PatientID) && !String.IsNullOrEmpty(sPID.PatientIdentifierList))
                    {
                        sPatientID = sPID.PatientIdentifierList;
                    }
                    else
                    {
                        sPatientID = sPID.PatientID;
                    }
                }

                if (sRU_R01.GetPATIENT_RESULT().PATIENT.PID.PatientNameRepetitionsUsed > 0)
                {
                    var sNameObj = sRU_R01.GetPATIENT_RESULT().PATIENT.PID.GetPatientName().FirstOrDefault();

                    if (sNameObj != null)
                    {
                        sPID.PatientName = "" + "^" +
                                           sNameObj.GivenName + "^" +
                                           sNameObj.SecondAndFurtherGivenNamesOrInitialsThereof + "^" +
                                           sNameObj.SuffixEgJRorIII + "^" +
                                           sNameObj.PrefixEgDR + "^" +
                                           sNameObj.DegreeEgMD + "^" +
                                           sNameObj.NameTypeCode;

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

                //----------------- Observation Request ----------------------//
                if (OBRStructure != null)
                {
                    var SubID = "";
                    var ResultStatus = "";
                    var ResultValue = "";
                    var ReferenceRange = "";
                    var ResultUnit = "";
                    var ProceduralControl = "";

                    var obr = (OBR)OBRStructure.FirstOrDefault();
                    sResultTestCode = obr.UniversalServiceIdentifier.Identifier.Value;
                    sResultTestType = obr.UniversalServiceIdentifier.Text.Value;


                    if (!String.IsNullOrEmpty(obr.ObservationEndDateTime.Value))
                    {
                        if (obr.ObservationEndDateTime.Value.Length == 14)
                        {
                            dAnalysisDateTime = DateTime.ParseExact(obr.ObservationEndDateTime.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else if (obr.ObservationEndDateTime.Value.Length == 22)
                        {
                            dAnalysisDateTime = DateTime.ParseExact(obr.ObservationEndDateTime.Value, "yyyyMMdd HH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            dAnalysisDateTime = DateTime.ParseExact(obr.ObservationEndDateTime.Value, "yyyyMMddHHmmss-ffff", System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }

                    obr = null;

                    List<(string, string, double[])> graphDatas = new List<(string, string, double[])>();
                    int totalXData = 0;

                    foreach (var obxTemp in OBXStructure)
                    {
                        var obx = (OBX)obxTemp;

                        String? sObservValue = "";

                        if (obx.GetObservationValue().Count() > 0)
                        {
                            var SubIDSplit = obx.ObservationSubID.Value.Split(".");

                            if (SubIDSplit.Count() == 3)
                            {
                                SubID = obx.ObservationSubID.Value;
                                ResultStatus = obx.GetObservationValue().FirstOrDefault().Data.ToString();
                                ReferenceRange = obx.ReferencesRange.Value;
                            }
                            else
                            {
                                if (SubIDSplit[3] == "1")
                                {
                                    if (obx.ObservationResultStatus.Value == "F")
                                    {
                                        strResultObservStatus = "Valid";
                                    }
                                    else if (obx.ObservationResultStatus.Value == "X")
                                    {
                                        strResultObservStatus = "Invalid";
                                    }

                                    ProceduralControl = strResultObservStatus;
                                    //ResultUnit = obx.Units.Identifier.Value;
                                }
                                else if (SubIDSplit[3] == "2")
                                {
                                    ResultValue = obx.GetObservationValue().FirstOrDefault().Data.ToString();
                                    ResultUnit = obx.Units.Identifier.Value;
                                }
                                else if (SubIDSplit[3] == "3")
                                {
                                    var components = (obx.GetObservationValue().FirstOrDefault().Data as NHapi.Model.V26.Datatype.NA);
                                    var extraComponents = components.ExtraComponents;
                                    double benchmark = double.Parse(components.Value1.ToString(), CultureInfo.InvariantCulture);
                                    double[] doubleArray = new double[extraComponents.NumComponents() + 4];
                                    int currentElement = 0;
                                    totalXData = (extraComponents.NumComponents() + 4) > totalXData ? extraComponents.NumComponents() + 4 : totalXData;
                                    var parameterName = obx.ObservationIdentifier.Text.Value.Replace(sResultTestType, "").TrimStart();

                                    if (string.IsNullOrEmpty(parameterName)) { parameterName = obx.ObservationIdentifier.Text.Value; }

                                    foreach (var component in components.Components)
                                    {
                                        doubleArray[currentElement++] = (double.Parse(component.ToString(), CultureInfo.InvariantCulture) - benchmark) / 1000;
                                    }

                                    for (int i = 0; i < extraComponents.NumComponents(); i++)
                                    {
                                        doubleArray[currentElement++] = (double.Parse(extraComponents.GetComponent(i).Data.ToString(), CultureInfo.InvariantCulture) - benchmark) / 1000;
                                    }

                                    var ICValue = "";

                                    if(ResultStatus == "Positive") { ICValue = "(+/"+ ResultValue + ")"; }
                                    else { ICValue = "(-/" + ResultValue + ")"; }

                                    graphDatas.Add((parameterName, ICValue, doubleArray));

                                    if (obx.ObservationIdentifier.Text.Value.Contains("IC"))
                                    {

                                        // Create a new 600x400 pixel plot
                                        var plt = new Plot();
                                        plt.HideGrid();
                                        double[] x = ScottPlot.Generate.Consecutive(totalXData);
                                        string imageString = null;
                                        bool isInvalid = graphDatas.Where(x => x.Item2 == "(+/0.00)" || x.Item2 == "(-/0.00)").Count() == graphDatas.Count();

                                        if (isInvalid)
                                        {
                                            var txt = plt.Add.Text("Invalid", 0, 0);
                                            txt.Alignment = Alignment.MiddleCenter; // Center alignment
                                            txt.LabelFontSize = 34;
                                            txt.LabelFontColor = Colors.Red;
                                            plt.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.EmptyTickGenerator();
                                            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.EmptyTickGenerator();
                                        }
                                        else
                                        {
                                            foreach (var graphData in graphDatas)
                                            {
                                                var line = plt.Add.SignalXY(x, graphData.Item3);
                                                line.LegendText = graphData.Item1;
                                            }

                                            // Customize chart
                                            plt.Legend.Alignment = Alignment.UpperLeft;
                                            plt.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.EmptyTickGenerator();
                                            plt.ShowLegend();
                                            plt.Axes.AutoScale();

                                            var xLimits = plt.Axes.GetLimits().XRange;
                                            var yLimits = plt.Axes.GetLimits().YRange;
                                            plt.Axes.SetLimitsY(yLimits.Min, yLimits.Max * 2);
                                            plt.Axes.SetLimitsX(0, 40);

                                        }

                                        int width = 1200;
                                        int height = 400;
                                        int tableWidth = 600;

                                        Bitmap finalImage = new Bitmap(width, height);
                                        using Graphics g = Graphics.FromImage(finalImage);
                                        g.Clear(System.Drawing.Color.White);

                                        DrawTable(g, sResultTestType, graphDatas, tableWidth, height);
                                        Bitmap chart = null;

                                        using (var ms = new MemoryStream(plt.GetImageBytes(600, 400)))
                                        {
                                            // Ensure the stream is at the beginning
                                            ms.Position = 0;
                                            chart = new Bitmap(ms);
                                        }

                                        //Bitmap chart = new Bitmap;
                                        g.DrawImage(chart, tableWidth, 0);

                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            // Save the bitmap to the memory stream in the given format
                                            finalImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                            imageString = Convert.ToBase64String(ms.ToArray());
                                        }

                                        sTestResultGraph.Add(new txn_testresults_graphsExtended() { FileName = graphDatas.FirstOrDefault().Item1 + ".png", Base64String = imageString });

                                        parameterName = graphDatas.FirstOrDefault().Item1 + " IC";

                                        graphDatas.Clear();

                                        ResultStatus = ResultStatus == "Positive" ? "Valid" : "Invalid";

                                    }

                                    sTestResultDetails.Add(new txn_testresults_details
                                    {
                                        TestParameter = parameterName,
                                        SubID = SubID,
                                        ProceduralControl = ProceduralControl,
                                        TestResultStatus = ResultStatus,
                                        TestResultValue = ResultValue,
                                        TestResultUnit = (!String.IsNullOrEmpty(ResultUnit)) ? ResultUnit : "",
                                        ReferenceRange = ReferenceRange
                                    });


                                }
                            }

                            if (obx.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.TX))
                            {
                                sTestResultStatus = obx.GetObservationValue().FirstOrDefault().Data.ToString();
                            }

                            if (obx.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.NA))
                            {
                                var sNAObject = obx.GetObservationValue().FirstOrDefault().Data;

                                int iTotalComponent = sNAObject.ExtraComponents.NumComponents();
                                List<String> sVal = new List<String>();

                                // --- Get from Component ------ //
                                PropertyInfo[] props = sNAObject.GetType().GetProperties();
                                foreach (PropertyInfo p in props)
                                {
                                    if (p.PropertyType == typeof(NHapi.Model.V26.Datatype.NM))
                                    {
                                        sVal.Add(p.GetValue(sNAObject, null).ToString());
                                    }
                                    ;
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
                                if (obx.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.CWE))
                                {
                                    var sCWEObject = obx.GetObservationValue().FirstOrDefault().Data;

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
                                    sObservValue = obx.GetObservationValue().FirstOrDefault().Data.ToString();
                                }

                            }
                        }

                        String sUnitValue = "";
                        sUnitValue = obx.Units.Identifier.Value + "^" +
                                     obx.Units.Text.Value + "^" +
                                     obx.Units.NameOfCodingSystem;

                        if (sUnitValue.Replace("^", "").Length == 0)
                        {
                            sUnitValue = "";
                        }

                        String sObservIdentifier = "";
                        sObservIdentifier = obx.ObservationIdentifier.Identifier.Value + "^" +
                                            obx.ObservationIdentifier.Text.Value + "^" +
                                            obx.ObservationIdentifier.NameOfCodingSystem.Value + "^" +
                                            obx.ObservationIdentifier.AlternateIdentifier.Value;

                        if (sObservIdentifier.Replace("^", "").Length == 0)
                        {
                            sObservIdentifier = "";
                        }

                        sOBXObjList.Add(new tbltestanalyze_results_observationresult
                        {
                            SetID = obx.SetIDOBX.Value,
                            ValueType = obx.ValueType.Value,
                            ObservationIdentifier = sObservIdentifier,
                            ObservationSubID = obx.ObservationSubID.Value,
                            ObservationValue = sObservValue,
                            Units = sUnitValue,
                            ReferencesRange = obx.ReferencesRange.Value,
                            AbnormalFlag = (obx.GetAbnormalFlags().Length > 0) ?
                                            obx.GetAbnormalFlags().FirstOrDefault().Value : null,
                            ObservationResultStatus = obx.ObservationResultStatus.Value,
                            ObservationDateTime = obx.DateTimeOfTheObservation.Value,
                            ProducerID = obx.ProducerSID.Text.Value,
                            ResponsibleObserver = sOperatorID,
                            ObservationMethod = (obx.GetObservationMethod().Length > 0) ?
                                                 obx.GetObservationMethod().FirstOrDefault().Text.ToString() : null,
                            EquipmentInstanceIdentifier = (obx.GetEquipmentInstanceIdentifier().Length > 0) ?
                                                           obx.GetEquipmentInstanceIdentifier().FirstOrDefault().EntityIdentifier.Value : null,
                            AnalysisDateTime = obx.DateTimeOfTheAnalysis.Value

                        });

                        obx = null;
                    }

                    if (NTEStructure != null)
                    {
                        var nteTemp = (NTE)NTEStructure.FirstOrDefault();

                        sNTEObj.Add(new tbltestanalyze_results_notes
                        {
                            SetID = "1",
                            Segment = "NTE",
                            SourceComment = nteTemp.SourceOfComment.Value,
                            Comment = nteTemp.GetComment(1).Value,
                        });

                        notes = nteTemp.GetComment(1).Value;

                        nteTemp = null;
                    }
                }
                else
                {
                    foreach (var observation in sRU_R01.PATIENT_RESULTs.FirstOrDefault().ORDER_OBSERVATIONs)
                    {
                        sResultTestCode = observation.OBR.UniversalServiceIdentifier.Identifier.Value;
                        sResultTestType = observation.OBR.UniversalServiceIdentifier.Text.Value;
                        sUniversalIdentifier = observation.OBR.UniversalServiceIdentifier.Text.Value;

                        sOBRObj.SetID = observation.OBR.SetIDOBR.Value;
                        sOBRObj.PlacerOrderNumber = observation.OBR.PlacerOrderNumber.EntityIdentifier.Value + "^" +
                                                    observation.OBR.PlacerOrderNumber.NamespaceID.Value + "^" +
                                                    observation.OBR.PlacerOrderNumber.UniversalID.Value + "^" +
                                                    observation.OBR.PlacerOrderNumber.UniversalIDType.Value;
                        sOBRObj.FillerOrderNumber = observation.OBR.FillerOrderNumber.EntityIdentifier.Value + "^" +
                                                    observation.OBR.FillerOrderNumber.NamespaceID.Value + "^" +
                                                    observation.OBR.FillerOrderNumber.UniversalID.Value + "^" +
                                                    observation.OBR.FillerOrderNumber.UniversalIDType.Value;
                        sOBRObj.UniversalServIdentifier = observation.OBR.UniversalServiceIdentifier.Identifier.Value + "^" +
                                                          observation.OBR.UniversalServiceIdentifier.Text.Value + "^" +
                                                          observation.OBR.UniversalServiceIdentifier.NameOfCodingSystem.Value;
                        sOBRObj.Priority = observation.OBR.Priority.Value;
                        sOBRObj.RequestedDateTime = observation.OBR.RequestedDateTime.Value;
                        sOBRObj.ObservationDateTime = observation.OBR.ObservationDateTime.Value.Trim();
                        sOBRObj.ObservationEndDateTime = observation.OBR.ObservationEndDateTime.Value.Trim();
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
                                notes = sTestResultStatus;
                            }
                        }

                        // --------------- Observation Results ----------------//
                        foreach (var observationDetail in observation.OBSERVATIONs)
                        {
                            String sInterpretation = "";
                            String? sObservValue = "";

                            if (observationDetail.OBX.GetObservationValue().Count() > 0)
                            {
                                if (observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.NA))
                                {
                                    var sNAObject = observationDetail.OBX.GetObservationValue().FirstOrDefault().Data;

                                    int iTotalComponent = sNAObject.ExtraComponents.NumComponents();
                                    List<String> sVal = new List<String>();

                                    // --- Get from Component ------ //
                                    PropertyInfo[] props = sNAObject.GetType().GetProperties();
                                    foreach (PropertyInfo p in props)
                                    {
                                        if (p.PropertyType == typeof(NHapi.Model.V26.Datatype.NM))
                                        {
                                            sVal.Add(p.GetValue(sNAObject, null).ToString());
                                        }
                                        ;
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
                                    if (observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.CWE))
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

                                //strObserveValue = sObservValue;
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

                            if (!String.IsNullOrEmpty(observationDetail.OBX.DateTimeOfTheAnalysis.Value))
                            {
                                if (observationDetail.OBX.DateTimeOfTheAnalysis.Value.Length == 14)
                                {
                                    dAnalysisDateTime = DateTime.ParseExact(observationDetail.OBX.DateTimeOfTheAnalysis.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else if (observationDetail.OBX.DateTimeOfTheAnalysis.Value.Length == 22)
                                {
                                    dAnalysisDateTime = DateTime.ParseExact(observationDetail.OBX.DateTimeOfTheAnalysis.Value, "yyyyMMdd HH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    dAnalysisDateTime = DateTime.ParseExact(observationDetail.OBX.DateTimeOfTheAnalysis.Value, "yyyyMMddHHmmss-ffff", System.Globalization.CultureInfo.InvariantCulture);
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
                                ObservationDateTime = observationDetail.OBX.DateTimeOfTheObservation.Value,
                                ProducerID = observationDetail.OBX.ProducerSID.Text.Value,
                                ResponsibleObserver = sOperatorID,
                                ObservationMethod = (observationDetail.OBX.GetObservationMethod().Length > 0) ?
                                                     observationDetail.OBX.GetObservationMethod().FirstOrDefault().Text.ToString() : null,
                                EquipmentInstanceIdentifier = (observationDetail.OBX.GetEquipmentInstanceIdentifier().Length > 0) ?
                                                               observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().EntityIdentifier.Value : null,
                                AnalysisDateTime = observationDetail.OBX.DateTimeOfTheAnalysis.Value

                            });

                            // ------------ Notes -------------------//
                            if (observationDetail.NTEs.Count() > 0)
                            {
                                String sComment = GenerateNTEComments(observationDetail.NTEs.ToList());

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
                                    if (sResultTestType.ToLower() == "cav ab")
                                    {
                                        var sCWEOBXValue = observationDetail.OBX.GetObservationValue();
                                        if (sCWEOBXValue.Length > 0)
                                        {
                                            NHapi.Model.V26.Datatype.CWE sVNData = (NHapi.Model.V26.Datatype.CWE)sCWEOBXValue[0].Data;
                                            if (sVNData != null)
                                            {
                                                String sCWEValue = sVNData.NameOfCodingSystem.Value;
                                                if (sCWEValue.ToLower() == "invalid")
                                                {
                                                    sTestResultStatus = sCWEValue;

                                                    String sValue = "";
                                                    String[] strArryValue = sComment.Split(",");
                                                    if (strArryValue.Length > 0)
                                                    {
                                                        sValue = strArryValue[1].Replace("Value=", "").Trim();
                                                    }

                                                    Decimal.TryParse(sValue, CultureInfo.InvariantCulture, out iResultValue);
                                                    sObservValue = sValue;
                                                }
                                                else
                                                {
                                                    String[] arrayCWE = sCWEValue.Split(",");
                                                    if (arrayCWE.Length > 0)
                                                    {
                                                        sTestResultStatus = arrayCWE[0].Trim();

                                                        String[] arrayValue = arrayCWE[1].Split("VN");
                                                        if (arrayValue.Length > 0)
                                                        {
                                                            sResultRule = "VN";
                                                            sObservValue = arrayValue[1].Trim();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        sResultRule = "COI";

                                        String sValue = "";
                                        String[] strArryValue = sComment.Split(",");
                                        if (strArryValue.Length > 0)
                                        {
                                            sValue = strArryValue[1].Replace("Value=", "").Trim();
                                        }

                                        Decimal.TryParse(sValue, CultureInfo.InvariantCulture, out iResultValue);
                                        sObservValue = sValue;
                                    }
                                }

                                if (sComment.ToLower().Contains("interpretation"))
                                {
                                    String[] strArryValue = sComment.Split("=");
                                    if (strArryValue.Length > 0 && strArryValue[1].Trim() != "Not Used")
                                    {
                                        sInterpretation = strArryValue[1].Trim();
                                        sTestResultStatus = strArryValue[1].Trim();
                                    }
                                    else
                                    {
                                        sTestResultStatus = "Normal";
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


                            if (sResultTestType.ToLower() != "babesia gibsoni/canis" && sResultTestType.ToLower() != "canine diarrhea 8 panel")
                            {
                                string referenceRange = observationDetail.OBX.ReferencesRange.Value;
                                string measuringRange = "";
                                if (sResultTestType.ToLower() != "cav ab")
                                {
                                    if (sResultRule == "COI")
                                    {
                                        sTestResultStatus = General.ProcessObservationResultStatusValue(false, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);
                                    }
                                    else
                                    {
                                        sTestResultStatus = General.ProcessObservationResultStatusValueReferenceRange("V200", sObservValue, sResultTestType, "General", "General", out referenceRange, out measuringRange);

                                        if (string.IsNullOrEmpty(sTestResultStatus))
                                        {
                                            sTestResultStatus = General.ProcessObservationResultStatusValue(true, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);

                                            if (!string.IsNullOrEmpty(sTestResultStatus))
                                            {
                                                referenceRange = observationDetail.OBX.ReferencesRange.Value;
                                            }
                                        }
                                    }
                                    //sTestResultStatus = General.ProcessObservationResultStatusValue(isRangeReference, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);
                                }

                                sTestResultDetails.Add(new txn_testresults_details
                                {
                                    TestParameter = observationDetail.OBX.ObservationIdentifier.Text.Value.Replace("?", ""),
                                    SubID = observationDetail.OBX.ObservationSubID.Value,
                                    ProceduralControl = strResultObservStatus,
                                    TestResultStatus = sTestResultStatus,
                                    TestResultValue = sObservValue,
                                    TestResultUnit = (!String.IsNullOrEmpty(observationDetail.OBX.Units.Identifier.Value)) ? observationDetail.OBX.Units.Identifier.Value : sResultRule,
                                    ReferenceRange = referenceRange,
                                    MeasuringRange = measuringRange,
                                    Interpretation = sInterpretation
                                });
                            }
                        }
                    }
                }

                //if (sResultTestType.ToLower() == "babesia gibsoni/canis")
                //{
                //    sTestResultDetails = ProcessBabesiaGibsoniTestResult(sOBXObjList);
                //}

                sOverallStatus = "Normal";
                if (sTestResultDetails != null)
                {
                    if (sTestResultDetails.Where(x => x.TestParameter.ToLower() == "cav ab").Count() > 0)
                    {
                        sOverallStatus = sTestResultDetails[0].TestResultStatus;
                    }
                    else
                    {
                        if (sTestResultDetails.Where(x => x.TestResultStatus == "Positive").Count() > 0 && sTestResultDetails.Where(x => x.TestParameter.Contains(" IC")).Count() == 0)
                        {
                            sOverallStatus = "Abnormal";
                        }
                        else if (sTestResultDetails.Where(x => x.TestResultStatus == "Invalid").Count() > 0)
                        {
                            if(sTestResultDetails.Where(x => x.TestParameter.Contains(" IC")).Count() > 0)
                            {
                                sOverallStatus = "IC Invalid";
                            }
                            else
                            {
                                sOverallStatus = "Invalid";
                            }
                        }
                        else if (sTestResultDetails.Where(x => x.TestResultStatus == "Abnormal").Count() > 0)
                        {
                            sOverallStatus = "Abnormal";
                        }
                        else if (sTestResultDetails.Where(x => x.TestResultStatus == "Normal").Count() == sTestResultDetails.Count())
                        {
                            sOverallStatus = "Normal";
                        }
                        else if (sTestResultDetails.Where(x => x.TestParameter.Contains(" IC")).Count() > 1 && sTestResultDetails.Where(x => x.TestResultStatus == "Valid" && x.TestParameter.Contains(" IC")).Count() == sTestResultDetails.Where(x => x.TestParameter.Contains(" IC")).Count())
                        {
                            sOverallStatus = "IC Valid";
                        }
                        else
                        {
                            var sStatus = sTestResultDetails.FirstOrDefault(x => !string.IsNullOrEmpty(x.TestResultStatus));
                            sOverallStatus = sStatus != null ? sStatus.TestResultStatus : "Error";
                        }
                    }

                }

                sTestResultObj.TestResultDateTime = dAnalysisDateTime;
                sTestResultObj.TestResultType = sResultTestType;
                sTestResultObj.OperatorID = sOperatorID;
                sTestResultObj.PatientID = sPatientID;
                sTestResultObj.InchargePerson = "";
                sTestResultObj.OverallStatus = sOverallStatus;
                sTestResultObj.CreatedDate = DateTime.Now;
                sTestResultObj.CreatedBy = sSystemName;
                sTestResultObj.DeviceSerialNo = Worker.MainModel.DeviceSerialNum != null ? Worker.MainModel.DeviceSerialNum : sSerialNo;
                sTestResultObj.PMSFunction = "Visible";

                sResultObj = new tbltestanalyze_results
                {
                    MessageType = sRU_R01.MSH.MessageType.MessageStructure.Value,
                    MessageDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    CreatedBy = sSystemName
                };

                parameters = sTestResultDetails.Select(x => x.TestParameter).ToList();

                clinic = TestResultRepository.GetConfigurationByKey("ClinicID");
                PMS = TestResultRepository.GetConfigurationByKey("InterfaceSettingsPMS");
                if (clinic != null && !string.IsNullOrEmpty(clinic.ConfigurationValue) && PMS != null && PMS.ConfigurationValue != "None")
                {
                    var scheduleString = await VcheckAPI.GetSchedule(clinic.ConfigurationValue, sTestResultObj.PatientID, sResultTestType);
                    sScheduledTestObj = string.IsNullOrEmpty(scheduleString) ? null : JsonConvert.DeserializeObject<ScheduledTestModel>(scheduleString);

                    if (sScheduledTestObj != null)
                    {
                        sTestResultObj.PatientName = sScheduledTestObj.PatientName;
                    }

                    scheduleString = null;
                }

                Boolean bResult = TestResultRepository.insertTestObservationMessage(sResultObj, sMSHObj, sPIDObj, sOBRObj, sOBXObjList, sNTEObj, null, null, null);
                if (bResult)
                {
                    // Insert into Test Result table & create notification 
                    sTestResultDetails = TestResultRepository.createTestResultsMultipleParam(sTestResultObj, sTestResultDetails,out sTestResultObj, sTestResultGraph);

                    if (sScheduledTestObj != null && !string.IsNullOrEmpty(sScheduledTestObj.ScheduleUniqueID))
                    {
                        if (string.IsNullOrEmpty(sResultTestCode) || double.TryParse(sResultTestCode, CultureInfo.InvariantCulture, out _))
                        {
                            var testResponseString = await VcheckAPI.GetTestByNameOrCode(sTestResultObj.TestResultType, null);
                            sResultTestCode = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testid;
                            testResponseString = null;
                        }

                        var success = false;
                        success = await pmsHandler.SendToPMS(sTestResultObj, sTestResultDetails, sScheduledTestObj, sResultTestCode, notes);

                        await VcheckAPI.UpdateScheduleStatus(sScheduledTestObj.LocationID, sScheduledTestObj.PatientID, sScheduledTestObj.ScheduleUniqueID.Split("-")[1], sScheduledTestObj.CreatedBy, success ? 4 : 3, sTestResultObj.TestResultType);

                        if (!success)
                        {
                            NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientName, sSystemName);
                        }
                    }
                    else if (PMS != null && !string.IsNullOrEmpty(PMS.ConfigurationValue) && PMS.ConfigurationValue != "None")
                    {
                        NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientID, sSystemName);
                    }

                    if (PMS != null && PMS.ConfigurationValue != "None") { NotificationRepository.SendNotification(sTestResultObj.PatientID, sSystemName); }
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.Error("ProcessMessage >>> V26 >>> " + ex.ToString());
                isSuccess = false;
            }

            isSuccess = false;
            sTestResultDetails = null;

            sResultRule = null;
            sResultTestType = null;
            sResultTestCode = null;
            sOperatorID = null;
            sPatientID = null;
            sSerialNo = null;
            sUniversalIdentifier = null;
            sTestResultStatus = null;
            strResultObservStatus = null;
            iResultValue = 0;
            dAnalysisDateTime = DateTime.MinValue;
            notes = null;
            sOverallStatus = null;

            sMSHObj = null;
            sPIDObj = null;
            sOBRObj = null;
            sNTEObj = null;
            sOBXObjList = null;
            sTestResultGraph = null;

            sRU_R01 = null;

            StructureList = Array.Empty<string>();
            OBXStructure = Array.Empty<IStructure>();
            OBRStructure = Array.Empty<IStructure>();
            NTEStructure = Array.Empty<IStructure>();

            sTestResultObj = null;
            sResultObj = null;
            parameters = null;
            sScheduledTestObj = null;
            clinic = null;
            PMS = null;

            return isSuccess;
        }

        /// <summary>
        /// Process HL7 Message OUL
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <param name="sSystemName"></param>
        /// <returns></returns>
        public static async Task<bool> ProcessMessageOULAsync(NHapi.Base.Model.IMessage sIMessage, String sSystemName)
        {
            Boolean isSuccess = false;
            var sTestResultDetails = new List<txn_testresults_details>();

            String? sResultRule = "";
            String? sResultTestType = "";
            String? sResultTestCode = "";
            String? sOperatorID = "";
            String? sPatientID = "";
            String? sSerialNo = "";
            String? sUniversalIdentifier = "";
            String? sTestResultStatus = "";
            String? strResultObservStatus = "";
            Decimal iResultValue = 0;
            DateTime dAnalysisDateTime = DateTime.MinValue;
            string notes = "";
            String? sOverallStatus = "Normal";
            List<string> parameters;
            mst_configuration clinic;
            mst_configuration PMS;

            tbltestanalyze_results_messageheader sMSHObj = new tbltestanalyze_results_messageheader();
            List<tbltestanalyze_results_patientidentification> sPIDObj = new List<tbltestanalyze_results_patientidentification>();
            var sOBRObj = new tbltestanalyze_results_observationrequest();
            var sNTEObj = new List<tbltestanalyze_results_notes>();
            var sOBXObjList = new List<tbltestanalyze_results_observationresult>();
            var sTestResultGraph = new List<txn_testresults_graphsExtended>();
            tbltestanalyze_results_patientidentification sPID = new tbltestanalyze_results_patientidentification();
            txn_testresults sTestResultObj = new txn_testresults();
            tbltestanalyze_results sResultObj;
            ScheduledTestModel? sScheduledTestObj = new ScheduledTestModel();

            NHapi.Model.V26.Message.OUL_R22 sRU_R01 = new NHapi.Model.V26.Message.OUL_R22();

            try
            {
                sRU_R01 = (NHapi.Model.V26.Message.OUL_R22)sIMessage;

                if (sRU_R01.MSH.SendingApplication.NamespaceID != null && sRU_R01.MSH.SendingApplication.NamespaceID.Value != null)
                {
                    sSerialNo = sRU_R01.MSH.SendingApplication.NamespaceID.Value.Trim();
                }

                // --------------- Message Header --------------//
                sMSHObj = new tbltestanalyze_results_messageheader
                {
                    FieldSeparator = sRU_R01.MSH.FieldSeparator.Value,
                    EncodingCharacters = sRU_R01.MSH.EncodingCharacters.Value,
                    SendingApplication = ((sRU_R01.MSH.SendingApplication.NamespaceID.Value != null) ? sRU_R01.MSH.SendingApplication.NamespaceID.Value.Trim() : "") + "^" +
                                         ((sRU_R01.MSH.SendingApplication.UniversalID.Value != null) ? sRU_R01.MSH.SendingApplication.UniversalID.Value.Trim() : "") + "^" +
                                         ((sRU_R01.MSH.SendingApplication.UniversalIDType.Value != null) ? sRU_R01.MSH.SendingApplication.UniversalIDType.Value.Trim() : ""),
                    SendingFacility = sRU_R01.MSH.SendingFacility.NamespaceID.Value,
                    ReceivingApplication = sRU_R01.MSH.ReceivingApplication.NamespaceID.Value,
                    ReceivingFacility = sRU_R01.MSH.ReceivingFacility.NamespaceID.Value,
                    DateTimeMessage = sRU_R01.MSH.DateTimeOfMessage.Value,
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



                if (sRU_R01.PATIENT.PID.PatientIdentifierListRepetitionsUsed > 0)
                {
                    sPID.SetID = sRU_R01.PATIENT.PID.SetIDPID.Value;
                    sPID.PatientID = sRU_R01.PATIENT.PID.PatientID.IDNumber.Value;
                    sPID.AlternatePatientID = (sRU_R01.PATIENT.PID.GetAlternatePatientIDPID().Length > 0) ?
                                               sRU_R01.PATIENT.PID.GetAlternatePatientIDPID().FirstOrDefault().IDNumber.ToString() : null;

                    sPID.PatientIdentifierList = (sRU_R01.PATIENT.PID.GetPatientIdentifierList().Length > 0) ?
                                                 sRU_R01.PATIENT.PID.GetPatientIdentifierList().FirstOrDefault().IDNumber.ToString() : null;


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

                var sSPMObj = new tbltestanalyze_results_specimen();
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

                //----------------- Observation Request ----------------------//
                var sTestResultLst = new List<txn_testresults>();
                foreach (var observation in sRU_R01.SPECIMENs.FirstOrDefault().ORDERs)
                {
                    sResultTestCode = observation.OBR.UniversalServiceIdentifier.Identifier.Value;
                    sResultTestType = observation.OBR.UniversalServiceIdentifier.Text.Value;
                    sUniversalIdentifier = observation.OBR.UniversalServiceIdentifier.Text.Value;

                    sOBRObj.SetID = observation.OBR.SetIDOBR.Value;
                    sOBRObj.PlacerOrderNumber = observation.OBR.PlacerOrderNumber.EntityIdentifier.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.NamespaceID.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.UniversalID.Value + "^" +
                                                observation.OBR.PlacerOrderNumber.UniversalIDType.Value;
                    sOBRObj.FillerOrderNumber = observation.OBR.FillerOrderNumber.EntityIdentifier.Value + "^" +
                                                observation.OBR.FillerOrderNumber.NamespaceID.Value + "^" +
                                                observation.OBR.FillerOrderNumber.UniversalID.Value + "^" +
                                                observation.OBR.FillerOrderNumber.UniversalIDType.Value;
                    sOBRObj.UniversalServIdentifier = observation.OBR.UniversalServiceIdentifier.Identifier.Value + "^" +
                                                      observation.OBR.UniversalServiceIdentifier.Text.Value + "^" +
                                                      observation.OBR.UniversalServiceIdentifier.NameOfCodingSystem.Value;
                    sOBRObj.Priority = observation.OBR.Priority.Value;
                    sOBRObj.RequestedDateTime = observation.OBR.RequestedDateTime.Value;
                    sOBRObj.ObservationDateTime = observation.OBR.ObservationDateTime.Value.Trim();
                    sOBRObj.ObservationEndDateTime = observation.OBR.ObservationEndDateTime.Value.Trim();
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
                            notes = sTestResultStatus;
                        }
                    }

                    // --------------- Observation Results ----------------//
                    foreach (var observationDetail in observation.RESULTs)
                    {
                        String sInterpretation = "";
                        String? sObservValue = "";

                        if (observationDetail.OBX.GetObservationValue().Count() > 0)
                        {
                            if (observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.NA))
                            {
                                var sNAObject = observationDetail.OBX.GetObservationValue().FirstOrDefault().Data;

                                int iTotalComponent = sNAObject.ExtraComponents.NumComponents();
                                List<String> sVal = new List<String>();

                                // --- Get from Component ------ //
                                PropertyInfo[] props = sNAObject.GetType().GetProperties();
                                foreach (PropertyInfo p in props)
                                {
                                    if (p.PropertyType == typeof(NHapi.Model.V26.Datatype.NM))
                                    {
                                        sVal.Add(p.GetValue(sNAObject, null).ToString());
                                    }
                                    ;
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
                                if (observationDetail.OBX.GetObservationValue().FirstOrDefault().Data.GetType() == typeof(NHapi.Model.V26.Datatype.CWE))
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

                        if (!String.IsNullOrEmpty(observationDetail.OBX.DateTimeOfTheObservation.Value))
                        {
                            if (observationDetail.OBX.DateTimeOfTheObservation.Value.Length == 14)
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observationDetail.OBX.DateTimeOfTheObservation.Value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else if (observationDetail.OBX.DateTimeOfTheObservation.Value.Length == 22)
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observationDetail.OBX.DateTimeOfTheObservation.Value, "yyyyMMdd HH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                dAnalysisDateTime = DateTime.ParseExact(observationDetail.OBX.DateTimeOfTheObservation.Value, "yyyyMMddHHmmss-ffff", System.Globalization.CultureInfo.InvariantCulture);
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
                            ObservationDateTime = observationDetail.OBX.DateTimeOfTheObservation.Value,
                            ProducerID = observationDetail.OBX.ProducerSID.Text.Value,
                            ResponsibleObserver = sOperatorID,
                            ObservationMethod = (observationDetail.OBX.GetObservationMethod().Length > 0) ?
                                                 observationDetail.OBX.GetObservationMethod().FirstOrDefault().Text.ToString() : null,
                            EquipmentInstanceIdentifier = (observationDetail.OBX.GetEquipmentInstanceIdentifier().Length > 0) ?
                                                           observationDetail.OBX.GetEquipmentInstanceIdentifier().FirstOrDefault().EntityIdentifier.Value : null,
                            AnalysisDateTime = observationDetail.OBX.DateTimeOfTheAnalysis.Value

                        });

                        // ------------ Notes -------------------//
                        if (observationDetail.NTEs.Count() > 0)
                        {
                            String sComment = GenerateNTEComments(observationDetail.NTEs.ToList());

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
                                if (sResultTestType.ToLower() == "cav ab")
                                {
                                    var sCWEOBXValue = observationDetail.OBX.GetObservationValue();
                                    if (sCWEOBXValue.Length > 0)
                                    {
                                        NHapi.Model.V26.Datatype.CWE sVNData = (NHapi.Model.V26.Datatype.CWE)sCWEOBXValue[0].Data;
                                        if (sVNData != null)
                                        {
                                            String sCWEValue = sVNData.NameOfCodingSystem.Value;
                                            if (sCWEValue.ToLower() == "invalid")
                                            {
                                                sTestResultStatus = sCWEValue;

                                                String sValue = "";
                                                String[] strArryValue = sComment.Split(",");
                                                if (strArryValue.Length > 0)
                                                {
                                                    sValue = strArryValue[1].Replace("Value=", "").Trim();
                                                }

                                                Decimal.TryParse(sValue, CultureInfo.InvariantCulture, out iResultValue);
                                                sObservValue = sValue;
                                            }
                                            else
                                            {
                                                String[] arrayCWE = sCWEValue.Split(",");
                                                if (arrayCWE.Length > 0)
                                                {
                                                    sTestResultStatus = arrayCWE[0].Trim();

                                                    String[] arrayValue = arrayCWE[1].Split("VN");
                                                    if (arrayValue.Length > 0)
                                                    {
                                                        sResultRule = "VN";
                                                        sObservValue = arrayValue[1].Trim();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    sResultRule = "COI";

                                    String sValue = "";
                                    String[] strArryValue = sComment.Split(",");
                                    if (strArryValue.Length > 0)
                                    {
                                        sValue = strArryValue[1].Replace("Value=", "").Trim();
                                    }

                                    Decimal.TryParse(sValue, CultureInfo.InvariantCulture, out iResultValue);
                                    sObservValue = sValue;
                                }
                            }

                            if (sComment.ToLower().Contains("interpretation"))
                            {
                                String[] strArryValue = sComment.Split("=");
                                if (strArryValue.Length > 0 && strArryValue[1].Trim() != "Not Used")
                                {
                                    sInterpretation = strArryValue[1].Trim();
                                    sTestResultStatus = strArryValue[1].Trim();
                                }
                                else
                                {
                                    sTestResultStatus = "Normal";
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


                        if (sResultTestType.ToLower() != "babesia gibsoni/canis" && sResultTestType.ToLower() != "canine diarrhea 8 panel")
                        {
                            string referenceRange = observationDetail.OBX.ReferencesRange.Value;
                            string measuringRange = "";
                            if (sResultTestType.ToLower() != "cav ab")
                            {
                                if (sResultRule == "COI")
                                {
                                    sTestResultStatus = General.ProcessObservationResultStatusValue(false, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);
                                }
                                else
                                {
                                    sTestResultStatus = General.ProcessObservationResultStatusValueReferenceRange("V200", sObservValue, sResultTestType, "General", "General", out referenceRange, out measuringRange);

                                    if (string.IsNullOrEmpty(sTestResultStatus))
                                    {
                                        sTestResultStatus = General.ProcessObservationResultStatusValue(true, sObservValue, observationDetail.OBX.ReferencesRange.Value, iResultValue);

                                        if (!string.IsNullOrEmpty(sTestResultStatus))
                                        {
                                            referenceRange = observationDetail.OBX.ReferencesRange.Value;
                                        }
                                    }

                                }
                            }

                            sTestResultDetails.Add(new txn_testresults_details
                            {
                                TestParameter = observationDetail.OBX.ObservationIdentifier.Text.Value.Replace("?", ""),
                                SubID = observationDetail.OBX.ObservationSubID.Value,
                                ProceduralControl = strResultObservStatus,
                                TestResultStatus = sTestResultStatus,
                                TestResultValue = sObservValue,
                                TestResultUnit = (!String.IsNullOrEmpty(observationDetail.OBX.Units.Identifier.Value)) ? observationDetail.OBX.Units.Identifier.Value : sResultRule,
                                ReferenceRange = referenceRange,
                                MeasuringRange = measuringRange,
                                Interpretation = sInterpretation
                            });
                        }
                    }
                }

                sOverallStatus = "Normal";
                if (sTestResultDetails != null)
                {
                    if (sTestResultDetails.Where(x => x.TestParameter.ToLower() == "cav ab").Count() > 0)
                    {
                        sOverallStatus = sTestResultDetails[0].TestResultStatus;
                    }
                    else
                    {
                        if (sTestResultDetails.Where(x => x.TestResultStatus == "Positive").Count() > 0 && sTestResultDetails.Where(x => x.TestParameter.Contains(" IC")).Count() == 0)
                        {
                            sOverallStatus = "Abnormal";
                        }
                        else if (sTestResultDetails.Where(x => x.TestResultStatus == "Invalid").Count() > 0)
                        {
                            sOverallStatus = "Invalid";
                        }
                        else if (sTestResultDetails.Where(x => x.TestResultStatus == "Abnormal").Count() > 0)
                        {
                            sOverallStatus = "Abnormal";
                        }
                        else if (sTestResultDetails.Where(x => x.TestResultStatus == "Normal").Count() == sTestResultDetails.Count())
                        {
                            sOverallStatus = "Normal";
                        }
                        else
                        {
                            var sStatus = sTestResultDetails.FirstOrDefault(x => !string.IsNullOrEmpty(x.TestResultStatus));
                            sOverallStatus = sStatus != null ? sStatus.TestResultStatus : "Error";
                        }
                    }

                }

                sTestResultObj.TestResultDateTime = dAnalysisDateTime;
                sTestResultObj.TestResultType = sResultTestType;
                sTestResultObj.OperatorID = sOperatorID;
                sTestResultObj.PatientID = sPatientID;
                sTestResultObj.InchargePerson = "";
                sTestResultObj.OverallStatus = sOverallStatus;
                sTestResultObj.CreatedDate = DateTime.Now;
                sTestResultObj.CreatedBy = sSystemName;
                sTestResultObj.DeviceSerialNo = Worker.MainModel.DeviceSerialNum != null ? Worker.MainModel.DeviceSerialNum : sSerialNo;
                sTestResultObj.PMSFunction = "Visible";

                sResultObj = new tbltestanalyze_results
                {
                    MessageType = sRU_R01.MSH.MessageType.MessageStructure.Value,
                    MessageDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    CreatedBy = sSystemName
                };

                parameters = sTestResultDetails.Select(x => x.TestParameter).ToList();

                sScheduledTestObj = new ScheduledTestModel();
                clinic = TestResultRepository.GetConfigurationByKey("ClinicID");
                PMS = TestResultRepository.GetConfigurationByKey("InterfaceSettingsPMS");
                if (clinic != null && !string.IsNullOrEmpty(clinic.ConfigurationValue) && PMS != null && PMS.ConfigurationValue != "None")
                {
                    var scheduleString = await VcheckAPI.GetSchedule(clinic.ConfigurationValue, sTestResultObj.PatientID, sResultTestType);
                    sScheduledTestObj = string.IsNullOrEmpty(scheduleString) ? null : JsonConvert.DeserializeObject<ScheduledTestModel>(scheduleString);

                    if (sScheduledTestObj != null)
                    {
                        sTestResultObj.PatientName = sScheduledTestObj.PatientName;
                    }

                    scheduleString = null;
                }

                Boolean bResult = TestResultRepository.insertTestObservationMessage(sResultObj, sMSHObj, sPIDObj, sOBRObj, sOBXObjList, sNTEObj, null, null, null);
                if (bResult)
                {
                    // Insert into Test Result table & create notification 
                    sTestResultDetails = TestResultRepository.createTestResultsMultipleParam(sTestResultObj, sTestResultDetails, out sTestResultObj, sTestResultGraph);

                    if (sScheduledTestObj != null && !string.IsNullOrEmpty(sScheduledTestObj.ScheduleUniqueID))
                    {
                        if (string.IsNullOrEmpty(sResultTestCode) || double.TryParse(sResultTestCode, CultureInfo.InvariantCulture, out _))
                        {
                            var testResponseString = await VcheckAPI.GetTestByNameOrCode(sTestResultObj.TestResultType, null);
                            sResultTestCode = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testid;
                            testResponseString = null;
                        }

                        var success = false;
                        success = await pmsHandler.SendToPMS(sTestResultObj, sTestResultDetails, sScheduledTestObj, sResultTestCode, notes);

                        await VcheckAPI.UpdateScheduleStatus(sScheduledTestObj.LocationID, sScheduledTestObj.PatientID, sScheduledTestObj.ScheduleUniqueID.Split("-")[1], sScheduledTestObj.CreatedBy, success ? 4 : 3, sTestResultObj.TestResultType);

                        if (!success)
                        {
                            NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientName, sSystemName);
                        }
                    }
                    else if (PMS != null && !string.IsNullOrEmpty(PMS.ConfigurationValue) && PMS.ConfigurationValue != "None")
                    {
                        NotificationRepository.SendPMSErrorNotification(sResultTestType, sTestResultObj.PatientID, sSystemName);
                    }

                    if (PMS != null && PMS.ConfigurationValue != "None") { NotificationRepository.SendNotification(sTestResultObj.PatientID, sSystemName); }
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.Error("ProcessMessage >>> V26 >>> " + ex.ToString());
                isSuccess = false;
            }

            isSuccess = false;
            sTestResultDetails = null;

            sResultRule = null;
            sResultTestType = null;
            sResultTestCode = null;
            sOperatorID = null;
            sPatientID = null;
            sSerialNo = null;
            sUniversalIdentifier = null;
            sTestResultStatus = null;
            strResultObservStatus = null;
            iResultValue = 0;
            dAnalysisDateTime = DateTime.MinValue;
            notes = null;
            sOverallStatus = null;

            sMSHObj = null;
            sPIDObj = null;
            sOBRObj = null;
            sNTEObj = null;
            sOBXObjList = null;
            sTestResultGraph = null;

            sRU_R01 = null;

            sTestResultObj = null;
            sResultObj = null;
            parameters = null;
            sScheduledTestObj = null;
            clinic = null;
            PMS = null;

            return isSuccess;
        }

        /// <summary>
        /// Get NTE Comment data 
        /// </summary>
        /// <param name="nte"></param>
        /// <returns></returns>
        public static String GenerateNTEComments(List<NTE> nte)
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
                _logger.Error("GenerateNTEComments >>> V26 >>> " + ex.ToString());
            }

            return nteComment;
        }

        /// <summary>
        /// Process Babesia type observation test result 
        /// </summary>
        /// <param name="sObservationResult"></param>
        /// <returns></returns>
        public static List<txn_testresults_details> ProcessBabesiaGibsoniTestResult(List<tbltestanalyze_results_observationresult> sObservationResult)
        {
            List<txn_testresults_details> sResultDetails = new List<txn_testresults_details>();

            var sObservationGroup = sObservationResult.GroupBy(x => x.ObservationIdentifier).ToList();
            if (sObservationGroup.Count() > 0)
            {
                foreach (var g in sObservationGroup)
                {
                    var sObserv = sObservationResult.Where(x => x.ObservationIdentifier == g.Key).ToList();
                    if (sObserv != null)
                    {
                        String sIdentifier = "";
                        String[] arrayIdentifier = g.Key.Split("^");

                        if (arrayIdentifier.Length > 0)
                        {
                            sIdentifier = arrayIdentifier[1];
                        }

                        String? sSetID = sObserv.OrderBy(x => Convert.ToInt32(x.SetID)).FirstOrDefault().SetID;
                        int iNextID = Convert.ToInt32(sSetID) + 2;

                        String sProcControl = "";
                        var s = sObserv.Where(x => x.SetID == sSetID).FirstOrDefault().ObservationResultStatus;
                        if (s == "F")
                        {
                            sProcControl = "Valid";
                        }
                        else
                        {
                            sProcControl = "Invalid";
                        }

                        String sUnitString = "";
                        var unitObj = sObserv.Where(x => x.SetID == sSetID).FirstOrDefault().Units;
                        if (unitObj != null)
                        {
                            String[] arrayUnit = unitObj.Split("^");
                            if (arrayUnit.Length > 0)
                            {
                                sUnitString = arrayUnit[0];
                            }
                        }

                        sResultDetails.Add(new txn_testresults_details
                        {
                            TestParameter = sIdentifier.Replace("Babesia gibsoni/canis ", ""),
                            SubID = sObserv.Where(x => x.SetID == sSetID).FirstOrDefault().ObservationSubID,
                            ProceduralControl = sProcControl,
                            TestResultStatus = sObserv.Where(x => x.SetID == sSetID).FirstOrDefault().ObservationValue,
                            TestResultValue = sObserv.Where(x => x.SetID == iNextID.ToString()).FirstOrDefault().ObservationValue.ToString(),
                            TestResultUnit = sUnitString,
                            ReferenceRange = sObserv.Where(x => x.SetID == sSetID).FirstOrDefault().ReferencesRange,
                            Interpretation = ""
                        });
                    }
                }
            }

            return sResultDetails;
        }



        static void DrawTable(Graphics g, string TestName, List<(string, string, double[])> Datas, int width, int height)
        {
            Pen border = new Pen(System.Drawing.Color.DodgerBlue, 2);
            Font header = new Font("Arial", 16, System.Drawing.FontStyle.Bold);
            Font text = new Font("Arial", 14);
            int y = 100;

            g.DrawRectangle(border, 0, 0, width, height);

            g.DrawString("Result", header, Brushes.Black, 20, 20);
            //g.DrawString("Pathogen", header, Brushes.Black, 250, 20);
            //g.DrawString("", header, Brushes.Black, 450, 20);

            g.DrawLine(border, 0, 70, width, 70);

            foreach (var Data in Datas)
            {
                g.DrawString(Data.Item1, text, Brushes.Black, 20, y);
                //g.DrawString(Data.Item1, text, Brushes.Black, 250, y);
                g.DrawString(Data.Item2, text, Data.Item2.ToString().Contains("+") ? Brushes.Red : Brushes.Black, 450, y);

                y = y + 60;
            }
        }
    }
}
