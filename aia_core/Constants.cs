namespace aia_core
{
    public struct DefaultConstants
    {
        public const string BasicAuthentication = "BasicAuthentication";
        public const string CustomBasicAuthentication = "CustomBasicAuthentication";
        public const string AccessTokenBearer = "AccessTokenBearer";
        public const string OtpTokenBearer = "OtpTokenBearer";

        public const int IndividualPolicyNoLength = 10;
        public const int LimitDaysForUpcomingAndOverdue = 30;
        public const int LimitDaysForUpcomingAndOverdueForULI = 45;

        public const string AiaApiDateFormat = "yyyy-MM-dd";
        public const string AiaILApiSuccessCode = "success";

        public const int ClaimContactHours = 72;

        public const string DocTypeIdForEmailPdf = "CLMF001";
        public const string NoBenefitForm = "NoBenefitForm";

        public const string IdTypeNrc = "N";
        public const string IdTypePassport = "X";
        public const string IdTypeOther = "O";

        public const string FollowupDocTypeId = "CLMDOC1";

        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DateFormat = "yyyy-MM-dd";

        public const string CLAIM_MEDICAL_BILL_DOCTYPEID = "CLMR001";
        public const string CLAIM_MEDICAL_RECORD_DOCTYPEID = "CLMDOC1";


        public const int OTP_LIMIT_COUNT_PER_USER = 5;
        public const int OTP_LIMIT_INTERVAL_PER_USER_IN_MINUTES = 60;
        public const int OTP_LIMIT_COUNT_PER_IP = 10;
        public const int OTP_LIMIT_INTERVAL_PER_IP_IN_MINUTES = 10;
        public const int OTP_LIMIT_COUNT_GLOBAL = 1000;
        public const int OTP_LIMIT_INTERVAL_PER_GLOBAL_IN_MINUTES = 1;
    }

    public struct CMSClaim
    {
        public const string ID = "ID";
        public const string Email = "Email";
        public const string Name = "Name";
        public const string RoleID = "RoleID";
        public const string GenerateToken = "GenerateToken";
        public const string JTI = "JTI";
    }

    public struct ClaimBenefitCode
    {
        public const string DentalCare = "DTL";
        public const string PhysicalCheckup = "PHY";
        public const string MaternityCare = "PPNAT";
        public const string VisionCare = "VSI";
        public const string Vaccination = "VCN";


        //DentalCare,
        //CriticalIllnessBenefit,
        //AccidentalDeath,
        //Death,
        //VisionCare,
        //Vaccination,
        //Inpatient,
        //MaternityCare,
        //PhysicalCheckup,
        //AmbulatoryCare,
        //OutpatientCare,
        //AcceleratedCancerBenefit,
        //TotalPermanentDisability,
        //PartialDisabilityAndInjury,
        //SurgeryOrMiscarriage,
    }

    public struct ClaimDignosisCode
    {
        public const string MaternityCare = "0057";
        public const string DentalCare = "0039";
        public const string PhysicalCheckup = "0034";        
        public const string VisionCare = "0085";
        public const string Vaccination = "0086";


        //DentalCare,
        //CriticalIllnessBenefit,
        //AccidentalDeath,
        //Death,
        //VisionCare,
        //Vaccination,
        //Inpatient,
        //MaternityCare,
        //PhysicalCheckup,
        //AmbulatoryCare,
        //OutpatientCare,
        //AcceleratedCancerBenefit,
        //TotalPermanentDisability,
        //PartialDisabilityAndInjury,
        //SurgeryOrMiscarriage,
    }

    public struct ClaimApiClaimCode
    {
        public const string MaternityCare = "MT";
        public const string DentalCare = "DT";
        public const string PhysicalCheckup = "PC";
        public const string VisionCare = "VS";
        public const string Vaccination = "VC";


        //DentalCare,
        //CriticalIllnessBenefit,
        //AccidentalDeath,
        //Death,
        //VisionCare,
        //Vaccination,
        //Inpatient,
        //MaternityCare,
        //PhysicalCheckup,
        //AmbulatoryCare,
        //OutpatientCare,
        //AcceleratedCancerBenefit,
        //TotalPermanentDisability,
        //PartialDisabilityAndInjury,
        //SurgeryOrMiscarriage,
    }
}
