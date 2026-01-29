using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.ClaimProcess
{
    public class ClaimProcessResponseModel
    {
        public Results results { get; set; }

    }

    public class Results
    {
        public string admissionDate { get; set; }
        public List<string> billType { get; set; }
        public string billingDate { get; set; }
        public string claimId { get; set; }
        public string dischargeDate { get; set; }
        public string doctorName { get; set; }
        public string hospitalName { get; set; }
        public string netAmount { get; set; }
        public string patientName { get; set; }
    }

    public class ClaimResultType1
    {
        public string admissionDate { get; set; }
        public List<string> billType { get; set; }
        public string billingDate { get; set; }
        public string claimId { get; set; }
        public string dischargeDate { get; set; }
        public string doctorName { get; set; }
        public string hospitalName { get; set; }
        public string netAmount { get; set; }
        public string patientName { get; set; }
    }

    public class ClaimResultType2
    {
        public string admissionDiagnosis { get; set; }
        public string age { get; set; }
        public string chiefComplaint { get; set; }
        public string claimId { get; set; }
        [JsonProperty("co-morbidity")]
        public string coMorbidity { get; set; }
        public string dateofAdmission { get; set; }
        public string dateofDischarge { get; set; }
        public string dischargeDiagnosis { get; set; }
        public string doctorName { get; set; }
        public string hospitalName { get; set; }
        public string pastMedicalHistory { get; set; }
        public string pastSurgicalHistory { get; set; }
        public string patientName { get; set; }
        public string registerationNo { get; set; }
        public string sex { get; set; }
    }

}
