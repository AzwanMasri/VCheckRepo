using Local_API.Lib.Models.ScheduleData;
using Local_API.Lib.Models.TestData;
using Local_API.Lib.Repositories;
using Local_API.Lib.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace Local_API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class APIController : ControllerBase
    {
        private APIRepository _apiRepository = new APIRepository();
        private int CanViewOther;

        /// <summary>
        /// Test connection
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet(Name = "TestConnection")]
        public ActionResult<ResponseModel> TestConnection()
        {
            var response = new ResponseModel();
            response.Header = new HeaderModel();
            response.Body = new ResponseBody();

            response.Header.timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            response.Header.clientKey = "";

            response.Body.ResponseCode = "VV.0001";
            response.Body.ResponseStatus = "Success";
            response.Body.ResponseMessage = "Success";
            response.Body.Results = null;

            return response;
        }/// <summary>
         /// API for Insert Scheduled Test
         /// </summary>
         /// <param name="request"></param>
         /// <returns></returns>
        [HttpPost(Name = "SendOrderInfo")]
        public ActionResult<ResponseModel> SendOrderInfo(CreateScheduledDataRequest request)
        {
            var sResp = new ResponseModel();
            sResp.Header = new HeaderModel() { timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), clientKey = request.Header.clientKey }; ;
            sResp.Body = new ResponseBody();
            OrderInfo orderInfo = new OrderInfo();

            String sRespCode = "";
            String sRespStatus = "";
            String sRespMessage = "";

            int isMismatchedWrongUniqueIDError = 0;
            string TestName = "";
            string clientName = "Unauthorized User";
            request.body.LocationID = "Local";

            try
            {
                if (request.body.ValidateMandatoryField())
                {
                    _apiRepository.RefreshData();
                    if (_apiRepository.Authenticate(request.Header.clientKey, out CanViewOther))
                    {
                        if (_apiRepository.ValidateTestInfo(request.body.TestUniqueID, request.body.Species, request.body.Gender, out isMismatchedWrongUniqueIDError, out TestName))
                        {
                            ClientModel sAuthProfile = _apiRepository.GetClientProfileByClientKey(request.Header.clientKey);

                            ScheduledTestRepository.RefreshData();
                            clientName = (sAuthProfile != null) ? sAuthProfile.Name : "";
                            var uniqueKey = request.body.TestUniqueID.Split("-");
                            ScheduledTestRepository.RefreshData();
                            ScheduledTestModel scheduledObj = ScheduledTestRepository.GetScheduledTestByOrderID(ConfigSettings.GetConfigurationSettings(), uniqueKey[1], clientName, request.body.LocationID);
                            var uniqueID = "";
                            var accessionNo = 0;
                            var exist = false;

                            if (scheduledObj == null)
                            {
                                scheduledObj = new ScheduledTestModel();
                                scheduledObj.ScheduledTestType = TestName;

                                DateTime dtScheduled = DateTime.MinValue;
                                if (DateTime.TryParseExact(request.body.ScheduledDateTime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dtScheduled))
                                {
                                    scheduledObj.ScheduledDateTime = dtScheduled;
                                }

                                scheduledObj.ScheduledBy = request.body.ScheduledBy;
                                scheduledObj.InchargePerson = request.body.PersonIncharges;
                                scheduledObj.PatientID = request.body.PatientID;
                                scheduledObj.PatientName = request.body.PatientName;
                                scheduledObj.Gender = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.body.Gender.ToLower());
                                scheduledObj.Species = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.body.Species.ToLower());
                                scheduledObj.OwnerName = request.body.OwnerName;
                                scheduledObj.ScheduleTestStatus = 0;
                                scheduledObj.TestCompleted = 0;
                                scheduledObj.LocationID = "Local";
                                scheduledObj.CreatedBy = clientName;
                                scheduledObj.CreatedDate = DateTime.Now.ToUniversalTime();

                                DateTime patienttDOB;
                                if (DateTime.TryParse(request.body.PatientDOB, out patienttDOB))
                                {
                                    scheduledObj.PatientDOB = patienttDOB;
                                }

                                var uniqueIDArray = request.body.TestUniqueID.Split("-");
                                accessionNo = _apiRepository.GetAccessionNo(scheduledObj.CreatedBy, uniqueKey[1]);
                                uniqueID = request.body.TestUniqueID + "-" + accessionNo + "-" + GenerateUniqueKey(8);
                                scheduledObj.ScheduleUniqueID = uniqueID;
                                orderInfo.AccessionNo = accessionNo;
                                orderInfo.OrderID = uniqueIDArray[1];

                                if (accessionNo == 0)
                                {
                                    throw (new Exception("Error creating accession number."));
                                }
                            }
                            else if (!scheduledObj.ScheduledTestType.Contains(TestName))
                            {
                                uniqueID = scheduledObj.ScheduleUniqueID;
                                uniqueKey = uniqueID.Split("-");
                                scheduledObj.ScheduledTestType = scheduledObj.ScheduledTestType + ", " + TestName;
                                accessionNo = int.Parse(uniqueKey[2]);
                                orderInfo.AccessionNo = accessionNo;
                                orderInfo.OrderID = uniqueKey[1];
                            }
                            else
                            {
                                exist = true;
                            }

                            if (exist)
                            {
                                sRespCode = "VV.2003";
                                sRespStatus = "Fail";
                                sRespMessage = "Duplicate test for this patient.";
                            }
                            else if (ScheduledTestRepository.InsertUpdateScheduledTest(ConfigSettings.GetConfigurationSettings(), scheduledObj))
                            {
                                sRespCode = "VV.0001";
                                sRespStatus = "Success";
                                sRespMessage = "Success.";
                            }
                            else
                            {
                                sRespCode = "VV.2003";
                                sRespStatus = "Fail";
                                sRespMessage = "Failed to insert Scheduled Test.";
                            }
                        }
                        else
                        {
                            if (isMismatchedWrongUniqueIDError == 1)
                            {
                                sRespCode = "VV.1005";
                                sRespStatus = "Fail";
                                sRespMessage = "Mismatched Species or Gender";
                            }
                            else if (isMismatchedWrongUniqueIDError == 0)
                            {
                                sRespCode = "VV.1006";
                                sRespStatus = "Fail";
                                sRespMessage = "Invalid Test Unique ID";
                            }
                            else if (isMismatchedWrongUniqueIDError == 3)
                            {
                                sRespCode = "VV.1006";
                                sRespStatus = "Fail";
                                sRespMessage = "Order ID is missing.";
                            }
                            else
                            {
                                throw new Exception("Error validating schedule test info.");
                            }
                        }

                    }
                    else
                    {
                        sRespCode = "VV.0003";
                        sRespStatus = "Fail";
                        sRespMessage = "Unauthorized Request";
                    }
                }
                else
                {
                    sRespCode = "VV.0004";
                    sRespStatus = "Fail";
                    sRespMessage = "Missing Mandatory Fields";
                }

                sResp.Body.ResponseCode = sRespCode;
                sResp.Body.ResponseStatus = sRespStatus;
                sResp.Body.ResponseMessage = sRespMessage;
                sResp.Body.Results = orderInfo;
            }
            catch (Exception ex)
            {
                sResp.Body.ResponseCode = "VV.9999";
                sResp.Body.ResponseStatus = "Exception";
                sResp.Body.ResponseMessage = "Exception Error";
            }

            if (sRespCode != "VV.0001")
            {
                TemplateDBContext TemplateContext = new TemplateDBContext(ConfigSettings.GetConfigurationSettings());
                ConfigurationDBContext ConfigurationContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());
                NotificationDBContext NotificationContext = new NotificationDBContext(ConfigSettings.GetConfigurationSettings());

                ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
                var notificationTemplate = TemplateContext.GetTemplateByCodeLang("SE01", (sLangCode != null ? sLangCode.ConfigurationValue : ""));
                var temp = notificationTemplate;

                var content = temp.TemplateContent.Replace("###<testname>###", request.body.ScheduledTestName).Replace("###<name>###", request.body.PatientName).Replace("###<id>###", request.body.PatientID).Replace("###<message>###", sResp.Body.ResponseMessage);

                NotificationModel notification = new NotificationModel()
                {
                    NotificationType = "Schedule Error",
                    NotificationTitle = temp.TemplateTitle,
                    NotificationContent = content,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedBy = "Local API"
                };

                NotificationContext.InsertNotification(notification);

                return BadRequest(sResp);
            }

            return sResp;
        }

        /// <summary>
        /// Update Scheduled Test Info API
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost(Name = "UpdateScheduledTest")]
        public ResponseModel UpdateScheduledTest(ScheduleDataRequest request)
        {
            var response = new ResponseModel();
            response.Header = new HeaderModel() { timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), clientKey = request.Header.clientKey };
            response.Body = new ResponseBody();
            string responseCode = "";
            String responseMessage = "";
            String responseStatus = "";

            ScheduledTestModel result = null;
            var sResult = new List<ScheduleResultObject>();

            try
            {
                if (!String.IsNullOrEmpty(request.Body.ScheduledUniqueID))
                {
                    _apiRepository.RefreshData();
                    if (request.Header.clientKey != null && _apiRepository.Authenticate(request.Header.clientKey, out CanViewOther))
                    {
                        ScheduledTestRepository.RefreshData();
                        ClientModel sAuthProfile = _apiRepository.GetClientProfileByClientKey(request.Header.clientKey);
                        var UpdatedBy = (sAuthProfile != null) ? sAuthProfile.Name : "";
                        _apiRepository.UpdateScheduledTest(request.Body.ScheduledUniqueID, request.Body.ScheduledDatetime, request.Body.InchargePerson, request.Body.AnalyzerName, UpdatedBy,
                                                        out result, out responseCode, out responseMessage, out responseStatus);

                        if (result != null)
                        {
                            sResult.Add(new ScheduleResultObject
                            {
                                scheduleduniqueid = result.ScheduleUniqueID,
                                scheduleddatetime = (result.ScheduledDateTime != null) ?
                                                        result.ScheduledDateTime.Value.ToString("yyyyMMddHHmmss") : null,
                                scheduledtesttype = result.ScheduledTestType,
                                scheduledby = result.ScheduledBy,
                                inchargeperson = result.InchargePerson,
                                patientid = result.PatientID
                            });
                        }
                        ;
                    }
                    else
                    {
                        responseCode = "VV.0003";
                        responseStatus = "Fail";
                        responseMessage = "Unauthorized Request";
                    }
                }
                else
                {
                    responseCode = "VV.2002";
                    responseStatus = "Fail";
                    responseMessage = "Missing Scheduled Unique ID";
                }

                response.Body = new ResponseBody()
                {
                    ResponseCode = responseCode,
                    ResponseStatus = responseStatus,
                    ResponseMessage = responseMessage,
                    Results = sResult
                };
            }
            catch (Exception ex)
            {
                response.Body = new ResponseBody()
                {
                    ResponseCode = "VV.9999",
                    ResponseStatus = "Exception",
                    ResponseMessage = "Exception Error"
                };
            }

            return response;
        }

        /// <summary>
        /// Cancel Scheduled Request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost(Name = "CancelScheduledTest")]
        public ActionResult<ResponseModel> CancelScheduledTest(ScheduleDataRequest request)
        {
            var response = new ResponseModel();
            response.Header = new HeaderModel() { clientKey = request.Header.clientKey, timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") };
            response.Body = new ResponseBody();

            String responseCode = "";
            String responseMessage = "";
            String responseStatus = "";

            request.Body.LocationID = "Local";

            try
            {
                _apiRepository.RefreshData();
                if (request.Header.clientKey != null && _apiRepository.Authenticate(request.Header.clientKey, out CanViewOther))
                {
                    if (!String.IsNullOrEmpty(request.Body.OrderID))
                    {
                        ClientModel sAuthProfile = _apiRepository.GetClientProfileByClientKey(request.Header.clientKey);
                        var UpdatedBy = (sAuthProfile != null) ? sAuthProfile.Name : "";
                        UpdatedBy = string.IsNullOrEmpty(request.Body.ClientName) ? UpdatedBy : request.Body.ClientName;
                        ScheduledTestRepository.RefreshData();
                        _apiRepository.CancelScheduledTestByOrderID(request.Body.OrderID, request.Body.LocationID, UpdatedBy, out responseCode, out responseMessage, out responseStatus);
                    }
                    else
                    {
                        responseCode = "VV.2002";
                        responseStatus = "Fail";
                        responseMessage = "Missing Order ID";
                    }
                }
                else
                {
                    responseCode = "VV.0003";
                    responseStatus = "Fail";
                    responseMessage = "Unauthorized Request";
                }

                response.Body.ResponseCode = responseCode;
                response.Body.ResponseStatus = responseStatus;
                response.Body.ResponseMessage = responseMessage;

            }
            catch (Exception ex)
            {
                response.Body.ResponseCode = "VV.9999";
                response.Body.ResponseStatus = "Exception";
                response.Body.ResponseMessage = "Exception Error";
            }

            if (response.Body.ResponseCode != "VV.0001")
            {
                return BadRequest(response);
            }

            return response;
        }

        /// <summary>
        /// Get Test List
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetFullInfoTestList")]
        public ActionResult<ResponseModel> GetFullInfoTestList(TestDataRequest request)
        {
            var response = new ResponseModel();
            response.Header = new HeaderModel() { timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), clientKey = request.Header.clientKey }; ;
            response.Body = new ResponseBody();

            String responseCode = "";
            String responseMessage = "";
            String responseStatus = "";

            List<TestDataObjectExtended> sResultList = new List<TestDataObjectExtended>();

            try
            {
                _apiRepository.RefreshData();
                if (request.Header.clientKey != null && _apiRepository.Authenticate(request.Header.clientKey, out CanViewOther))
                {
                    var sTestList = TestResultsRepository.GetAllTestList(ConfigSettings.GetConfigurationSettings());
                    if (sTestList != null && sTestList.Count > 0)
                    {
                        foreach (var test in sTestList)
                        {
                            sResultList.Add(new TestDataObjectExtended
                            {
                                testid = test.TestID,
                                testname = test.TestName,
                                testdescription = test.TestDescription,
                                species = test.Species,
                                analyzers = test.Analyzer
                            });
                        }
                    }

                    responseCode = "VV.0001";
                    responseStatus = "Success";
                    responseMessage = "Success";
                }
                else
                {
                    responseCode = "VV.0003";
                    responseStatus = "Fail";
                    responseMessage = "Unauthorized Request";
                }

                response.Body.ResponseCode = responseCode;
                response.Body.ResponseStatus = responseStatus;
                response.Body.ResponseMessage = responseMessage;
                response.Body.Results = sResultList;
            }
            catch (Exception ex)
            {
                response.Body.ResponseCode = "VV.9999";
                response.Body.ResponseStatus = "Exception";
                response.Body.ResponseMessage = "Exception Error";
            }

            if (response.Body.ResponseCode != "VV.0001")
            {
                return BadRequest(response);
            }

            return response;
        }

        /// <summary>
        /// Get Test Result
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// 
        [HttpPost(Name = "GetTestResult")]
        public ActionResult<ResponseModel> GetTestResult(TestDataRequest request)
        {
            var response = new ResponseModel();
            response.Header = new HeaderModel() { timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), clientKey = request.Header.clientKey }; ;
            response.Body = new ResponseBody();

            String responseCode = "";
            String responseMessage = "";
            String responseStatus = "";

            List<TestResultListing> resultListing = new List<TestResultListing>();

            _apiRepository.RefreshData();
            if (request.Header.clientKey != null && _apiRepository.Authenticate(request.Header.clientKey, out CanViewOther))
            {
                resultListing = _apiRepository.GetTestResultByScheduleUniqueID(request.Body.ScheduleUniqueKey);

                responseCode = "VV.0001";
                responseStatus = "Success";
                responseMessage = "Success";
            }
            else
            {
                responseCode = "VV.0003";
                responseStatus = "Fail";
                responseMessage = "Unauthorized Request";
            }

            response.Body.ResponseCode = responseCode;
            response.Body.ResponseStatus = responseStatus;
            response.Body.ResponseMessage = responseMessage;
            response.Body.Results = resultListing;

            return response;
        }

        public static string GenerateUniqueKey(int size)
        {
            char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray(); ;
            byte[] data = new byte[4 * size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }
    }
}
