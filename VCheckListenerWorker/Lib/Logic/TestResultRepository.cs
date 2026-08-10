using log4net;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using VCheckListenerWorker.Lib.DBContext;
using VCheckListenerWorker.Lib.Models;

namespace VCheckListenerWorker.Lib.Logic
{
    public class TestResultRepository
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        public static string graphFolder = Host.CreateApplicationBuilder().Configuration.GetSection("FileOutput:Graph").Value;

        /// <summary>
        /// Insert Test Result Raw Data
        /// </summary>
        /// <param name="sResult"></param>
        /// <param name="sResultMSH"></param>
        /// <param name="sResultPID"></param>
        /// <param name="sResultOBR"></param>
        /// <param name="sResultOBXs"></param>
        /// <param name="sResultNTEs"></param>
        /// <returns></returns>
        public static Boolean insertTestObservationMessage(tbltestanalyze_results sResult,
                                                           tbltestanalyze_results_messageheader sResultMSH,
                                                           List<tbltestanalyze_results_patientidentification> sResultPID,
                                                           tbltestanalyze_results_observationrequest sResultOBR,
                                                           List<tbltestanalyze_results_observationresult> sResultOBXs,
                                                           List<tbltestanalyze_results_notes> sResultNTEs,
                                                           tbltestanalyze_results_patientvisit sResultPV,
                                                           tbltestanalyze_results_specimen sResultSpecimen = null, 
                                                           tbltestanalyze_results_specimencontainer sResultContainer = null)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    ctx.tbltestanalyze_results.Add(sResult);
                    ctx.SaveChanges();

                    sResultMSH.ResultRowID = sResult.ResultRowID;
                    ctx.tbltestanalyze_results_messageheader.Add(sResultMSH);
                    ctx.SaveChanges();

                    if (sResultPID.Count > 0)
                    {
                        foreach (var PID in sResultPID)
                        {
                            PID.ResultRowID = sResult.ResultRowID;
                            ctx.tbltestanalyze_results_patientidentification.Add(PID);
                            ctx.SaveChanges();
                        }
                    }


                    sResultOBR.ResultRowID = sResult.ResultRowID;
                    ctx.tbltestanalyze_results_observationrequest.Add(sResultOBR);
                    ctx.SaveChanges();

                    if (sResultOBXs.Count > 0)
                    {
                        foreach(var OBX in sResultOBXs)
                        {
                            OBX.ResultRowID = sResult.ResultRowID;

                            ctx.tbltestanalyze_results_observationresult.Add(OBX);
                            ctx.SaveChanges();
                        }

                    }

                    if (sResultNTEs.Count > 0)
                    {
                        foreach(var NTE in sResultNTEs)
                        {
                            NTE.ResultRowID = sResult.ResultRowID;

                            ctx.tbltestanalyze_results_notes.Add(NTE);
                            ctx.SaveChanges();
                        }
                    }

                    if (sResultPV != null)
                    {
                        sResultPV.ResultRowID = sResult.ResultRowID;

                        ctx.tbltestanalyze_results_patientvisit.Add(sResultPV);
                        ctx.SaveChanges();
                    }

                    if (sResultSpecimen != null)
                    {
                        sResultSpecimen.ResultRowID = sResult.ResultRowID;

                        ctx.tbltestanalyze_results_specimen.Add(sResultSpecimen);
                        ctx.SaveChanges();
                    }

                    if (sResultContainer != null)
                    {
                        sResultContainer.ResultRowID = sResult.ResultRowID;

                        ctx.tbltestanalyze_results_specimencontainer.Add(sResultContainer);
                        ctx.SaveChanges();
                    }

