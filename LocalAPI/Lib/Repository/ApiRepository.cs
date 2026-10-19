using LocalAPI.Lib.Util;
using LocalAPI.Message.TestData;
using log4net;
using System.Collections.Generic;
using System.Reflection;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace LocalAPI.Lib.Repository
{
    public class ApiRepository
    {
        private ClientAuthDBContext _AuthContext = new ClientAuthDBContext();

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        public bool Authenticate(string clientKey, out int CanViewOther)
        {
            CanViewOther = 0;

            try
            {
                var ClientAuthIndex = _AuthContext.Mst_Client_Auth.FirstOrDefault(x => x.ClientKey == clientKey);
                CanViewOther = ClientAuthIndex != null ? ClientAuthIndex.CanViewOthers : 0;

                return ClientAuthIndex != null && _AuthContext.Mst_Client.Where(x => x.ID == ClientAuthIndex.ClientID && x.Status == 1).Any();
            }
            catch (Exception ex)
            {
                log.Error("Database Error >>> ", ex);
                return false;
            }
        }

        public ClientModel GetClientProfileByClientKey(String sClientKey)
        {
            try
            {
                var sClientAuth = _AuthContext.Mst_Client_Auth.Where(x => x.ClientKey == sClientKey).FirstOrDefault();
                if (sClientAuth != null)
                {
                    return _AuthContext.Mst_Client.Where(x => x.ID == sClientAuth.ClientID).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                log.Error("ApiRepository >>> GetClientProfileByClientKey >>> " + ex.ToString());
            }

            return null;
        }

        public int GetAccessionNo(string clientName, string orderID)
        {
            var accessionNo = 0;
            try
            {
                var client = _AuthContext.Mst_Client.FirstOrDefault(x => x.Name == clientName);
                accessionNo = client.RunningNo;
                client.RunningNo = accessionNo + 1;

                _AuthContext.Update(client);
                _AuthContext.SaveChanges();

                return accessionNo;
            }
            catch (Exception ex)
            {

                log.Error("Database Error >>> ", ex);
                return 0;
            }
        }

        public bool ValidateTestInfo(string TestUniqueID, string Species, string Gender, out int isMismatchedWrongUniqueIDError, out string TestName)
        {
            isMismatchedWrongUniqueIDError = 0;
            var uniqueKey = TestUniqueID.Split("-");
            TestName = "";

            if (Gender == "Male Neutered")
            {
                Gender = "Neutered Male";
            }
            else if (Gender == "Female Spayed")
            {
                Gender = "Spayed Female";
            }

            try
            {
                var data = TestResultsRepository.GetTestByUniqueID(ConfigSettings.GetConfigurationSettings(), uniqueKey[0]);
                if (data == null)
                {
                    return false;
                }
                else if ((data.Analyzer != "C1" && !data.Species.ToLower().Split(",").Contains(Species.ToLower())) || !data.Gender.ToLower().Split(",").Contains(Gender.ToLower()))
                {
                    isMismatchedWrongUniqueIDError = 1;
                    return false;
                }
                else if (uniqueKey.Length == 1)
                {
                    isMismatchedWrongUniqueIDError = 3;
                    return false;
                }
                else
                {
                    TestName = data.TestName.Replace(" (C1)", "").Replace(" (C10)", "");
                    return true;
                }
            }
            catch (Exception ex)
            {

                log.Error("Database Error >>> ", ex);

                isMismatchedWrongUniqueIDError = 2;
                return false;
            }
        }

        public void UpdateScheduledTest(string scheduledUniqueID, string scheduledDatetime, string inchargePerson, string analyzerName, string updatedBy, out ScheduledTestModel returnData, out string responseCode, out string responseMessage, out string responseStatus)
        {
            returnData = null;

            try
            {
                var data = ScheduledTestRepository.GetScheduledTestByUniqueID(ConfigSettings.GetConfigurationSettings(), scheduledUniqueID);
                if (data == null)
                {
                    responseCode = "VV.0002";
                    responseMessage = "No Data Found";
                }
                else if ((!String.IsNullOrEmpty(scheduledDatetime) && data.ScheduledDateTime != DateTime.ParseExact(scheduledDatetime, "yyyyMMddHHmmss", null)) || (!String.IsNullOrEmpty(inchargePerson) && data.InchargePerson != inchargePerson))
                {
                    data.ScheduledDateTime = (!String.IsNullOrEmpty(scheduledDatetime)) ? DateTime.ParseExact(scheduledDatetime, "yyyyMMddHHmmss", null) : data.ScheduledDateTime;
                    data.InchargePerson = (!String.IsNullOrEmpty(inchargePerson)) ? inchargePerson : data.InchargePerson;
                    data.UpdatedBy = updatedBy;
                    data.UpdatedDate = DateTime.Now;

                    ScheduledTestRepository.UpdateScheduledTestByUniqueID(ConfigSettings.GetConfigurationSettings(), data);

                    responseCode = "VV.0001";
                    responseMessage = "Success";
                    returnData = data;
                }
                else if (!string.IsNullOrEmpty(analyzerName) && (data.SentToAnalyzer == null || !data.SentToAnalyzer.Split(",").Contains(analyzerName)))
                {
                    data.SentToAnalyzer = data.SentToAnalyzer == null ? analyzerName : data.SentToAnalyzer + "," + analyzerName;
                    data.UpdatedBy = updatedBy;
                    data.UpdatedDate = DateTime.Now;

                    ScheduledTestRepository.UpdateScheduledTestByUniqueID(ConfigSettings.GetConfigurationSettings(), data);

                    responseCode = "VV.0001";
                    responseMessage = "Success";
                    returnData = data;
                }
                else
                {
                    responseCode = "VV.2001";
                    responseMessage = "No Changes Detected";
                    returnData = data;
                }

                responseStatus = "Success";
            }
            catch (Exception ex)
            {
                responseCode = "VV.9999";
                responseStatus = "Fail";
                responseMessage = "Internal Error";

                log.Error("Database Error >>> ", ex);
            }
        }

        public void CancelScheduledTestByOrderID(string orderID, string locationID, string updatedBy, out string responseCode, out string responseMessage, out string responseStatus)
        {
            try
            {
                var data = ScheduledTestRepository.GetScheduledTestByOrderID(ConfigSettings.GetConfigurationSettings(), orderID, updatedBy, locationID);
                if (data == null)
                {
                    responseCode = "VV.0002";
                    responseMessage = "No Data Found";
                }
                else if (data.ScheduleTestStatus == 2)
                {
                    responseCode = "VV.0001";
                    responseMessage = "Scheduled test are already canceled.";
                }
                else
                {
                    data.ScheduleTestStatus = 2;
                    data.TestCompleted = 1;
                    data.UpdatedBy = updatedBy;
                    data.UpdatedDate = DateTime.Now.ToUniversalTime();

                    ScheduledTestRepository.UpdateScheduledTestStatus(ConfigSettings.GetConfigurationSettings(), data, updatedBy);

                    responseCode = "VV.0001";
                    responseMessage = "Scheduled test successfully canceled.";
                }

                responseStatus = "Success";
            }
            catch (Exception ex)
            {
                responseCode = "VV.9999";
                responseStatus = "Fail";
                responseMessage = "Internal Error";

                log.Error("Database Error >>> ", ex);
            }
        }

        public List<TestResultListing> GetTestResultByScheduleUniqueID(string sUniqueID)
        {
            List<TestResultModel> TestInfos = new List<TestResultModel>();
            var sScheduleData = ScheduledTestRepository.GetScheduledTestByUniqueID(ConfigSettings.GetConfigurationSettings(), sUniqueID);
            List<TestResultListing> resultListing = new List<TestResultListing>();

            if (sScheduleData != null) 
            {
                TestInfos = TestResultsRepository.GetTestResultMatchSchedule(ConfigSettings.GetConfigurationSettings(), sScheduleData.PatientID, sScheduleData.ScheduledTestType);

                foreach (var testInfo in TestInfos)
                {
                    resultListing.Add(new TestResultListing() { TestName = testInfo.TestResultType, TestResultDetails = TestResultsRepository.GetResultDetailsByTestResultID(ConfigSettings.GetConfigurationSettings(), testInfo.ID) });
                }

            }

            return resultListing;

        }
    }
}
