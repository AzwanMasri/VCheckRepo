using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VCheckListenerWorker.Lib.Models
{
    public class TestResultModel
    {
    }

    public class tbltestanalyze_results
    {
        [Key]
        public long ResultRowID { get; set; }
        public string? MessageType { get; set; }
        public DateTime MessageDateTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class tbltestanalyze_results_messageheader
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? FieldSeparator { get; set; }
        public string? EncodingCharacters {  get; set; }
        public string? SendingApplication {  get; set; }
        public string? SendingFacility {  get; set; }
        public string? ReceivingApplication { get; set; }
        public string? ReceivingFacility { get; set; }
        public string? DateTimeMessage {  get; set; }
        public string? MessageType { get; set; }
        public string? MessageControlID { get; set; }
        public string? ProcessingID {  get; set; }
        public string? VersionID {  get; set; }
        public string? AcceptAckmgtType { get; set; }
        public string? AppAckmgtType { get; set; }
        public string? CountryCode { get; set; }
        public string? CharacterSet { get; set; }
        public string? PrincipalLanguageMsg { get; set; }
        public string? MessageProfileIdentifier { get; set; }
    }

    public class tbltestanalyze_results_notes
    {
        [Key]
        public long RowID { get; set; } 
        public long ResultRowID { get; set; }
        public string? Segment { get; set; }
        public string SetID { get; set; }
        public string SourceComment {  get; set; }
        public string? Comment {  get; set; }
    }

    public class tbltestanalyze_results_observationrequest
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? SetID { get; set; }
        public string? PlacerOrderNumber { get; set; }
        public string? FillerOrderNumber {  get; set; }
        public string? UniversalServIdentifier {  get; set; }
        public string? Priority { get; set; }
        public string? RequestedDateTime {  get; set; }
        public string? ObservationDateTime {  get; set; }
        public string? ObservationEndDateTime { get; set; }
        public string? CollectVolume {  get; set; }
        public string? CollectorIdentifier {  get; set; }
        public string? SpecimenActionCode {  get; set; }
    }

    public class tbltestanalyze_results_observationresult
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? SetID { get; set; }
        public string? ValueType { get; set; }
        public string? ObservationIdentifier { get; set; }
        public string? ObservationSubID { get; set; }
        public string? ObservationValue { get; set; }
        public string? Units { get; set; }
        public string? ReferencesRange { get; set; }
        public string? AbnormalFlag {  get; set; }
        public string? ObservationResultStatus {  get; set; }
        public string? ObservationDateTime { get; set; }
        public string? ProducerID { get; set; }
        public string? ResponsibleObserver {  get; set; }
        public string? ObservationMethod {  get; set; }
        public string? EquipmentInstanceIdentifier { get; set; }
        public string? AnalysisDateTime { get; set; }
    }

    public class tbltestanalyze_results_patientidentification
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? SetID { get; set; }
        public string? PatientID { get; set; }
        public string? PatientIdentifierList { get; set; }
        public string? AlternatePatientID { get; set; }
        public string? PatientName { get; set; }
        public string? MotherMaidenName { get; set; }
        public string? DateofBirth { get; set; }
    }

    public class tbltestanalyze_results_patientvisit
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
	    public string? SetID  { get; set; }
        public string? PatientClass  { get; set; }
        public string? AssignedPatientLocation  { get; set; }
        public string? AdmissionType  { get; set; }
	    public string? PreadmitNumber { get; set; }
	    public string? PriorPatientLocation  { get; set; }
	    public string? AttendingDoctor  { get; set; }
	    public string? ReferringDoctor  { get; set; }
	    public string? ConsultingDoctor  { get; set; }
	    public string? HospitalService { get; set; }
	    public string? TemporaryLocation  { get; set; }
	    public string? PreadmitTestIndicator  { get; set; }
	    public string? ReAdmissionIndicator  { get; set; }
	    public string? AdmitSource  { get; set; }
	    public string? AmbulatoryStatus  { get; set; }
	    public string? VIPIndicator { get; set; }
	    public string? AdmittingDoctor  { get; set; }
	    public string? PatientType { get; set; }
	    public string? VisitNumber  { get; set; }
    }

    public class tbltestanalyze_results_specimen
    {
        [Key]
        public long RowID { get; set; }
        public long ResultRowID { get; set; }
        public string? SetID { get; set; }
	    public string? SpecimenID { get; set; }
	    public string? SpecimentParentID { get; set; }
	    public string? SpecimenType { get; set; }
	    public string? SpecimenTypeModifier { get; set; }
	    public string? SpecimenAdditives { get; set; }
	    public string? SpecimenCollectionMethod { get; set; }
	    public string? SpecimenSourceSite { get; set; }
	    public string? SpecimenSourceSiteModifier { get; set; }
	    public string? SpecimenCollectionSite { get; set; }
	    public string? SpecimenRole { get; set; }
    }

    public class tbltestanalyze_results_specimencontainer
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
	    public string? AvailableSpecimenVolume{ get; set; }
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

    public class txn_testresults
    {
        [Key]
        public long ID { get; set; }
        public DateTime? TestResultDateTime { get; set; }
        public string? TestResultType { get; set; }
        public string? OperatorID { get; set; }
        public string? PatientID { get; set; }
        public string? PatientName { get; set; } //added to include patient name in report (azwan - 20250214)
        public string? InchargePerson { get; set; }
        public string? OverallStatus { get; set; }
        //public string? ObservationStatus { get; set; }
        //public string? TestResultStatus { get; set; }
        //public Decimal? TestResultValue { get; set; }
        //public string? TestResultRules { get; set; }
        public string? Species { get; set; }
        public string? Age { get; set; }
        public string? Weight { get; set; }
        public string? DeviceSerialNo { get; set; }
        public int IsDeleted { get; set; }
        public long Analyze_TableRowID { get; set; }
        public string PMSFunction { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class txn_testresults_details
    {
        [Key]
        public long ID { get; set; }
        public long TestResultRowID { get; set; }
        public string? TestParameter {  get; set; }
        public string? SubID { get; set; }
        public string? ProceduralControl {  get; set; }
        public string? TestResultStatus { get; set; }
        public string? TestResultValue { get; set; }
        public string? TestResultUnit { get; set; }
        public string? ReferenceRange {  get; set; }
        public string? MeasuringRange { get; set; }
        public string? Interpretation { get; set; }
    }

    public class txn_testresults_graphs
    {
        [Key]
        public long ID { get; set; }
        public long TestResultRowID { get; set; }
        public string? FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

    }

    public class txn_testresults_graphsExtended
    {
        public string? FileName { get; set; }
        public string Base64String { get; set; }
    }

    public class txn_notification
    {
        [Key]
        public int NotificationID { get; set; }
        public string NotificationType { get; set; }
        public string NotificationTitle { get; set; }
        public string NotificationContent { get; set; }
        public string CreatedDate { get; set; }
        public string CreatedBy { get; set;}
    }

    public class mst_template
    {
        [Key]
        public int TemplateID { get; set; }
        public string? TemplateType { get; set; }
        public string? TemplateCode { get; set; }
        public string? TemplateTitle { get; set; }
        public string? TemplateContent { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class mst_template_details
    {
        [Key]
        public int ID { get; set; }
        public int TemplateID { get; set; }
        public string? LangCode {  get; set; }
        public string? TemplateTitle { get; set; }
        public string? TemplateContent {  get;  set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class NotificationTemplateLang
    {
        public int TemplateID { get; set; }
        public string? TemplateType { get; set; }
        public string? TemplateCode { get; set; }
        public string? TemplateTitle { get; set; }
        public string? TemplateContent { get; set; }
        public string? TemplateLangCode { get; set; }
    }

    public class mst_configuration
    {
        [Key]
        public int ConfigurationID { get; set; }
        public string? ConfigurationKey { get; set; }
        public string? ConfigurationValue { get; set; }
    }

    public class ScheduledTestModel
    {
        [Key]
        public long ID { get; set; }
        public string? ScheduledTestType { get; set; }
        public DateTime? ScheduledDateTime { get; set; }
        public string? ScheduledBy { get; set; }
        public string? PatientID { get; set; }
        public string? PatientName { get; set; }
        public string? Gender { get; set; }
        public string? Species { get; set; }
        public string? OwnerName { get; set; }
        public string? InchargePerson { get; set; }
        public DateTime? PatientDOB { get; set; }
        public int? ScheduleTestStatus { get; set; }
        public int? TestCompleted { get; set; }
        public string? ScheduleUniqueID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public string? LocationID { get; set; }
    }

    public class DeviceModel
    {
        [Key]
        public int id { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceSerialNo { get; set; }
        public string? Description { get; set; }
        public string? DeviceIPAddress { get; set; }
        public string? DeviceImagePath { get; set; }
        public int? status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public int? DeviceTypeID { get; set; }
    }

    public class DeviceTypeModel
    {
        [Key]
        public int id { get; set; }
        public string? TypeName { get; set; }
        public string? ParameterType { get; set; }
        public int? Status { get; set; }
        public int? SeqNo { get; set; }
        public int? TwoWayCommunication { get; set; }
        public int? PosNegRequired { get; set; }
        public string? ImageSource { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class TestResultReferenceRangeModel
    {
        [Key]
        public long ID { get; set; }
        public string? Parameter { get; set; }
        public string? Analyzer { get; set; }
        public string? Species { get; set; }
        public string? AgeGroup { get; set; }
        public string? NormalGrayZoneAbnormal { get; set; }
        public string? LowNormalHigh { get; set; }
        public string? MeasuringRange { get; set; }
    }
}