                    isSuccess = true;
                }
            }
            catch(Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> InsertTestObservationMessag >>> " +ex.ToString());
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Insert into Test Result Table
        /// </summary>
        /// <param name="sTestResult"></param>
        /// <returns></returns>
        public static Boolean createTestResult(txn_testresults sTestResult)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    ctx.txn_Testresults.Update(sTestResult);
                    ctx.SaveChanges();

                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> createTestResult >>> " + ex.ToString());
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Insert into Test Result table with multiple breakdown details
        /// </summary>
        /// <param name="sTestResult"></param>
        /// <param name="sTestResultDetail"></param>
        /// <returns></returns>
        public static List<txn_testresults_details> createTestResultsMultipleParam(txn_testresults sTestResult, List<txn_testresults_details> sTestResultDetail, out txn_testresults sCurrentTestResult, List<txn_testresults_graphsExtended> sTestResultGraph = null)
        {
            string[] cCortisolParameter = { "Pre-ACTH", "Post-ACTH", "Pre-dose(L)", "Post-4Hours(L)", "Post-8Hours(L)", "Pre-dose(H)", "Post-4Hours(H)", "Post-8Hours(H)" };
            string[] ignoreParameter = { "cCortisol", "Pre-ACTH", "Pre-dose(L)", "Pre-dose(H)" };

            try
            {
                if (sTestResult.TestResultType == "cCortisol")
                {
                    var sCurrentParameter = sTestResultDetail.Select(x => x.TestParameter).FirstOrDefault();

                    if (!ignoreParameter.Contains(sCurrentParameter))
                    {
                        var currentParameterIndex = Array.IndexOf(cCortisolParameter, sCurrentParameter);
                        var sPreviousParameter = cCortisolParameter[currentParameterIndex - 1];

                        var sTestResultDetailTemp = UpdateCortisolResult(sPreviousParameter, sCurrentParameter, sTestResult, sTestResultDetail, out sCurrentTestResult);

                        if (sTestResultDetailTemp != null && sTestResultDetailTemp.Count() > 0)
                        {
                            return sTestResultDetailTemp;
                        }
                    }
                }

                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    sCurrentTestResult = sTestResult;
                    ctx.txn_Testresults.Add(sTestResult);
                    ctx.SaveChanges();

                    foreach (var d in sTestResultDetail)
                    {
                        d.TestResultRowID = sTestResult.ID;

                        ctx.txn_testresults_details.Add(d);
                        ctx.SaveChanges();
                    }

                    if (sTestResultGraph != null && sTestResultGraph.Count() > 0)
                    {
                        if (!Directory.Exists(graphFolder + sTestResult.ID))
                        {
                            Directory.CreateDirectory(graphFolder + sTestResult.ID);
                        }

                        foreach (var d in sTestResultGraph)
                        {
                            var graph = new txn_testresults_graphs() { TestResultRowID = sTestResult.ID, FileName = d.FileName, CreatedDate = DateTime.Now, CreatedBy = "VCheckViewer Listener" };
                            ctx.txn_testresults_graphs.Add(graph);

                            byte[] imageBytes = Convert.FromBase64String(d.Base64String);

                            File.WriteAllBytes(graphFolder + sTestResult.ID + "\\" + d.FileName, imageBytes);
                        }
                        ctx.SaveChanges();
                    }

                    return sTestResultDetail;
                }

                //using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                //{
                //    var existingTest = ctx.txn_Testresults.FirstOrDefault(x => x.CreatedDate > MinimumDatetime && x.TestResultType == sTestResult.TestResultType && x.PatientID == sTestResult.PatientID && x.IsDeleted == 0);

                //    if(existingTest != null && existingTest.ID != 0)
                //    {
                //        existingTest.TestResultDateTime = sTestResult.TestResultDateTime;
                //        existingTest.OverallStatus = sTestResult.OverallStatus;
                //        existingTest.UpdatedDate = DateTime.Now;
                //        existingTest.UpdatedBy = "VCheckViewer Listener";

                //        ctx.txn_Testresults.Update(existingTest);
                //        ctx.SaveChanges();

                //        var existingTestDetails = ctx.txn_testresults_details.Where(x => x.TestResultRowID == existingTest.ID).ToList();
                //        var existingTestDetails_namelist = existingTestDetails.Select(x => x.TestParameter);
                //        var sTestResultDetail_NotExistNamelist = sTestResultDetail.Where(x => !existingTestDetails_namelist.Contains(x.TestParameter));
                //        var sTestResultDetail_ExistNamelist = sTestResultDetail.Where(x => existingTestDetails_namelist.Contains(x.TestParameter));

                //        var existingTestDetailsGraph = ctx.txn_testresults_graphs.Where(x => x.TestResultRowID == existingTest.ID).ToList();
                //        var existingTestDetailsGraph_nameList = existingTestDetailsGraph.Select(x => x.FileName);
                //        var existingTestDetailsGraph_NotExistNameList = existingTestDetailsGraph_nameList != null ? sTestResultGraph.Where(x => !existingTestDetailsGraph_nameList.Contains(x.FileName)) : new List<txn_testresults_graphsExtended>();
                //        var existingTestDetailsGraph_ExistNameList = existingTestDetailsGraph_nameList != null ? sTestResultGraph.Where(x => existingTestDetailsGraph_nameList.Contains(x.FileName)) : new List<txn_testresults_graphsExtended>();

                //        foreach (var testDetail_NotExist in sTestResultDetail_NotExistNamelist)
                //        {
                //            testDetail_NotExist.TestResultRowID = existingTest.ID;

                //            ctx.txn_testresults_details.Add(testDetail_NotExist);
                //            ctx.SaveChanges();
                //            existingTestDetails.Add(testDetail_NotExist);
                //        }

                //        foreach (var testDetail_Exist in sTestResultDetail_ExistNamelist)
                //        {
                //            var existingDetail = existingTestDetails.FirstOrDefault(x => x.TestParameter == testDetail_Exist.TestParameter);
                //            existingDetail.TestResultStatus = testDetail_Exist.TestResultStatus;
                //            existingDetail.TestResultValue = testDetail_Exist.TestResultValue;
                //            existingDetail.TestResultUnit = testDetail_Exist.TestResultUnit;
                //            existingDetail.ReferenceRange = testDetail_Exist.ReferenceRange;
                //            existingDetail.Interpretation = testDetail_Exist.Interpretation;
                //            existingDetail.MeasuringRange = testDetail_Exist.MeasuringRange;

                //            ctx.txn_testresults_details.Update(existingDetail);
                //            ctx.SaveChanges();
                //        }

                //        if (sTestResultGraph != null && sTestResultGraph.Count() > 0)
                //        {
                //            if (!Directory.Exists(graphFolder + existingTest.ID))
                //            {
                //                Directory.CreateDirectory(graphFolder + existingTest.ID);
                //            }

                //            foreach (var d in existingTestDetailsGraph_NotExistNameList)
                //            {
                //                var graph = new txn_testresults_graphs() { TestResultRowID = existingTest.ID, FileName = d.FileName, CreatedDate = DateTime.Now, CreatedBy = "VCheckViewer Listener" };
                //                ctx.txn_testresults_graphs.Add(graph);
                //                ctx.SaveChanges();

                //                byte[] imageBytes = Convert.FromBase64String(d.Base64String);

                //                File.WriteAllBytes(graphFolder + existingTest.ID + "\\" + d.FileName, imageBytes);
                //            }

                //            foreach (var d in existingTestDetailsGraph_ExistNameList)
                //            {
                //                byte[] imageBytes = Convert.FromBase64String(d.Base64String);

                //                File.WriteAllBytes(graphFolder + existingTest.ID + "\\" + d.FileName, imageBytes);
                //            }
                //        }

                //        sCurrentTestResult = existingTest;
                //        sTestResultDetail = existingTestDetails;
                //    }
                //    else
                //    {
                //        sCurrentTestResult = sTestResult;
                //        ctx.txn_Testresults.Add(sTestResult);
                //        ctx.SaveChanges();

                //        foreach (var d in sTestResultDetail)
                //        {
                //            d.TestResultRowID = sTestResult.ID;

                //            ctx.txn_testresults_details.Add(d);
                //            ctx.SaveChanges();
                //        }

                //        if (sTestResultGraph != null && sTestResultGraph.Count() > 0)
                //        {
                //            if (!Directory.Exists(graphFolder + sTestResult.ID))
                //            {
                //                Directory.CreateDirectory(graphFolder + sTestResult.ID);
                //            }

                //            foreach (var d in sTestResultGraph)
                //            {
                //                var graph = new txn_testresults_graphs() { TestResultRowID = sTestResult.ID, FileName = d.FileName, CreatedDate = DateTime.Now, CreatedBy = "VCheckViewer Listener" };
                //                ctx.txn_testresults_graphs.Add(graph);

                //                byte[] imageBytes = Convert.FromBase64String(d.Base64String);

                //                File.WriteAllBytes(graphFolder + sTestResult.ID + "\\" + d.FileName, imageBytes);
                //            }
                //            ctx.SaveChanges();
                //        }
                //    }

                //    return sTestResultDetail;
                //}
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> createTestResultsMultipleParam >>> " + ex.ToString());
                sCurrentTestResult = sTestResult;
                return null;
            }
        }

        //public static Boolean createBulkTestResult(List<txn_testresults> sTestResult)
        //{
        //    Boolean isSuccess = false;

        //    try
        //    {
        //        using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
        //        {
        //            ctx.txn_Testresults.AddRange(sTestResult);
        //            ctx.SaveChanges();

        //            isSuccess = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error("VCheckListenerWorker >>> TestResultRepository >>> createBulkTestResult >>> " + ex.ToString());
        //        isSuccess = false;
        //    }

        //    return isSuccess;
        //}

        /// <summary>
        /// Get Notification Template By Code
        /// </summary>
        /// <param name="sTemplateCode"></param>
        /// <returns></returns>
        public static mst_template GetNotificationTemplate(String sTemplateCode)
        {
            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    return ctx.mst_template.Where(x => x.TemplateCode == sTemplateCode).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> GetNorificationTemplate >>> " + ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Get Notification Template By Code & Locale
        /// </summary>
        /// <param name="sTemplateCode"></param>
        /// <param name="sLangCode"></param>
        /// <returns></returns>
        public static NotificationTemplateLang GetNotificationTemplateByLanguage(String sTemplateCode, String sLangCode)
        {
            var sTemplateObj = new NotificationTemplateLang();

            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    var sResult = ctx.mst_template.Where(x => x.TemplateCode == sTemplateCode).FirstOrDefault();
                    if (sResult != null)
                    {
                        var sTemplateContentObj = ctx.mst_template_details.Where(x => x.TemplateID == sResult.TemplateID && x.LangCode == sLangCode).FirstOrDefault();
                        if (sTemplateContentObj != null)
                        {
                            sTemplateObj.TemplateID = sTemplateContentObj.TemplateID;
                            sTemplateObj.TemplateType = sResult.TemplateType;
                            sTemplateObj.TemplateCode = sResult.TemplateCode;
                            sTemplateObj.TemplateTitle = sTemplateContentObj.TemplateTitle;
                            sTemplateObj.TemplateContent = sTemplateContentObj.TemplateContent;
                            sTemplateObj.TemplateLangCode = sTemplateContentObj.LangCode;
                        }
                    }

                    return sTemplateObj;
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> GetNotificationTemplateByLanguage >>> " + ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Insert Notification 
        /// </summary>
        /// <param name="sNotification"></param>
        /// <returns></returns>
        public static Boolean insertNotification(txn_notification sNotification)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    ctx.txn_notification.Add(sNotification);
                    ctx.SaveChanges();

                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> insertNotification >>> " + ex.ToString());
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Get Configuration Settings By Key
        /// </summary>
        /// <param name="sConfigurationkey"></param>
        /// <returns></returns>
        public static mst_configuration GetConfigurationByKey(String sConfigurationkey)
        {
            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    return ctx.mst_configuration.Where(x => x.ConfigurationKey == sConfigurationkey).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> GetConfigurationByKey >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Configuration Settings By Key
        /// </summary>
        /// <param name="sConfigurationkey"></param>
        /// <returns></returns>
        public static List<mst_configuration> GetAllConfiguration()
        {
            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    return ctx.mst_configuration.ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> GetConfigurationByKey >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Configuration Settings By Key
        /// </summary>
        /// <param name="sConfigurationkey"></param>
        /// <returns></returns>
        public static ScheduledTestModel GetScheduleByPatientID(String sPatientID)
        {
            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    return ctx.Txn_ScheduledTests.FirstOrDefault(x => x.PatientID == sPatientID && x.ScheduleTestStatus == 0);
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> GetScheduleByPatientID >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Update Scheduled Test Information by Order ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sScheduledTestObj"></param>
        /// <returns></returns>
        public static Boolean UpdateScheduledTestStatusByOrderID(int sStatus, string sOrderID, string sClientName, string sUpdatedBy)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    var sData = ctx.Txn_ScheduledTests.Where(x => x.CreatedBy == sClientName).AsEnumerable().Where(y => y.ScheduleUniqueID.Split("-")[1] == sOrderID);
                    if (sData != null)
                    {
                        foreach (var sSchedule in sData)
                        {
                            if (sSchedule.ScheduleTestStatus != sStatus)
                            {
                                sSchedule.ScheduleTestStatus = sStatus;
                                sSchedule.UpdatedDate = DateTime.Now;
                                sSchedule.UpdatedBy = sUpdatedBy;
                            }
                        }

                        ctx.UpdateRange(sData);

                        ctx.SaveChanges();

                        isSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                isSuccess = false;
            }

            return isSuccess;
        }

        /// <summary>
        /// Get Device Settings By IP
        /// </summary>
        /// <param name="sConfigurationkey"></param>
        /// <returns></returns>
        public static DeviceModel GetDeviceByIPSerialNo(String? sIP, String? sSerialNo, out string sDeviceType)
        {
            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    DeviceModel? device = new DeviceModel();

                    if (string.IsNullOrEmpty(sIP))
                    {
                        device = ctx.mst_deviceslist.FirstOrDefault(x => x.DeviceSerialNo == sSerialNo && x.status == 1);
                    }
                    else if (string.IsNullOrEmpty(sSerialNo))
                    {
                        device = ctx.mst_deviceslist.FirstOrDefault(x => x.DeviceIPAddress == sIP && x.status == 1);
                    }
                    else
                    {
                        device = ctx.mst_deviceslist.FirstOrDefault(x => x.DeviceSerialNo == sSerialNo && x.status == 1);

                        if(device == null)
                        {
                            device = ctx.mst_deviceslist.FirstOrDefault(x => x.DeviceIPAddress == sIP && x.status == 1);
                        }
                    }

                    if (device != null && device.id != 0)
                    {
                        sDeviceType = ctx.mst_devicetype.FirstOrDefault(x => x.id == device.DeviceTypeID).TypeName;
                    }
                    else
                    {
                        sDeviceType = "";
                    }

                    return device;
                }
            }
            catch (Exception ex)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> GetScheduleByPatientID >>> " + ex.ToString());
                sDeviceType = "";
                return null;
            }
        }

        /// <summary>
        /// Get parameter reference range
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static TestResultReferenceRangeModel GetReferenceRangeByParameterAnalyzerSpecies(string parameter, string analyzer, string species, string ageGroup)
        {
            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    IEnumerable<TestResultReferenceRangeModel> referenceRangeInfos = new List<TestResultReferenceRangeModel>();

                    if (string.IsNullOrEmpty(analyzer))
                    {
                        referenceRangeInfos = ctx.mst_testresults_referencerange.AsEnumerable().Where(x => x.Parameter == parameter);
                    }
                    else
                    {
                        referenceRangeInfos = ctx.mst_testresults_referencerange.AsEnumerable().Where(x => x.Parameter == parameter && x.Analyzer.Split(", ").Contains(analyzer));
                    }
                    
                    TestResultReferenceRangeModel referenceRangeInfo = new TestResultReferenceRangeModel();

                    if (referenceRangeInfos != null && referenceRangeInfos.Count() > 0)
                    {
                        referenceRangeInfo = referenceRangeInfos.FirstOrDefault(x => x.Species == species && x.AgeGroup == ageGroup);
                    }
                    else
                    {
                        return referenceRangeInfo;
                    }


                    if (referenceRangeInfo != null)
                    {
                        return referenceRangeInfo;
                    }
                    else
                    {
                        referenceRangeInfo = referenceRangeInfos.FirstOrDefault(x => x.Species == species);

                        if(referenceRangeInfo != null)
                        {
                            return referenceRangeInfo;
                        }
                        else
                        {
                            referenceRangeInfo = referenceRangeInfos.FirstOrDefault(x => x.AgeGroup == ageGroup);

                            if(referenceRangeInfo != null)
                            {
                                return referenceRangeInfo;
                            }
                            else
                            {
                                return referenceRangeInfos.FirstOrDefault();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());

                return null;
            }
        }

        /// <summary>
        /// Get Appsetting configuration
        /// </summary>
        /// <returns></returns>
        public static IConfiguration GetConfigurationSettings()
        {
            var iHost = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

            return iHost.Configuration;
        }

        /// <summary>
        /// Get result cCortisol
        /// </summary>
        /// <returns></returns>
        public static List<txn_testresults_details> UpdateCortisolResult(string sPreviousParameter, string sCurrentParameter, txn_testresults sTestResult, List<txn_testresults_details> sTestResultDetail, out txn_testresults sCurrentTestResult)
        {
            sCurrentTestResult = new txn_testresults();
            DateTime MinimumDatetime = DateTime.Now.AddHours(-48);

            try
            {
                using (var ctx = new TestResultDBContext(GetConfigurationSettings()))
                {
                    var existingTestList = ctx.txn_Testresults.AsNoTracking().Where(x => x.CreatedDate > MinimumDatetime && x.TestResultType == "cCortisol" && x.PatientID == sTestResult.PatientID && x.IsDeleted == 0).ToList();
                    var existingTest = new txn_testresults();
                    var existingTestDetails = new List<txn_testresults_details>();

                    if (!existingTestList.Any())
                    {
                        return null;
                    }
                    else
                    {
                        foreach (txn_testresults existingTestTemp in existingTestList)
                        {
                            existingTestDetails = ctx.txn_testresults_details.AsNoTracking().Where(x => x.TestResultRowID == existingTestTemp.ID).ToList();
                            var parameterTemp = existingTestDetails.Select(x => x.TestParameter);

                            if (parameterTemp.Contains(sPreviousParameter) && !parameterTemp.Contains(sCurrentParameter))
                            {
                                existingTest = existingTestTemp;
                                break;
                            }
                        }
                    }

                    if (existingTest != null && existingTest.ID != 0)
                    {
                        existingTest.TestResultDateTime = sTestResult.TestResultDateTime;
                        existingTest.OverallStatus = sTestResult.OverallStatus;
                        existingTest.UpdatedDate = DateTime.Now;
                        existingTest.UpdatedBy = "VCheckViewer Listener";

                        ctx.txn_Testresults.Update(existingTest);
                        ctx.SaveChanges();

                        var existingTestDetails_namelist = existingTestDetails.Select(x => x.TestParameter);
                        var sTestResultDetail_NotExistNamelist = sTestResultDetail.Where(x => !existingTestDetails_namelist.Contains(x.TestParameter));
                        var sTestResultDetail_ExistNamelist = sTestResultDetail.Where(x => existingTestDetails_namelist.Contains(x.TestParameter));

                        foreach (var testDetail_NotExist in sTestResultDetail_NotExistNamelist)
                        {
                            testDetail_NotExist.TestResultRowID = existingTest.ID;

                            ctx.txn_testresults_details.Add(testDetail_NotExist);
                            ctx.SaveChanges();
                            existingTestDetails.Add(testDetail_NotExist);
                        }

                        sCurrentTestResult = existingTest;

                        return existingTestDetails;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("VCheckListenerWorker >>> TestResultRepository >>> createTestResultsMultipleParam >>> UpdateCortisolResult >>> " + e.ToString());
                return null;
            }
        }
    }
}
