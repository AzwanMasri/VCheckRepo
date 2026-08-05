using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer.Lib.Function;
using Document = QuestPDF.Fluent.Document;

namespace VCheckViewer.Lib.DocumentTemplate
{
    public class TestResultTemplate : IDocument
    {
        private List<DownloadPrintResultModel> sDownloadPrintResultModels { get; set; }
        private bool sIsPrint { get; set; }

        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public TestResultTemplate(List<DownloadPrintResultModel> sDownloadPrintResultModels, bool sIsPrint)
        {
            this.sDownloadPrintResultModels = sDownloadPrintResultModels;
            this.sIsPrint = sIsPrint;
        }

        public void Compose(IDocumentContainer container)
        {
            var sReportImagePath = configDBContext.GetConfigurationData("ReportImagePath").FirstOrDefault();
            var sClinicName = configDBContext.GetConfigurationData("ClinicName").FirstOrDefault();
            var sReportTitle = configDBContext.GetConfigurationData("ReportTitle").FirstOrDefault();
            var sClinicAddress = configDBContext.GetConfigurationData("ClinicAddress").FirstOrDefault();
            var sClinicPhoneNum = configDBContext.GetConfigurationData("ClinicPhoneNum").FirstOrDefault();
            var sBuilder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            String sIconConfigPath = sBuilder.Configuration.GetSection("PrintTemplate:IconPath").Value;
            String sFooterLogo = System.Environment.CurrentDirectory + sIconConfigPath;
            String sIconPath = sReportImagePath != null && File.Exists(sReportImagePath.ConfigurationValue) ? sReportImagePath.ConfigurationValue : sFooterLogo;
            String sDownloadPath = sBuilder.Configuration.GetSection("Configuration:DownloadFolderPath").Value;
            string graphFolder = sBuilder.Configuration.GetSection("Configuration:GraphFolder").Value;

            try
            {
                var sDocument = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A3);
                        page.MarginHorizontal(2, Unit.Centimetre);
                        page.MarginBottom(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));
                        page.DefaultTextStyle(x => x.FontFamily("Noto Sans"));

                        page.Header().BorderBottom(1).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.ConstantItem(7, Unit.Centimetre).PaddingLeft(20).PaddingRight(20).AlignCenter().AlignMiddle().Image(sIconPath);

                                row.RelativeItem().AlignMiddle()
                                .Column(middleColumn =>
                                {

                                    middleColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingRight(5)
                                            .Text(text =>
                                            {
                                                text.Span((sReportTitle != null) ? sReportTitle.ConfigurationValue : "").Bold();
                                            });
                                    });

