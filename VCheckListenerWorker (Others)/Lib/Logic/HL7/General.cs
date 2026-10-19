using System.Globalization;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using VCheckListenerWorker_Others.Lib.Models;

namespace VCheckListenerWorker_Others.Lib.Logic.HL7
{
    public class General
    {
        /// <summary>
        /// Process Observation Results Status
        /// </summary>
        /// <param name="isRangeReference"></param>
        /// <param name="sResultValue"></param>
        /// <param name="sReferenceRange"></param>
        /// <param name="dResultValue"></param>
        /// <returns></returns>
        public static String ProcessObservationResultStatusValue(Boolean isRangeReference, String sResultValue, String sReferenceRange, Decimal dResultValue)
        {
            String sRetStatus = "";

            if (isRangeReference)
            {
                if(!String.IsNullOrEmpty(sReferenceRange) && Regex.IsMatch(sReferenceRange, "[A-Za-z]"))
                {
                    sRetStatus = "Normal";
                }
                else if (String.IsNullOrEmpty(sResultValue))
                {
                    sRetStatus = "Abnormal";
                }
                else if (sResultValue.ToLower() != "invalid")
                {
                    Decimal dTargetValue = 0;
                    Decimal dMinusOne = Convert.ToDecimal("0.001", CultureInfo.InvariantCulture);
                    if (!String.IsNullOrEmpty(sResultValue))
                    {
                        sResultValue = sResultValue.Replace("<", "").Replace("nan", "");
                        Decimal.TryParse(sResultValue, CultureInfo.InvariantCulture, out dTargetValue);
                        //dTargetValue = dTargetValue - dMinusOne;
                    }

                    Decimal dRangeA = 0;
                    Decimal dRangeB = 0;

                    if (!String.IsNullOrEmpty(sReferenceRange))
                    {
                        String[] strRange = [];
                        if (sReferenceRange.Contains(";"))
                        {
                            strRange = (sReferenceRange.Replace("[", "").Replace("]", "")).Split(";");
                        }
                        else if (sReferenceRange.Contains("-"))
                        {
                            strRange = sReferenceRange.Split("-");
                        }

                        if (strRange.Length > 1)
                        {
                            Decimal.TryParse(strRange[0], CultureInfo.InvariantCulture, out dRangeA);
                            Decimal.TryParse(strRange[1], CultureInfo.InvariantCulture, out dRangeB);
                        }

                        if ((dRangeA - dMinusOne) < dTargetValue && dTargetValue < (dRangeB + dMinusOne))
                        {
                            sRetStatus = "Normal";
                        }
                        else
                        {
                            sRetStatus = "Abnormal";
                        }
                    }
                    else
                    {
                        sRetStatus = "Normal";
                    }
                }
                else
                {
                    sRetStatus = "Invalid";
                }
            }
            else
            {
                if (sResultValue.ToLower() == "invalid")
                {
                    sRetStatus = "Invalid";
                }
                else
                {
                    sRetStatus = "Normal";
                }
            }

            return sRetStatus;
        }

        /// <summary>
        /// Process Observation Results Status for U3
        /// </summary>
        /// <param name="isRangeReference"></param>
        /// <param name="sResultValue"></param>
        /// <param name="sReferenceRange"></param>
        /// <param name="dResultValue"></param>
        /// <returns></returns>
        public static String ProcessObservationResultStatusValueU3(Boolean isRangeReference, String sResultValue, String sReferenceRange, Decimal dResultValue)
        {
            String sRetStatus = "";

            if (isRangeReference)
            {
                if (!Regex.IsMatch(sReferenceRange, "[A-Za-z]"))
                {
                    Decimal dTargetValue = 0;
                    var referenceRangeTemp = sReferenceRange;
                    if (!String.IsNullOrEmpty(sResultValue))
                    {
                        if (sResultValue.Contains("-") && sResultValue.Length > 1)
                        {
                            var resultTemp = sResultValue.Split("-");
                            sResultValue = resultTemp[0];
                        }

                        sResultValue = sResultValue.Replace("<", "").Replace("=", "").Replace("nan", "");
                        Decimal.TryParse(sResultValue, CultureInfo.InvariantCulture, out dTargetValue);
                    }

                    if (sReferenceRange.Contains("<"))
                    {
                        var tempReferenceRange = referenceRangeTemp.Replace("<", "").Replace("=", "");
                        sRetStatus = dTargetValue <= Decimal.Parse(tempReferenceRange, CultureInfo.InvariantCulture) ? "Normal" : "Abnormal";
                    }
                    else
                    {
                        Decimal dRangeA = 0;
                        Decimal dRangeB = 0;

                        String[] strRange = [];
                        if (sReferenceRange.Contains(";"))
                        {
                            strRange = (sReferenceRange.Replace("[", "").Replace("]", "")).Split(";");
                        }
                        else if (sReferenceRange.Contains("-"))
                        {
                            strRange = sReferenceRange.Split("-");
                        }

                        if (strRange.Length > 1)
                        {
                            Decimal.TryParse(strRange[0], CultureInfo.InvariantCulture, out dRangeA);
                            Decimal.TryParse(strRange[1], CultureInfo.InvariantCulture, out dRangeB);


                            if (dRangeA <= dTargetValue && dTargetValue <= dRangeB)
                            {
                                sRetStatus = "Normal";
                            }
                            else
                            {
                                sRetStatus = "Abnormal";
                            }
                        }
                        else
                        {
                            if (dTargetValue == Decimal.Parse(sReferenceRange, CultureInfo.InvariantCulture))
                            {
                                sRetStatus = "Normal";
                            }
                            else
                            {
                                sRetStatus = "Abnormal";
                            }
                        }
                    }
                }
                else
                {
                    sRetStatus = "Normal";
                }
            }
            else
            {
                sRetStatus = "Normal";
            }

            return sRetStatus;
        }

