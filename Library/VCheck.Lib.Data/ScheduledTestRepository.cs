using log4net;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace VCheck.Lib.Data
{
    public class ScheduledTestRepository
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private static DateTime MinimumDatetime = DateTime.Now.ToUniversalTime().AddHours(-48);
        private static string[] TwoWayCommAnalyzers = { "C10", "H6", "V200", "V2400", "C1" };

        public static DateTime MinimumDatetimeOverall = DateTime.Now.ToUniversalTime().AddDays(-11);

        public static List<ScheduledTestModel> CurrentScheduleData = InitiateScheduleData();

        public static List<ScheduledTestModel> InitiateScheduleData()
        {
            try
            {
                using (var ctx = new ScheduleDBContext(Host.CreateApplicationBuilder().Configuration))
                {
                    return ctx.Txn_ScheduledTests.Where(x => x.ScheduledDateTime >= MinimumDatetimeOverall).ToList();
                }
            }
            catch (Exception ex)
            {
                return new List<ScheduledTestModel>();
            }
        }

        public static void RefreshData()
        {
            CurrentScheduleData = InitiateScheduleData();
        }

        /// <summary>
        /// Get upcoming scheduled tests
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static List<ScheduledTestModel> GetCurrentScheduledTestList(IConfiguration config)
        {
            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                return CurrentScheduleData.Where(x => x.ScheduledDateTime >= MinimumDatetime && x.ScheduleTestStatus == 0)
                                                .OrderBy(x => x.ScheduledDateTime)
                                                .Take(5)
                                                .ToList();


            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Scheduled Test  Info by Unique ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static ScheduledTestModel GetScheduledTestByUniqueID(IConfiguration config, String sUniqueID, String? sPatientID = "")
        {
            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                if (string.IsNullOrEmpty(sPatientID))
                {
                    return CurrentScheduleData.Where(x => x.ScheduleUniqueID == sUniqueID).FirstOrDefault();
                }
                else if (string.IsNullOrEmpty(sUniqueID))
                {
                    return CurrentScheduleData.FirstOrDefault(x => x.PatientID == sPatientID && x.ScheduleTestStatus == 0);
                }
                else if (sPatientID == "Unique")
                {
                    return CurrentScheduleData.FirstOrDefault(x => x.ScheduleUniqueID.Substring(x.ScheduleUniqueID.Length - 8) == sUniqueID && x.ScheduleTestStatus != 2);
                }
                else
                {
                    return CurrentScheduleData.Where(x => x.ScheduleUniqueID.Substring(x.ScheduleUniqueID.Length - 8) == sUniqueID && x.PatientID == sPatientID).OrderByDescending(y => y.CreatedDate).LastOrDefault();
                }
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Scheduled Test  Info by Unique ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static ScheduledTestModel GetScheduledTestByOrderID(IConfiguration config, String sOrderID, String? sClientName, String? sLocationID)
        {
            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                if (string.IsNullOrEmpty(sLocationID))
                {
                    return CurrentScheduleData.Where(x => x.CreatedBy == sClientName).AsEnumerable().FirstOrDefault(y => y.ScheduleUniqueID.Split("-")[1] == sOrderID);
                }
                else
                {
                    return CurrentScheduleData.Where(x => x.CreatedBy == sClientName && x.LocationID == sLocationID).AsEnumerable().FirstOrDefault(y => y.ScheduleUniqueID.Split("-")[1] == sOrderID);
                }
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Update Scheduled Test Information by Unique ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sScheduledTestObj"></param>
        /// <returns></returns>
        public static Boolean UpdateScheduledTestByUniqueID(IConfiguration config, ScheduledTestModel sScheduledTestObj)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new ScheduleDBContext(config))
                {
                    var sData = CurrentScheduleData.Where(x => x.ScheduleUniqueID == sScheduledTestObj.ScheduleUniqueID).FirstOrDefault();
                    if (sData != null)
                    {
                        if (sData.ScheduledDateTime != sScheduledTestObj.ScheduledDateTime)
                        {
                            sData.ScheduledDateTime = sScheduledTestObj.ScheduledDateTime;
                        }

                        if (sData.InchargePerson != sScheduledTestObj.InchargePerson)
                        {
                            sData.InchargePerson = sScheduledTestObj.InchargePerson;
                        }

                        if (sData.ScheduleTestStatus != sScheduledTestObj.ScheduleTestStatus)
                        {
                            sData.ScheduleTestStatus = sScheduledTestObj.ScheduleTestStatus;
                        }

                        if (sData.SentToAnalyzer != sScheduledTestObj.SentToAnalyzer)
                        {
                            sData.SentToAnalyzer = sScheduledTestObj.SentToAnalyzer;
                        }

                        if (sData.TestCompleted != sScheduledTestObj.TestCompleted)
                        {
                            sData.TestCompleted = sScheduledTestObj.TestCompleted;
                        }

                        if (sData.UpdatedBy != sScheduledTestObj.UpdatedBy)
                        {
                            sData.UpdatedBy = sScheduledTestObj.UpdatedBy;
                        }

                        if (sData.UpdatedDate != sScheduledTestObj.UpdatedDate)
                        {
                            sData.UpdatedDate = sScheduledTestObj.UpdatedDate;
                        }

                        ctx.Update(sData);
                        ctx.SaveChanges();

                        isSuccess = true;

                        RefreshData();
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
        /// Update Scheduled Test Information by Order ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sScheduledTestObj"></param>
        /// <returns></returns>
        public static Boolean UpdateScheduledTestStatus(IConfiguration config, ScheduledTestModel scheduledTest, string sUpdatedBy)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new ScheduleDBContext(config))
                {
                    //var sData = ctx.Txn_ScheduledTests.Where(x => x.CreatedBy == sClientName && x.LocationID == sLocationID).AsEnumerable().Where(y => y.ScheduleUniqueID.Split("-")[1] == sOrderID);
                    //if (sData != null)
                    //{
                    //    foreach(var sSchedule in sData)
                    //    {
                    //        if (sSchedule.ScheduleTestStatus != sStatus)
                    //        {
                    //            sSchedule.ScheduleTestStatus = sStatus;
                    //            sSchedule.UpdatedDate = DateTime.Now;
                    //            sSchedule.UpdatedBy = sUpdatedBy;
                    //        }
                    //    }

                    //    ctx.Update(scheduledTest);

                    //    ctx.SaveChanges();

                    //    isSuccess = true;
                    //}

                    ctx.Update(scheduledTest);

                    ctx.SaveChanges();

                    isSuccess = true;

                    RefreshData();
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
        /// Insert new Scheduled Test 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sScheduledTestObj"></param>
        /// <returns></returns>
        public static Boolean InsertUpdateScheduledTest(IConfiguration config, ScheduledTestModel sScheduledTestObj)
        {
            Boolean isSuccess = false;

            try
            {
                using (var ctx = new ScheduleDBContext(config))
                {
                    ctx.Txn_ScheduledTests.Update(sScheduledTestObj);
                    ctx.SaveChanges();

                    isSuccess = true;

                    RefreshData();
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
        /// Get Scheduled Test  Info by Unique ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static ScheduledTestModel GetScheduledTestByID(IConfiguration config, long sID)
        {
            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                return CurrentScheduleData.Where(x => x.ID == sID).FirstOrDefault();
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Scheduled Test List Info by Location ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static List<ScheduledTestModel> GetScheduleListByLocation(IConfiguration config, string locationID, bool usingLocal = false)
        {
            DateTime MinimumDatetime = usingLocal ? DateTime.Now.AddHours(-48) : DateTime.Now.ToUniversalTime().AddHours(-48);
            var sBuilder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            int dataRowLimit = usingLocal ? 50 : sBuilder.Configuration.GetValue<int>("DataRowLimit");

            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                return CurrentScheduleData.Where(x => x.ScheduledDateTime >= MinimumDatetime && x.LocationID == locationID)
                                                .OrderByDescending(x => x.CreatedDate)
                                                .Take(dataRowLimit)
                                                .ToList();
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Scheduled Test List Info by Location ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static List<ScheduledTestModel> GetNotCompletedScheduleListByLocation(IConfiguration config, string locationID, string testName, bool ExtendDateTime, bool usingLocal = false)
        {
            DateTime MinimumDatetime = usingLocal ? DateTime.Now : DateTime.Now.ToUniversalTime();
            var sBuilder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            int dataRowLimit = usingLocal ? 50 : sBuilder.Configuration.GetValue<int>("DataRowLimit");

            if (ExtendDateTime)
            {
                MinimumDatetime = MinimumDatetime.AddDays(-10);
            }
            else
            {
                MinimumDatetime = MinimumDatetime.AddHours(-48);
            }

            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                var scheduleList = CurrentScheduleData.Where(x => x.ScheduledDateTime >= MinimumDatetime && x.LocationID == locationID && x.ScheduleTestStatus != 2).ToList();

                scheduleList = ExtendDateTime ? scheduleList : scheduleList.Where(x => x.TestCompleted == 0).ToList();

                return scheduleList.AsEnumerable().Where(x => x.ScheduledTestType.Split(", ").Contains(testName))
                                .OrderBy(x => x.ScheduledDateTime)
                                .Take(dataRowLimit)
                                .ToList();
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Scheduled Test List Info by Location ID that espired
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static List<ScheduledTestModel> GetExpiredScheduleListByLocation(IConfiguration config, string locationID)
        {
            try
            {
                using (var ctx = new ScheduleDBContext(config))
                {
                    var expiredOrder = CurrentScheduleData.Where(x => x.ScheduledDateTime < MinimumDatetime && x.LocationID == locationID && x.TestCompleted == 0 && x.ScheduleTestStatus < 3).ToList();

                    foreach (var item in expiredOrder)
                    {
                        item.TestCompleted = 1;
                    }

                    ctx.UpdateRange(expiredOrder);
                    ctx.SaveChanges();

                    RefreshData();

                    return expiredOrder;
                }
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Not Sent Scheduled Test List Info by Location ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        public static List<ScheduledTestModelExtended> GetScheduleListByLocationNotSent(IConfiguration config, string locationID, string uniqueID = null, bool ignoreOrderStatus = false)
        {
            List<ScheduledTestModelExtended> scheduledTestModelExtendeds = new List<ScheduledTestModelExtended>();

            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                List<ScheduledTestModel> ScheduleList = new List<ScheduledTestModel>();

                if (string.IsNullOrEmpty(uniqueID))
                {
                    if (ignoreOrderStatus)
                    {
                        ScheduleList = CurrentScheduleData.Where(x => x.ScheduledDateTime >= MinimumDatetime && x.ScheduleTestStatus != 2 && x.LocationID == locationID && x.TestCompleted == 0).ToList();
                    }
                    else
                    {
                        ScheduleList = CurrentScheduleData.Where(x => x.ScheduledDateTime >= MinimumDatetime && x.ScheduleTestStatus == 0 && x.LocationID == locationID && x.TestCompleted == 0).ToList();
                    }
                }
                else
                {
                    ScheduleList = CurrentScheduleData.Where(x => x.ScheduleUniqueID.Substring(x.ScheduleUniqueID.Length - 8) == uniqueID).ToList();
                }


                foreach (var schedule in ScheduleList)
                {
                    //var parameters =  TestResultsRepository.GetAllTestParameterByTestName(config, schedule.ScheduledTestType.Split(","));
                    //ScheduledTestModelExtended scheduledExtended = new ScheduledTestModelExtended() { Schedule = schedule, Parameters = parameters };
                    //var testIDAnalyzers = TestResultsRepository.GetAllAnalyzerByTestName(config, schedule.ScheduledTestType.Split(", "));
                    //ScheduledTestModelExtended scheduledExtended = new ScheduledTestModelExtended() { Schedule = schedule, IDAnalyzers = testIDAnalyzers };

                    string[] sentAnalyzer = Array.Empty<string>();
                    sentAnalyzer = schedule.SentToAnalyzer != null ? schedule.SentToAnalyzer.Split(",") : Array.Empty<string>();
                    var testIDAnalyzers = TestResultsRepository.GetAllAnalyzerByTestName(config, schedule.ScheduledTestType.Split(", "));
                    //testIDAnalyzers = testIDAnalyzers != null && testIDAnalyzers.Count() != 0 ? testIDAnalyzers.Where(x => !sentAnalyzer.Contains(x.Analyzers) && TwoWayCommAnalyzers.Contains(x.Analyzers)).ToList() : testIDAnalyzers;
                    testIDAnalyzers = testIDAnalyzers != null && testIDAnalyzers.Count() != 0 ? testIDAnalyzers.Where(x => !sentAnalyzer.Intersect(x.Analyzers.Split(", ")).Any() && TwoWayCommAnalyzers.Intersect(x.Analyzers.Split(", ")).Any()).ToList() : testIDAnalyzers;

                    if (testIDAnalyzers != null && testIDAnalyzers.Count() != 0)
                    {
                        ScheduledTestModelExtended scheduledExtended = new ScheduledTestModelExtended() { Schedule = schedule, IDAnalyzers = testIDAnalyzers };

                        scheduledTestModelExtendeds.Add(scheduledExtended);
                    }
                }

                return scheduledTestModelExtendeds;
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get Scheduled Test List Info by Location ID & Patient ID
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sUniqueID"></param>
        /// <returns></returns>
        //public static ScheduledTestModel GetScheduleByLocationPatientID(IConfiguration config, string locationID, string patientID, string[] testType)
        public static ScheduledTestModel GetScheduleByLocationPatientID(IConfiguration config, string locationID, string patientID, string testName)
        {
            try
            {
                if (CurrentScheduleData.Count == 0)
                {
                    RefreshData();
                }

                var scheduleList = CurrentScheduleData.Where(x => x.ScheduledDateTime >= MinimumDatetime && x.PatientID == patientID && x.LocationID == locationID && x.TestCompleted == 0);
                var schedule = scheduleList.AsEnumerable().OrderByDescending(y => y.CreatedDate).FirstOrDefault(x => x.ScheduledTestType.Split(", ").Contains(testName));

                //var schedule = ctx.Txn_ScheduledTests.OrderByDescending(y => y.CreatedDate).FirstOrDefault(x => x.ScheduledDateTime >= MinimumDatetime && x.PatientID == patientID && x.LocationID == locationID && x.TestCompleted == 0 && x.ScheduledTestType == testName);

                return schedule;
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                return null;
            }
        }
    }
}