                                    middleColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingRight(5)
                                            .PaddingTop(10)
                                            .Text(text =>
                                            {
                                                text.Span((sClinicName != null) ? sClinicName.ConfigurationValue : "").Bold();
                                            });
                                    });

                                    middleColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingRight(5)
                                            .PaddingTop(10)
                                            .AlignLeft()
                                            .Text(text =>
                                            {
                                                text.Span((sClinicAddress != null) ? sClinicAddress.ConfigurationValue : "").Bold();
                                            });
                                    });

                                    middleColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingRight(5)
                                            .PaddingTop(10)
                                            .Text(text =>
                                            {
                                                text.Span((sClinicPhoneNum != null) ? "+" +  sClinicPhoneNum.ConfigurationValue : "").Bold();
                                            });
                                    });
                                });

                                row.ConstantItem(10, Unit.Centimetre).AlignCenter().AlignMiddle()
                                .Column(rightColumn =>
                                {
                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span(Properties.Resources.Results_Label_PatientID + " : ").Bold();
                                                text.Span(sDownloadPrintResultModels[0].TestResult.PatientID);
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span(Properties.Resources.Results_Label_PatientName + " : ").Bold();
                                                text.Span(sDownloadPrintResultModels[0].TestResult.PatientName);
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span("Species : ").Bold();
                                                text.Span(sDownloadPrintResultModels[0].TestResult.Species);
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span("Age : ").Bold();
                                                text.Span(sDownloadPrintResultModels[0].TestResult.Age);
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span("Weight : ").Bold();
                                                text.Span(sDownloadPrintResultModels[0].TestResult.Weight);
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span(Properties.Resources.Results_Label_Doctor + " : ").Bold();
                                                text.Span(sDownloadPrintResultModels[0].TestResult.InchargePerson);
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span(Properties.Resources.Report_Label_PrintedOn + " : ").Bold();
                                                text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                                            });
                                    });

                                    rightColumn.Item().Column(col =>
                                    {
                                        col.Item()
                                            .PaddingLeft(5)
                                            .PaddingTop(10)
                                            .MinHeight(20)
                                            .Text(text =>
                                            {
                                                text.Span(Properties.Resources.Report_Label_PrintedBy + " : ").Bold();
                                                text.Span(App.MainViewModel.CurrentUsers.FullName);
                                            });
                                    });
                                });
                            });
                        });

                        //--Content-- //
                        page.Content()
                            .Column(c =>
                            {
                                int totalTest = sDownloadPrintResultModels.Count();
                                int current = 0;
                                foreach (var test in sDownloadPrintResultModels)
                                {
                                    current++;
                                    var DeviceName = DeviceRepository.GetDeviceNameBySerialNo(ConfigSettings.GetConfigurationSettings(), test.TestResult.DeviceSerialNo);
                                    DeviceName = DeviceName == "General" ? Properties.Resources.Schedule_Label_General : DeviceName;
                                    bool hasIC = test.TestResultDetails.Any(x => x.TestParameter.Contains(" IC"));

                                    c.Item().EnsureSpace().Column(c =>
                                    {
                                        c.Item().MinHeight(25);

                                        c.Item().MinHeight(20).Text(text =>
                                        {
                                            text.Span(DeviceName + " ").ExtraBold().FontSize(15);
                                            text.Span(" (" + test.TestResult.TestResultDateTime.Value.ToString("dd/MM/yyyy HH:mm") + ")").FontSize(8);
                                        });

                                        c.Item().Border(1).Row(tableRow =>
                                        {
                                            tableRow.RelativeItem().Column(column =>
                                            {
                                                column.Item().Background("#f2f2f2").Row(row =>
                                                {
                                                    row.RelativeItem()
                                                    .Column(leftColumn =>
                                                    {
                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .PaddingTop(10)
                                                                .MinHeight(20)
                                                                .AlignCenter()
                                                                .Text(Properties.Resources.Report_Label_Parameter)
                                                                .Bold();
                                                        });
                                                    });

                                                    row.RelativeItem()
                                                    .Column(leftColumn =>
                                                    {
                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .PaddingTop(10)
                                                                .MinHeight(20)
                                                                .AlignCenter()
                                                                .Text(Properties.Resources.Report_Label_ResultValue)
                                                                .Bold();
                                                        });
                                                    });

                                                    row.RelativeItem()
                                                    .Column(leftColumn =>
                                                    {
                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .PaddingTop(10)
                                                                .MinHeight(20)
                                                                .AlignCenter()
                                                                .Text(Properties.Resources.Report_Label_ResultStatus)
                                                                .Bold();
                                                        });
                                                    });

                                                    row.RelativeItem()
                                                    .Column(leftColumn =>
                                                    {
                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .PaddingTop(10)
                                                                .MinHeight(20)
                                                                .AlignCenter()
                                                                .Text(Properties.Resources.Report_Label_Reference)
                                                                .Bold();
                                                        });
                                                    });

                                                    row.RelativeItem()
                                                    .Column(leftColumn =>
                                                    {
                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .PaddingTop(10)
                                                                .MinHeight(20)
                                                                .AlignCenter()
                                                                .Text(" ");
                                                        });

                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().PaddingRight(5).PaddingTop(1).AlignCenter().AlignMiddle().MinHeight(15).MinWidth(110).Layers(layer =>
                                                            {
                                                                // Background bar
                                                                layer.PrimaryLayer().PaddingRight((float)73.3333).Text(Properties.Resources.Report_Label_Low).AlignCenter().FontSize(10);

                                                                layer.Layer().PaddingLeft((float)36.6666).PaddingRight((float)36.6666).Text(Properties.Resources.Schedule_Label_Normal).AlignCenter().FontSize(10);


                                                                layer.Layer().PaddingLeft((float)73.3333).Text(Properties.Resources.Report_Label_High).AlignCenter().FontSize(10);
                                                            });
                                                        });
                                                    });

                                                    row.RelativeItem()
                                                    .Column(leftColumn =>
                                                    {
                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .PaddingTop(10)
                                                                .MinHeight(20)
                                                                .AlignCenter()
                                                                .Text(Properties.Resources.Report_Label_PreviousRecord)
                                                                .Bold();
                                                        });

                                                        var previousTestDateTime = "-";

                                                        if (test.PreviousTestResult != null)
                                                        {
                                                            previousTestDateTime = test.PreviousTestResult.TestResultDateTime.Value.ToString("dd/MM/yyyy hh:mm");
                                                        }

                                                        leftColumn.Item().Column(col =>
                                                        {
                                                            col.Item().Background("#f2f2f2")
                                                                .PaddingLeft(5)
                                                                .AlignTop()
                                                                .AlignCenter()
                                                                .Text("(" + previousTestDateTime + ")")
                                                                .FontSize(8);
                                                        });
                                                    });
                                                });

                                                var testResultDetails = test.TestResultDetails.OrderBy(d => d.ID).ToList();

                                                foreach (var d in testResultDetails)
                                                {
                                                    var lowNormalHigh = 1;
                                                    var arrowIcon = "\\Content\\Images\\Down-Arrow.png";
                                                    var foreground = "#000000";
                                                    var testStatus = "";
                                                    var showIcon = false;
                                                    var reference = "-";
                                                    var paddingLeft = 0;
                                                    var paddingRight = 0;
                                                    var haveIndicator = false;
                                                    float resultValue = 0;
                                                    var testResultValue = !string.IsNullOrEmpty(d.TestResultValue) && !(d.TestResultValue.Contains("-") && !d.TestResultValue.StartsWith("-"));
                                                    testResultValue = testResultValue ? float.TryParse(d.TestResultValue.Replace("< ", "").Replace("> ", "").Replace("=", ""), CultureInfo.InvariantCulture, out resultValue) : false;
                                                    //
                                                    if (!string.IsNullOrEmpty(d.ReferenceRange) && d.ReferenceRange != "0" && !d.ReferenceRange.Contains("<") && !Regex.IsMatch(d.ReferenceRange, "[A-Za-z]") && !hasIC && testResultValue)
                                                    {
                                                        var range = d.ReferenceRange.Replace("[", "").Replace("]", "").Contains(";") ? d.ReferenceRange.Replace(" ", "").Replace("[", "").Replace("]", "").Split(";") : d.ReferenceRange.Replace(" ", "").Replace("[", "").Replace("]", "").Split("-"); ;
                                                        reference = range[0] + " - " + range[1];
                                                        paddingLeft = (CalculateRange(float.Parse(range[0].Replace("[", ""), CultureInfo.InvariantCulture), float.Parse(range[1].Replace("]", ""), CultureInfo.InvariantCulture), resultValue, out lowNormalHigh)) - 2;
                                                        paddingRight = 110 - paddingLeft - 2;
                                                        haveIndicator = true;
                                                    }
                                                    else if (!string.IsNullOrEmpty(d.ReferenceRange))
                                                    {
                                                        reference = d.ReferenceRange;
                                                    }

                                                    if (d.TestResultValue == "-")
                                                    {
                                                        testStatus = "-";
                                                    }
                                                    else if (string.IsNullOrEmpty(d.TestResultStatus))
                                                    {
                                                        testStatus = "Error";
                                                    }
                                                    else if (d.TestResultStatus.ToLower() == "positive")
                                                    {
                                                        testStatus = Properties.Resources.Dashboard_Label_Positive;
                                                    }
                                                    else if (d.TestResultStatus.ToLower() == "abnormal")//
                                                    {
                                                        testStatus = Properties.Resources.Schedule_Label_Abnormal;
                                                        foreground = "#ff0000";
                                                    }
                                                    else if (d.TestResultStatus.ToLower() == "normal")
                                                    {
                                                        testStatus = Properties.Resources.Schedule_Label_Normal;
                                                    }
                                                    else if (d.TestResultStatus.ToLower() == "negative")
                                                    {
                                                        testStatus = Properties.Resources.Dashboard_Label_Negative;
                                                    }
                                                    else
                                                    {
                                                        testStatus = d.TestResultStatus;
                                                    }

                                                    if (lowNormalHigh == 0)
                                                    {
                                                        arrowIcon = "/Content/Images/Down-Arrow-Light.png";
                                                        foreground = "#0011ff";
                                                        showIcon = true;
                                                    }
                                                    else if (lowNormalHigh == 2)
                                                    {
                                                        arrowIcon = "/Content/Images/Up-Arrow.png";
                                                        foreground = "#ff0000";
                                                        showIcon = true;
                                                    }

                                                    column.Item().Row(row =>
                                                    {
                                                        row.RelativeItem()
                                                        .Column(leftColumn =>
                                                        {
                                                            leftColumn.Item().Column(col =>
                                                            {
                                                                col.Item()
                                                                    .PaddingLeft(5)
                                                                    .PaddingTop(10)
                                                                    .MinHeight(20)
                                                                    .AlignCenter()
                                                                    .Text(text =>
                                                                    {
                                                                        if (showIcon)
                                                                        {
                                                                            text.Element().PaddingBottom(-1).Height(10).Image(System.Environment.CurrentDirectory + arrowIcon);
                                                                        }
                                                                        text.Span(d.TestParameter.Contains(" IC") ? "IC" : d.TestParameter).FontColor(foreground);
                                                                    });
                                                            });
                                                        });

                                                        row.RelativeItem()
                                                        .Column(leftColumn =>
                                                        {
                                                            leftColumn.Item().Column(col =>
                                                            {
                                                                col.Item()
                                                                    .PaddingLeft(5)
                                                                    .PaddingTop(10)
                                                                    .MinHeight(20)
                                                                    .AlignCenter()
                                                                    .Text(d.TestResultValue + (!string.IsNullOrEmpty(d.TestResultUnit) && d.TestResultValue.Contains(d.TestResultUnit) ? "" : " " + d.TestResultUnit)).FontColor(foreground);
                                                            });
                                                        });

                                                        row.RelativeItem()
                                                        .Column(leftColumn =>
                                                        {
                                                            leftColumn.Item().Column(col =>
                                                            {
                                                                col.Item()
                                                                    .PaddingLeft(5)
                                                                    .PaddingTop(10)
                                                                    .MinHeight(20)
                                                                    .AlignCenter()
                                                                    .Text(testStatus).FontColor(foreground);
                                                            });
                                                        });

                                                        row.RelativeItem()
                                                        .Column(leftColumn =>
                                                        {
                                                            leftColumn.Item().Column(col =>
                                                            {
                                                                col.Item()
                                                                    .PaddingLeft(5)
                                                                    .PaddingTop(10)
                                                                    .MinHeight(20)
                                                                    .AlignCenter()
                                                                    .Text(hasIC ? "-" : reference);
                                                            });
                                                        });
                                                        //
                                                        row.RelativeItem().PaddingRight(5).PaddingTop(1).AlignCenter().AlignMiddle().MinHeight(15).MinWidth(110).Layers(layer =>
                                                        {
                                                            if (haveIndicator && !hasIC)
                                                            {
                                                                // Background bar
                                                                layer.PrimaryLayer().Border(1);

                                                                layer.Layer().PaddingRight((float)73.3333).BorderRight(1);


                                                                layer.Layer().PaddingLeft((float)73.3333).BorderLeft(1);


                                                                layer.Layer().PaddingLeft(paddingLeft).PaddingRight(paddingRight).Background(foreground);
                                                            }
                                                            else
                                                            {
                                                                layer.PrimaryLayer().Text("-").AlignCenter();
                                                            }
                                                        });

                                                        var previousValue = "-";

                                                        if (test.PreviousTestResultDetails != null)
                                                        {
                                                            var previousParameter = test.PreviousTestResultDetails != null ? test.PreviousTestResultDetails.FirstOrDefault(x => x.TestParameter == d.TestParameter) : null;
                                                            previousValue = (previousParameter != null && !string.IsNullOrEmpty(previousParameter.TestResultValue)) ? previousParameter.TestResultValue : "-";
                                                        }

                                                        row.RelativeItem()
                                                        .Column(leftColumn =>
                                                        {
                                                            leftColumn.Item().Column(col =>
                                                            {
                                                                col.Item()
                                                                    .PaddingLeft(5)
                                                                    .PaddingTop(10)
                                                                    .MinHeight(20)
                                                                    .AlignCenter()
                                                                    .Text(previousValue);
                                                            });
                                                        });

                                                    });
                                                }
                                            });
                                        });

                                        if (test.TestResult.Analyze_TableRowID != null && test.TestResult.Analyze_TableRowID != 0)
                                        {
                                            var sTestResultSpecimentContainer = TestResultsRepository.GetTestResultSpecimenContainerByID(ConfigSettings.GetConfigurationSettings(), test.TestResult.Analyze_TableRowID);

                                            if (sTestResultSpecimentContainer != null)
                                            {
                                                c.Item().MinHeight(10);

                                                c.Item().MinHeight(10).Text(text =>
                                                {
                                                    text.Span("Serum Index").ExtraBold().FontSize(15);
                                                });

                                                c.Item().MinHeight(10).Text(text =>
                                                {
                                                    text.Span("HEM: " + sTestResultSpecimentContainer.HemolysisIndex).FontSize(15);
                                                    text.Span(" / ").FontSize(15);
                                                    text.Span("LIP: " + sTestResultSpecimentContainer.LipemiaIndex).FontSize(15);
                                                    text.Span(" / ").FontSize(15);
                                                    text.Span("ICT: " + sTestResultSpecimentContainer.IcterusIndex).FontSize(15);
                                                });
                                            }
                                        }

                                        c.Item().MinHeight(25);

                                        var garphPerRow = 3;
                                        int remainder = 0;
                                        var graph = test.TestResultsGraph;
                                        int totalRow = 0;
                                        int currentGraphIndex = 0;

                                        if (test.TestResultsGraph.Count() == 4) { garphPerRow = 2; }
                                        else if (test.TestResultsGraph.Count() < 3 && test.TestResultsGraph.Count() != 0) { garphPerRow = test.TestResultsGraph.Count(); }

                                        if (graph.Count() > 0)
                                        {
                                            using (System.Drawing.Image img = System.Drawing.Image.FromFile(graphFolder + test.TestResult.ID + "\\" + graph[currentGraphIndex].FileName))
                                            {
                                                garphPerRow = img.Width > 800 ? 1 : garphPerRow;
                                            }
                                        }

                                        if (garphPerRow != 3) { totalRow = test.TestResultsGraph.Count(); }
                                        else
                                        {
                                            totalRow = Math.DivRem(test.TestResultsGraph.Count(), garphPerRow, out remainder);
                                        }

                                        if (remainder > 0) { totalRow++; }

                                        for (int i = 0; i < totalRow; i++)
                                        {
                                            var totalRowGraph = 0;

                                            if (i == totalRow - 1 && remainder != 0) { totalRowGraph = remainder; }
                                            else { totalRowGraph = garphPerRow; }

                                            c.Item().Row(row =>
                                            {
                                                for (int j = 0; j < totalRowGraph; j++)
                                                {
                                                    row.RelativeItem().Image(graphFolder + test.TestResult.ID + "\\" + graph[currentGraphIndex].FileName);
                                                    currentGraphIndex++;
                                                }
                                            });

                                        }

                                        if (current != totalTest) { c.Item().PageBreak(); }

                                    });
                                }
                            });

                        page.Footer().AlignRight()
                        .Row(row =>
                        {
                            row.ConstantItem(3, Unit.Centimetre).Text(Properties.Resources.Report_Label_PoweredBy).FontColor("#aeb3b5").Bold();
                            row.ConstantItem(4, Unit.Centimetre).Image(sFooterLogo);
                        });

                    });
                });

                //QuestPDF.Settings.EnableDebugging = true;

                if (sIsPrint)
                {
                    App.FilePath = "Report-temp.pdf";
                    sDocument.GeneratePdf(App.FilePath);

                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = App.FilePath,
                        UseShellExecute = true
                    };
                    process.Start();
                }
                else
                {
                    var testResultType = "MultipleTest";
                    if(sDownloadPrintResultModels.Count == 1)
                    {
                        testResultType = string.IsNullOrEmpty(sDownloadPrintResultModels[0].TestResult.TestResultType) ? "General" : sDownloadPrintResultModels[0].TestResult.TestResultType.Replace(" ", "");
                    }

                    var patientID = string.IsNullOrEmpty(sDownloadPrintResultModels[0].TestResult.PatientID) ? "General" : sDownloadPrintResultModels[0].TestResult.PatientID.Replace(" ", "");
                    String sFileName = "TestResult_" + testResultType +
                                       "_PatientID_" + patientID + "_" +
                                       DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                    String sOutputPath = System.IO.Path.Combine(sDownloadPath, sFileName);
                    sDocument.GeneratePdf(sOutputPath);
                }

                sDocument = null;
                App.ErrorOccur = false;
            }
            catch (Exception ex)
            {
                //throw ex;
                App.ErrorOccur = true;
                App.log.Error("Print/Download Error >>> ", ex);
            }

        }

        public int CalculateRange(float start, float end, float value, out int lowNormalHigh)
        {
            float onceWidth = (float)36.6666;
            float twiceWidth = (float)73.3333;
            float fullwidth = 110;
            lowNormalHigh = 0;

            int position = 0;
            float range = end - start;
            float actualStart = start - range;
            float actualEnd = end + range;
            float percentage = 0;

            if (value >= start && end >= value)
            {
                float balance = value - start;
                percentage = (balance / range) * onceWidth + onceWidth;
                lowNormalHigh = 1;
            }
            else if (value < start && actualStart < value)
            {
                float balance = value - actualStart;
                percentage = (balance / range) * onceWidth;
                lowNormalHigh = 0;
            }
            else if (value > end && actualEnd > value)//
            {
                float balance = value - end;
                percentage = (balance / range) * onceWidth + twiceWidth;
                lowNormalHigh = 2;
            }
            else if(value > actualEnd)
            {
                percentage = fullwidth;
                lowNormalHigh = 2;
            }

            position = Convert.ToInt32(percentage);

            return position;
        }
    }
}
