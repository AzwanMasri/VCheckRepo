using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VCheck.Lib.Data.Models
{
    public class TestResultModel
    {
        [Key]
        public long ID { get; set; }
        public DateTime? TestResultDateTime { get; set; }
        public String? TestResultType { get; set; }
        public String? OperatorID { get; set; }
        public String? DeviceSerialNo { get; set; }
        public String? PatientID { get; set; }
        public String? PatientName { get; set; } //added to include patient name in report (azwan - 20250214)
        public String? InchargePerson { get; set; }
        //public String? ObservationStatus { get; set; }
        public String? Species { get; set; }
        public String? Age { get; set; }
        public String? Weight { get; set; }
        public String? OverallStatus { get; set; }
        //public String? TestResultStatus { get; set; }
        //public Decimal? TestResultValue { get; set;  }
        //public String? TestResultValue { get; set; }
        //public String? TestResultRules { get; set; }
        public string PMSFunction { get; set; }
        public int IsDeleted { get; set; }
        public long Analyze_TableRowID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public String? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public String? UpdatedBy { get; set; }
    }

    public class TestResultNotes
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? Segment { get; set; }
        public string? SetID { get; set; }
        public string? SourceComment { get; set; }
        public string? Comment { get; set; }
    }

    public class TestResultSpecimenContainer
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? ExternalAccessionIdentifier { get; set; }
        public string? AccessionIdentifier { get; set; }
        public string? ContainerIdentifier { get; set; }
        public string? PrimaryContainerIdentifier { get; set; }
        public string? EquipmentContainerIdentifier { get; set; }
        public string? SpecimenSource { get; set; }
        public string? RegistrationDateTime { get; set; }
        public string? ContainerStatus { get; set; }
        public string? CarrierType { get; set; }
        public string? CarrierIdentifier { get; set; }
        public string? PositionInCarrier { get; set; }
        public string? TrayTypeSAC { get; set; }
        public string? TrayIdentifier { get; set; }
        public string? PositionInTray { get; set; }
        public string? Location { get; set; }
        public string? ContainerHeight { get; set; }
        public string? ContainerDiameter { get; set; }
        public string? BarrierDelta { get; set; }
        public string? BottomDelta { get; set; }
        public string? ContainerHeightDiamtrUnits { get; set; }
        public string? ContainerVolume { get; set; }
        public string? AvailableSpecimenVolume { get; set; }
        public string? volumeUnits { get; set; }
        public string? SeparatorType { get; set; }
        public string? CapType { get; set; }
        public string? Additive { get; set; }
        public string? SpecimenComponent { get; set; }
        public string? DilutionFactor { get; set; }
        public string? Treatment { get; set; }
        public string? Temperature { get; set; }
        public string? HemolysisIndex { get; set; }
        public string? HemolysisIndexUnits { get; set; }
        public string? LipemiaIndex { get; set; }
        public string? LipemiaIndexUnits { get; set; }
        public string? IcterusIndex { get; set; }
        public string? IcterusIndexUnits { get; set; }
    }

    public class TestResultDetailsModel
    {
        [Key]
        public long ID { get; set; }
        public long TestResultRowID { get; set; }
        public String? TestParameter { get; set; }
        public String? SubID { get; set; }
        public String? ProceduralControl {  get; set; }
        public String? TestResultStatus { get; set; }
        public String? TestResultValue { get; set; }
        public String? TestResultUnit { get; set; }
        public String? ReferenceRange { get; set; }
        public String? MeasuringRange { get; set; }
        public String? Interpretation { get; set; }

    }

    public class TestResultGraphModel
    {
        [Key]
        public long ID { get; set; }
        public long TestResultRowID { get; set; }
        //public String? FolderName { get; set; }
        public String? FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public String CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public String? UpdatedBy { get; set; }

    }

    public class TestResultOutputFileModel : TestResultModel
    {
        public String? TestParameter { get; set; }
        public String? TestResultStatus { get; set; }
        //public String? TestResultValue { get; set; }
        public String? TestResultUnit { get; set; }
        public String? ReferenceRange { get; set; }
    }

    public class TestResultExtendedModel
    {
        [Key]
        public long ID { get; set; }
        public DateTime? TestResultDateTime { get; set; }
        public String? TestResultType { get; set; }
        public String? OperatorID { get; set; }
        public String? DeviceSerialNo { get; set; }
        public String? PatientID { get; set; }
        public String? PatientName { get; set; }
        public String? InchargePerson { get; set; }
        public String? ObservationStatus { get; set; }
        public String? TestResultStatus { get; set; }
        public String? TestResultValue { get; set; }
        public String? TestResultRules { get; set; }
        public DateTime? CreatedDate { get; set; }
        public String? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public String? UpdatedBy { get; set; }
    }

    public class TestResultAPIObject
    {
        public long ID { get; set; }
        public DateTime? TestResultDateTime { get; set; }
        public String? TestResultType { get; set; }
        public String? OperatorID { get; set; }
        public String? DeviceSerialNo { get; set; }
        public String? PatientID { get; set; }
        public String? InchargePerson { get; set; }
        public String? OverallStatus { get; set; }
        public String? TestResultParameter { get; set; }
        public String? ProceduralControl { get; set; }
        public String? TestResultStatus { get; set; }
        public String? TestResultValue { get; set; }
        public String? TestResultUnit { get; set; }
        public String? ReferenceRange { get; set; }
    }

    public class TestResultListingObj : TestResultModel 
    { 
        public long RowNo { get; set; }
        public String? TestResultDateTimeString { get; set; }
        public String? TestResultValueString { get; set; }
        public String? statusBackground { get; set; }
        public String? statusFontColor { get; set; }
        public String? printedBy { get; set;  }
        public DateTime? printedOn { get; set; }
        public Boolean isPrint { get; set; }
    }

    public class TestResultListingExtendedObj : TestResultExtendedModel
    {
        public long RowNo { get; set; }
        public String? TestResultDateTimeString { get; set; }
        public String? TestResultValueString { get; set; }
        public String? statusBackground { get; set; }
        public String? statusFontColor { get; set; }
        public String? printedBy { get; set; }
        public DateTime? printedOn { get; set; }
        public Boolean isPrint { get; set; }
        public string PMSFunction { get; set; }
    }

    public class PatientDataObject
    {
        public String? patientid { get; set; }
        public String? observationdatetime { get; set; }
        public String? observationtype { get; set; }
        public String? observationvalue { get; set; }
        public String? observationresult { get; set; }
        public String? observationrules { get; set; }
        public String? inchargeperson { get; set; }
        public String? observationby { get; set; }
    }

    public class TestListModel
    {
        [Key]
        public String? TestID { get; set; }
        public String? TestName { get; set; }
        //public String? Parameter { get; set; }
        public String? TestDescription { get; set; }
        public String? Analyzer { get; set; }
        public String? Species { get; set; }
        public String? Gender { get; set; }
        public DateTime? CreatedDate { get; set; }
        public String? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public String? UpdatedBy { get; set; }
    }

    public class ParametersModel
    {
        [Key]
        public int ID { get; set; }
        public String? Analyzer { get; set; }
        public String? Category { get; set; }
        public String? Parameter { get; set; }
        public int Order { get; set; }
    }

    public class DownloadPrintResultModel
    {
        public TestResultModel TestResult { get; set; }
        public List<TestResultDetailsModel> TestResultDetails { get; set; }
        public TestResultModel PreviousTestResult { get; set; }
        public List<TestResultDetailsModel> PreviousTestResultDetails { get; set; }
        public List<TestResultGraphModel> TestResultsGraph { get; set; }
    }

    public class TestDeviceName
    {
        public long TestID { get; set; }
        public string DeviceName { get; set; }
        public string TestResultType { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
