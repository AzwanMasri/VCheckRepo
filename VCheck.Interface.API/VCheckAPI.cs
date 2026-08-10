using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API.Lib.General;

namespace VCheck.Interface.API
{
    public class VCheckAPI
    {
        //private string url = "https://localhost:7245/"; // local
        private string url = "http://vcheckcentral.inteleon.xyz/"; // Testing
        //private string url = "http://vcheckstaging.inteleon.xyz/"; // Staging
        //private string url = "https://www.vcheckviewer.com/"; // prod with SSL

        private string clientKey = "qwertyuiop123asdfghjkl456zxcvbnm789";

        X509Certificate2 rootCaCert = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Certificates/Sectigo Public Server Authentication Root R46.crt"));
        HttpClientHandler handler = new HttpClientHandler();

        /// <summary>
        /// Get handler
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public void InitiateCertHandler()
        {
            try
            {
                handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                    {
                        // If there are no SSL policy errors, accept
                        if (errors == SslPolicyErrors.None)
                            return true;

                        // Build a custom chain including our root cert
                        using (var customChain = new X509Chain())
                        {
                            customChain.ChainPolicy.ExtraStore.Add(rootCaCert);
                            customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                            bool isValid = customChain.Build(new X509Certificate2(cert));

                            // Check if our root cert is in the chain
                            bool rootFound = false;
                            foreach (var element in customChain.ChainElements)
                            {
                                if (element.Certificate.Thumbprint == rootCaCert.Thumbprint)
                                {
                                    rootFound = true;
                                    break;
                                }
                            }

                            return isValid && rootFound;
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                handler = null;
            }
        }

        /// <summary>
        /// Test connection
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<bool> TestConnection()
        {
            InitiateCertHandler();

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    var resp = await client.GetAsync(url + "Api/testconnection");
                    string content = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine(content);

                    if (resp.StatusCode.ToString() == "OK")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Is Latest Version
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<bool> IsLatestVersion(string version)
        {
            InitiateCertHandler();

            ClientDataRequest request = new ClientDataRequest() { Header = new HeaderModel(), Body = new ClientDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.Version = version;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/IsLatestVersion", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        if(results.Body.Results != null)
                        {
                            return (bool)results.Body.Results;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        /// <summary>
        /// Update Location
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> UpdateLocation(LocationModel location)
        {
            InitiateCertHandler();

            UpdateLocationDataRequest request = new UpdateLocationDataRequest() { Header = new HeaderModel(), Body = new UpdateLocationDataRequestBody()};

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            var temp = JsonConvert.SerializeObject(location);
            request.Body = JsonConvert.DeserializeObject<UpdateLocationDataRequestBody>(temp);

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/UpdateLocation", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);
                                                
                        return results.Body.Results != null ? results.Body.Results.ToString() : null;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Update Location
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetLocation(string ClinicID = null)
        {
            InitiateCertHandler();

            LocationDataRequest request = new LocationDataRequest() { Header = new HeaderModel(), Body = new GetLocationDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.ClinicID = ClinicID;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetLocationList", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return results.Body.Results != null ? results.Body.Results.ToString() : null;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Update Location
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetScheduleList(string locationID, bool OnlyNotCompleted, bool OnlyExpired, string testName = null, bool ExtendDateTime = false)
        {
            InitiateCertHandler();

            ScheduleDataRequest request = new ScheduleDataRequest() { Header = new HeaderModel(), Body = new ScheduleDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;
            var endpoint = "";
            
            if (OnlyNotCompleted)
            {
                endpoint = "Api/GetNotCompletedScheduleListByLocation";
            }
            else if(OnlyExpired)
            {
                endpoint = "Api/GetAllExpiredSchedule";
            }
            else
            {
                endpoint = "Api/GetScheduleListByLocation";
            }

            request.Body.LocationID = locationID;
            request.Body.TestName = testName;
            request.Body.ExtendDateTime = ExtendDateTime;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + endpoint, content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return JsonConvert.SerializeObject(results.Body.Results);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Update Location
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetScheduleListNotSent(string locationID, string uniqueID = null)
        {
            InitiateCertHandler();

            ScheduleDataRequest request = new ScheduleDataRequest() { Header = new HeaderModel(), Body = new ScheduleDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.LocationID = locationID;
            request.Body.ScheduledUniqueID = uniqueID;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetScheduleListByLocationNotSent", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return JsonConvert.SerializeObject(results.Body.Results);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Get Schedule
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        //public async Task<string> GetSchedule(string locationID, string patientID, List<string> parameters, string uniqueID = null)
        public async Task<string> GetSchedule(string locationID, string patientID, string testName, string uniqueID = null)
        {
            InitiateCertHandler();

            ScheduleDataRequest request = new ScheduleDataRequest() { Header = new HeaderModel(), Body = new ScheduleDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.LocationID = locationID;
            request.Body.PatientID = patientID;
            request.Body.TestName = testName;
            request.Body.ScheduledUniqueID = uniqueID;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetScheduleByLocationPatientID", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return JsonConvert.SerializeObject(results.Body.Results);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Cancel Schedule
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<bool> UpdateScheduleStatus(string locationID, string patientID, string orderID, string clientName, int status, string testName = "")
        {
            InitiateCertHandler();

            ScheduleDataRequest request = new ScheduleDataRequest() { Header = new HeaderModel(), Body = new ScheduleDataRequestBody() };

            var endpoint = "";

            if (status == 1)
            {
                endpoint = "Api/SentScheduledTest";
            }
            else if(status == 2)
            {
                endpoint = "Api/CancelScheduledTest";
            }
            else if (status == 3)
            {
                endpoint = "Api/LinkScheduledTest";
            }
            else if (status == 4)
            {
                endpoint = "Api/CloseScheduledTest";
            }

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.LocationID = locationID;
            request.Body.PatientID = patientID;
            request.Body.OrderID = orderID;
            request.Body.ClientName = clientName;
            request.Body.Status = status;
            request.Body.TestName = testName;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + endpoint, content);

                    return resp.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }/// <summary>
         /// Cancel Schedule
         /// </summary>
         /// <param name="sResultRequest"></param>
         /// <returns></returns>
        public async Task<bool> UpdateScheduleAnalyzer(string analyzerName, string uniqueID)
        {
            InitiateCertHandler();

            ScheduleDataRequest request = new ScheduleDataRequest() { Header = new HeaderModel(), Body = new ScheduleDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.AnalyzerName = analyzerName;
            request.Body.ScheduledUniqueID = uniqueID;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/UpdateScheduledTest", content);

                    return resp.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        /// <summary>
        /// Get PMS URL
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetPMSUrl(int clientID)
        {
            InitiateCertHandler();

            URLDataRequest request = new URLDataRequest() { Header = new HeaderModel(), Body = new URLDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.ClientID = clientID;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetPMSURL", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return results.Body.Results.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        /// <summary>
        /// Download latest version zip
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<bool> DownloadLatestInstaller()
        {
            InitiateCertHandler();

            ClientDataRequest request = new ClientDataRequest() { Header = new HeaderModel(), Body = new ClientDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

                    string destinationPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

                    //string fileName = "VCheck Viewer Installer.exe";
                    string fileName = "Vcheck Patch.zip";

                    using (HttpResponseMessage response = await client.PostAsync(url + "File/DownloadLatestInstaller", content))
                    {
                        response.EnsureSuccessStatusCode();

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                      fileStream = new FileStream(destinationPath + "\\" + fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Download latest patch
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<bool> DownloadLatestPatch()
        {
            InitiateCertHandler();

            ClientDataRequest request = new ClientDataRequest() { Header = new HeaderModel(), Body = new ClientDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

                    string destinationPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

                    //string fileName = "VCheck Viewer Installer.exe";
                    string fileName = "Vcheck Patch.exe";

                    using (HttpResponseMessage response = await client.PostAsync(url + "File/DownloadLatestPatch", content))
                    {
                        response.EnsureSuccessStatusCode();

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                      fileStream = new FileStream(destinationPath + "\\" + fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Get API Error List
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetErrorList(string ClinicID, string? StartDate, string? EndDate)
        {
            InitiateCertHandler();

            ErrorDataRequest request = new ErrorDataRequest() { Header = new HeaderModel(), Body = new ErrorDataRequestBody() { ClinicID = ClinicID, StartDate = StartDate, EndDate = EndDate } };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetErrorList", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return JsonConvert.SerializeObject(results.Body.Results);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Get API Error List Unread Count
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<int> GetTotalErrorUnread(string ClinicID)
        {
            InitiateCertHandler();

            ErrorDataRequest request = new ErrorDataRequest() { Header = new HeaderModel(), Body = new ErrorDataRequestBody() { ClinicID = ClinicID } };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetTotalErrorUnread", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return int.Parse(results.Body.Results.ToString());
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        /// <summary>
        /// Get API Error List Not Sync
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetAllErrorNotSync(string ClinicID)
        {
            InitiateCertHandler();

            ErrorDataRequest request = new ErrorDataRequest() { Header = new HeaderModel(), Body = new ErrorDataRequestBody() { ClinicID = ClinicID } };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetAllErrorNotSync", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return JsonConvert.SerializeObject(results.Body.Results);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Get Test by Name or Code
        /// </summary>
        /// <param name="sResultRequest"></param>
        /// <returns></returns>
        public async Task<string> GetTestByNameOrCode(string testName, string testCode)
        {
            InitiateCertHandler();

            TestDataRequest request = new TestDataRequest() { Header = new HeaderModel(), Body = new TestDataRequestBody() };

            request.Header.timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            request.Header.clientKey = clientKey;

            request.Body.TestName = testName;
            request.Body.TestCode = testCode;

            String strJson = JsonConvert.SerializeObject(request);
            HttpContent content = new StringContent(strJson, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));


                    HttpResponseMessage resp = await client.PostAsync(url + "Api/GetTestByNameOrCode", content);
                    if (resp.IsSuccessStatusCode)
                    {
                        var resultString = resp.Content.ReadAsStringAsync().Result;
                        var results = JsonConvert.DeserializeObject<ResponseModel>(resultString);

                        return JsonConvert.SerializeObject(results.Body.Results);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
