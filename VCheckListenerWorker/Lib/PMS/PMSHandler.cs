using Mysqlx.Crud;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VCheck.Interface.API;
using VCheckListenerWorker.Lib.Logic;
using VCheckListenerWorker.Lib.Models;

namespace VCheckListenerWorker.Lib.PMS
{
    public class PMSHandler
    {
        public string PMS;
        public string url;
        public string port;
        public string username;
        public string password;
        public bool isIP;
        public System.Net.IPAddress iIP;

        TcpClient ourTcpClient = null;
        NetworkStream networkStream = null;
        String receivedMessage = "";

        private static readonly string subscriptDigits = "₀₁₂₃₄₅₆₇₈₉₋₊";
        private static readonly string superscriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹⁻⁺";
        private static readonly string normalDigits = "0123456789-+";
        private static readonly string[] calItems = { "A/G", "B/C", "GLOB", "Na/K"};

        public async Task<bool> SendToPMS(txn_testresults sTestResults, List<txn_testresults_details> sTestResultDetails, ScheduledTestModel sScheduledTestObj, string sTestCode, string sNotes = "")
        {
            VCheck.Interface.API.Greywind.RequestMessage.UpdateResultRequest sRequestAPI = new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultRequest();

            List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelTestObject> sResultListing = new List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelTestObject>();
            List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject> sPanelListing = new List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject>();

            String sOrderID = "";
            string[] ownerName = sScheduledTestObj.OwnerName.Split(" ");
            var lastName = "";
            int start = 0;

            foreach (var name in ownerName)
            {
                if (ownerName.Length == 1)
                {
                    lastName = name;
                }
                else if (start != 0)
                {
                    lastName = string.IsNullOrEmpty(lastName) ? lastName + ownerName[start] : lastName + " " + ownerName[start];
                }

                start++;
            }

            if (sScheduledTestObj.ScheduleUniqueID.Contains("-"))
            {
                var UniqueIDSplit = sScheduledTestObj.ScheduleUniqueID.Split("-");
                if (UniqueIDSplit.Length > 0)
                {
                    sOrderID = UniqueIDSplit[1];
                    sRequestAPI.accessionnumber = UniqueIDSplit[2];
                }
            }

            //sRequestAPI.accessionnumber = iTestResultID.ToString();
            sRequestAPI.clinic_id = sScheduledTestObj.LocationID.ToString();
            sRequestAPI.reportdate = sTestResults.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
            sRequestAPI.providerid = "";

            VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPatientObject sPatientObj = new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPatientObject();
            sPatientObj.patientid = sScheduledTestObj.PatientID;
            sPatientObj.firstname = (sScheduledTestObj != null) ? sScheduledTestObj.PatientName : "";
            sPatientObj.lastname = lastName;
            sPatientObj.gender = (sScheduledTestObj != null) ? sScheduledTestObj.Gender : "";
            sPatientObj.birthday = sScheduledTestObj.PatientDOB != null ? sScheduledTestObj.PatientDOB.Value.ToString("yyyy-MM-dd") : "";
            sPatientObj.species = (sScheduledTestObj != null) ? sScheduledTestObj.Species : "";
            sPatientObj.breed = "";

            sRequestAPI.patient = sPatientObj;

            VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject sPanelObj = new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject();

            var panel = string.IsNullOrEmpty(sTestResults.TestResultType) ? "VCheck" : sTestResults.TestResultType;

            sPanelObj.code = sTestCode;
            sPanelObj.name = panel;
            sPanelObj.status = "F";
            sPanelObj.source = "";
            sPanelObj.resultdate = sTestResults.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

            var initialParameterID = sTestCode + "-" + sOrderID;


            var sDetailsObj = sTestResultDetails;
            if (sDetailsObj != null && sDetailsObj.Count > 0)
            {
                int count = 0;

                foreach (var d in sDetailsObj)
                {
                    String[] sRange = Array.Empty<string>();
                    string referenceLow = "";
                    string referenceHigh = "";



                    if (!string.IsNullOrEmpty(d.ReferenceRange))
                    {
                        String sReferenceRange = d.ReferenceRange.Replace("[", "").Replace("]", "");
                        if (sReferenceRange != "")
                        {
                            if (sReferenceRange.Contains(","))
                            {
                                var temp = sReferenceRange.Split(",");
                                string[] result = new string[2];
                                Array.Copy(temp, 0, result, 0, 1);
                                Array.Copy(temp, 2, result, 1, 1);
                                sRange = result;
                            }
                            else if (sReferenceRange.Contains(";"))
                            {
                                sRange = sReferenceRange.Split(";");
                            }
                            else if (sReferenceRange.Contains("-"))
                            {
                                sRange = sReferenceRange.Split("-");
                            }
                            else if (sReferenceRange.Contains("<"))
                            {
                                sRange = new string[] { sReferenceRange, "" };
                            }
                            else if (sReferenceRange.Contains(">"))
                            {
                                sRange = new string[] { "", sReferenceRange };
                            }


                            if (sRange.Length > 1)
                            {
                                referenceLow = (sRange.Length > 0) ? sRange[0] : "";
                                referenceHigh = (sRange.Length > 0) ? sRange[1] : "";
                            }
                        }
                    }
                    else if(!string.IsNullOrEmpty(d.MeasuringRange))
                    {
                        String sReferenceRange = d.MeasuringRange.Replace("[", "").Replace("]", "");
                        if (sReferenceRange != "")
                        {
                            if (sReferenceRange.Contains(";"))
                            {
                                sRange = sReferenceRange.Split(";");
                            }
                            else if (sReferenceRange.Contains("-"))
                            {
                                sRange = sReferenceRange.Split("-");
                            }

                            referenceLow = (sRange.Length > 0) ? sRange[0] : "";
                            referenceHigh = (sRange.Length > 0) ? sRange[1] : "";
                        }
                    }

                    var flags = "";

                    if(d.TestResultStatus == "Low") { flags = "L"; }
                    else if (d.TestResultStatus == "High") { flags = "H"; }

                    Parameter = CheckParameter(d.TestParameter);
                    //var UniqueID = UniqueCode;

                    sResultListing.Add(new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelTestObject
                    {
                        name = Parameter,
                        //code = "Test4-" + count++,
                        //code = sTestCode + "-" + count++,
                        //code = sTestCode + "-" + UniqueCode,
                        code = initialParameterID + "-" + count++,
                        result = d.TestResultValue,
                        referencelow = referenceLow,
                        referencehigh = referenceHigh,
                        unitofmeasure = d.TestResultUnit,
                        nature = d.TestResultStatus == "Normal" | d.TestResultStatus == "Positive" ? "N" : "A", 
                        flags = flags,
                        status = "F",
                        notes = d.Interpretation
                    });
                }

                sPanelObj.tests = sResultListing;
            }
            sPanelListing.Add(sPanelObj);

            sRequestAPI.panels = sPanelListing;

            GetInterfaceInfo();

            var sRespAPI = false;
            VCheckAPI VcheckAPI = new VCheckAPI();

            if (PMS == "Greywind")
            {
                GreywindAPI sAPI = new GreywindAPI();
                url = await VcheckAPI.GetPMSUrl(2);
                sRespAPI = sAPI.UpdateResult(sRequestAPI, sOrderID, url);
            }
            else
            {
                if (isIP)
                {
                    sRespAPI = await SendData(sRequestAPI, iIP, int.Parse(port));
                }
                else
                {
                    if (!(port == "80" || port == "443"))
                    {
                        var splitURL = url.Split("/");

                        if (splitURL[0].ToLower().Contains("http"))
                        {
                            splitURL[2] = splitURL[2] + ":" + port;
                        }
                        else
                        {
                            splitURL[0] = splitURL[0] + ":" + port;
                        }

                        url = string.Join("/", splitURL);
                    }

                    GeneralAPI sAPI = new GeneralAPI();
                    sRespAPI = await sAPI.SendData(url, sRequestAPI, username, password);
                }
            }

            if (sRespAPI)
            {
                sTestResults.PMSFunction = "Collapsed";

                TestResultRepository.createTestResult(sTestResults);
                return true;
            }

            return false;

        }