        /// <summary>
        /// Process Observation Results Status According to reference range
        /// </summary>
        /// <param name="isRangeReference"></param>
        /// <param name="sResultValue"></param>
        /// <param name="sReferenceRange"></param>
        /// <param name="dResultValue"></param>
        /// <returns></returns>
        public static String ProcessObservationResultStatusValueReferenceRange(string sAnalyzer, String sResultValue, string sParameter, String sSpecies, string sAgeGroup, out string sReferenceRange, out string sMeasuringRange)
        {
            string sStatus = "";
            sReferenceRange = "";
            sMeasuringRange = "";

            try
            {
                TestResultReferenceRangeModel referenceRange = TestResultRepository.GetReferenceRangeByParameterAnalyzerSpecies(sParameter, sAnalyzer, sSpecies, sAgeGroup);
                if (referenceRange == null || referenceRange.ID == 0 || sResultValue == null)
                {
                    return sStatus;
                }

                sMeasuringRange = referenceRange.MeasuringRange;
                var resultValue = float.Parse(sResultValue.Replace("<","").Replace(">", ""), CultureInfo.InvariantCulture);

                if (!string.IsNullOrEmpty(referenceRange.NormalGrayZoneAbnormal))
                {
                    var rangeList = referenceRange.NormalGrayZoneAbnormal.Split(", ");
                    sReferenceRange = referenceRange.NormalGrayZoneAbnormal;

                    if (rangeList.Count() > 1)
                    {
                        sStatus = "Abnormal";

                        if (rangeList[0].Contains("<="))
                        {
                            if(resultValue <= float.Parse(rangeList[0].Replace("<=", ""), CultureInfo.InvariantCulture))
                            {
                                sStatus = "Normal";
                            }
                        }
                        else
                        {
                            if (resultValue < float.Parse(rangeList[0].Replace("<", ""), CultureInfo.InvariantCulture))
                            {
                                sStatus = "Normal";
                            }
                        }

                        if (sStatus == "Abnormal" && rangeList[1].Contains("-"))
                        {
                            var grayzone = rangeList[1].Split("-");

                            if(resultValue > float.Parse(grayzone[0], CultureInfo.InvariantCulture) && resultValue < float.Parse(grayzone[1], CultureInfo.InvariantCulture))
                            {
                                sStatus = "Gray Zone";
                            }
                        }

                        //sReferenceRange = rangeList[0].Replace("<=", "").Replace("<", "") + "-" + rangeList[2].Replace(">=", "").Replace(">", "");
                    }
                    else
                    {
                        //sReferenceRange = rangeList[0];

                        if (rangeList[0].Contains("<"))
                        {
                            if (resultValue < float.Parse(rangeList[0].Replace("<", ""), CultureInfo.InvariantCulture))
                            {
                                sStatus = "Normal";
                            }
                            else
                            {
                                sStatus = "Abnormal";
                            }
                        }
                        else if (rangeList[0].Contains("-"))
                        {
                            var normalZone = rangeList[0].Split("-");

                            if (resultValue >= float.Parse(normalZone[0], CultureInfo.InvariantCulture) && resultValue <= float.Parse(normalZone[1], CultureInfo.InvariantCulture))
                            {
                                sStatus = "Normal";
                            }
                            else
                            {
                                sStatus = "Abnormal";
                            }
                        }
                        else
                        {
                            if(sResultValue == rangeList[0])
                            {
                                sStatus = "Normal";
                            }
                            else
                            {
                                sStatus = "Abnormal";
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(referenceRange.LowNormalHigh))
                {
                    sReferenceRange = referenceRange.LowNormalHigh;

                    if (sReferenceRange.Contains("<="))
                    {
                        sReferenceRange = referenceRange.LowNormalHigh.Replace("<=", "") + "-" + referenceRange.LowNormalHigh.Replace("<=", "");
                    }

                    var normalZone = sReferenceRange.Split("-");

                    if (resultValue > (float.Parse(normalZone[0], CultureInfo.InvariantCulture) - (float)0.001) && resultValue < (float.Parse(normalZone[1], CultureInfo.InvariantCulture) + (float)0.001))
                    {
                        sStatus = "Normal";
                    }
                    else if (resultValue < float.Parse(normalZone[0], CultureInfo.InvariantCulture))
                    {
                        sStatus = "Low";
                    }
                    else
                    {
                        sStatus = "High";
                    }
                }

                return string.IsNullOrEmpty(sStatus) ? "Normal" : sStatus;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //public static String ProcessObservationResultValue(String sResultTestType, String sResultValue)
        //{
        //    String sRetStatus = "";

        //    try
        //    {
        //        switch (sResultTestType)
        //        {
        //            case "fPL2":
        //                sRetStatus = StatusInterpretation.processfPL2ResultStatus(sResultValue);
        //                break;

        //            case "fTnl":
        //                sRetStatus = StatusInterpretation.processFeliniTnlResultStatus(sResultValue);
        //                break;

        //            case "cTnI":
        //                sRetStatus = StatusInterpretation.processCanineTnIResultStatus(sResultValue);
        //                break;

        //            default:
        //                break;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    return sRetStatus;
        //}
    }
}
