using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckListenerWorker_Others.Lib.Models
{
    public class PMSModel
    {
    }

    public class UpdateResultRequest
    {
        public String? accessionnumber { get; set; }
        public String? clinic_id { get; set; }
        public String? reportdate { get; set; }
        public String? providerid { get; set; }
        public UpdateResultPatientObject patient { get; set; }
        public List<UpdateResultPanelsObject> panels { get; set; }
    }

    public class UpdateResultPatientObject
    {
        public String? patientid { get; set; }
        public String? firstname { get; set; }
        public String? lastname { get; set; }
        public String? gender { get; set; }
        public String? birthday { get; set; }
        public String? species { get; set; }
        public String? breed { get; set; }
    }

    public class UpdateResultPanelsObject
    {
        public String? name { get; set; }
        public String? code { get; set; }
        public String? status { get; set; }
        public String? source { get; set; }
        public String? resultdate { get; set; }
        public List<UpdateResultPanelTestObject> tests { get; set; }
        public String? notes { get; set; }
    }

    public class UpdateResultPanelTestObject
    {
        public String? name { get; set; }
        public String? code { get; set; }
        public String? result { get; set; }
        public String? referencelow { get; set; }
        public String? referencehigh { get; set; }
        public String? unitofmeasure { get; set; }
        public String? status { get; set; }
        public String? notes { get; set; }
    }
}