        public async Task<Boolean> SendData(VCheck.Interface.API.Greywind.RequestMessage.UpdateResultRequest sResultRequest, System.Net.IPAddress ip, int port)
        {
            String sContent = JsonConvert.SerializeObject(sResultRequest);
            int timeout = 5000;

            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeout);

                    try
                    {

                        ourTcpClient = new TcpClient();
                        await ourTcpClient.ConnectAsync(ip, port).WaitAsync(cts.Token);

                        //get the IO stream on this connection to write to
                        networkStream = ourTcpClient.GetStream();
                        var sendMessageByteBuffer = Encoding.UTF8.GetBytes(sContent);

                        if (networkStream.CanWrite)
                        {
                            networkStream.Write(sendMessageByteBuffer, 0, sendMessageByteBuffer.Length);
                            byte[] receiveMessageByteBuffer = new byte[ourTcpClient.ReceiveBufferSize];
                            var bytesReceivedFromServer = await networkStream.ReadAsync(receiveMessageByteBuffer, 0, receiveMessageByteBuffer.Length).WaitAsync(cts.Token);

                            if (bytesReceivedFromServer > 0 && networkStream.CanRead)
                            {
                                receivedMessage = Encoding.UTF8.GetString(receiveMessageByteBuffer);

                                var message = receivedMessage.Replace("\0", string.Empty);

                                return true;
                            }
                        }

                        return false;

                    }
                    catch (TaskCanceledException cancelled)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                //close the IO strem and the TCP connection
                networkStream?.Close();
                ourTcpClient?.Close();
            }
        }

        private void GetInterfaceInfo()
        {
            var configuration = TestResultRepository.GetAllConfiguration();

            var specificConfiguration = configuration.FirstOrDefault(x => x.ConfigurationKey == "InterfaceSettingsPMS");

            PMS = specificConfiguration != null ? specificConfiguration.ConfigurationValue : "";

            specificConfiguration = configuration.FirstOrDefault(x => x.ConfigurationKey == "InterfaceSettingsIP");
            url = specificConfiguration != null ? specificConfiguration.ConfigurationValue : "";

            isIP = System.Net.IPAddress.TryParse(url, out iIP);

            specificConfiguration = configuration.FirstOrDefault(x => x.ConfigurationKey == "InterfaceSettingsPortNo");
            port = specificConfiguration != null ? specificConfiguration.ConfigurationValue : "80";

            specificConfiguration = configuration.FirstOrDefault(x => x.ConfigurationKey == "InterfaceSettingsUsername");
            username = specificConfiguration != null ? specificConfiguration.ConfigurationValue : "";

            specificConfiguration = configuration.FirstOrDefault(x => x.ConfigurationKey == "InterfaceSettingsPassword");
            password = specificConfiguration != null ? specificConfiguration.ConfigurationValue : "";
        }

        public static string CheckParameter(string input)
        {
            foreach (var item in calItems) 
            {
                if(input.ToLower().Contains(item.ToLower())) { return item + "*"; }
            }

            return ConvertSubscriptsToNormal(input);
        }

        public static string ConvertSubscriptsToNormal(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            StringBuilder result = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                int subindex = subscriptDigits.IndexOf(c);
                int superindex = superscriptDigits.IndexOf(c);
                if (subindex >= 0 || superindex >= 0) 
                {
                    result.Append(subindex >= 0 ? normalDigits[subindex] : normalDigits[superindex]); // Replace with normal digit
                    break;
                }                    
                else
                    result.Append(c); // Keep unchanged
            }

            return result.ToString();
        }

        public static string Parameter { get; set; }
        public static string UniqueCode => Parameter switch
        {
            "Canine CRP" => "1",
            "fSAA3.0" => "2",
            "cPL2" => "3",
            "fPL2" => "4",
            "SDMA" => "5",
            "D-dimer" => "6",
            "f.NT-proBNP" => "7",
            "c.NT-proBNP" => "8",
            "cTnI" => "9",
            "fTnI" => "10",
            "T4" => "11",
            "cTSH" => "12",
            "cCortisol" => "13",
            "cPRG" => "14",
            "cPRG2.0" => "15",
            "Equine SAA" => "16",
            "ePRG" => "17",
            "Foal IgG" => "18",
            "WBC" => "19",
            "KET" => "20",
            "NIT" => "21",
            "URO" => "22",
            "BIL" => "23",
            "GLU" => "24",
            "PRO" => "25",
            "SG" => "26",
            "PH" => "27",
            "BLD" => "28",
            "VC" => "29",
            "MA" => "30",
            "CA" => "31",
            "CR" => "32",
            "PCR" => "33",
            "LYM%" => "34",
            "MON%" => "35",
            "NEU%" => "36",
            "EOS%" => "37",
            "BASO%" => "38",
            "LYM#" => "39",
            "MON#" => "40",
            "NEU#/HETERO" => "41",
            "EOS#" => "42",
            "BASO#" => "43",
            "IMG%" => "44",
            "IMG#" => "45",
            "RBC" => "46",
            "HGB" => "47",
            "HCT" => "48",
            "MCV" => "49",
            "MCH" => "50",
            "MCHC" => "51",
            "RDW_CV" => "52",
            "RDW_SD" => "53",
            "RET%" => "54",
            "RET#" => "55",
            "IRF" => "56",
            "LFR" => "57",
            "MFR" => "58",
            "HFR" => "59",
            "RHE" => "60",
            "PLT" => "61",
            "MPV" => "62",
            "PDW" => "63",
            "PCT" => "64",
            "P_LCR" => "65",
            "P_LCC" => "66",
            "IPF" => "67",
            "UREA" => "68",
            "UA" => "69",
            "TP" => "70",
            "TG" => "71",
            "TC" => "72",
            "TB" => "73",
            "PHOS" => "74",
            "Na" => "75",
            "Mg" => "76",
            "LDH" => "77",
            "K" => "78",
            "GLOB" => "79",
            "GGT" => "80",
            "Crea" => "81",
            "Cl" => "82",
            "CK" => "83",
            "BUN" => "84",
            "AST" => "85",
            "AMY" => "86",
            "ALT" => "87",
            "ALP" => "88",
            "ALB" => "89",
            "tCO2" => "90",
            "LAC" => "91",
            "LPS" => "92",
            "TBA" => "93",
            "U/C" => "94",
            "NH3" => "95",
            "HbA1c" => "96",
            "FRUC" => "97",
            "DB" => "98",
            "CHE" => "99",
            "c-Cys C" => "100",
            "c-CRP" => "101",
            "BUN/CREA" => "102",
            "f-SAA" => "103",
            "K+" => "104",
            "Na+" => "105",
            "TT-1" => "106",
            "PT-1" => "107",
            "Fib-1" => "108",
            "APTT" => "109",
            "TBIL" => "110",
            "B/C" => "111",
            "Globulin" => "112",
            "Urine Protein" => "113",
            "Urine Creatinine" => "114",
            "TRIG" => "115",
            "CHOL" => "116",
            "LIPA" => "117",
            "UPC" => "118",
            _ => "General"
        };
    }
}
