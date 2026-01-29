using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace aia_core
{
    public enum ErrorCode
    {
        E0,
        E101,
        E201,
        E202,
        E203,
        E204,
        E205,
        E206,
        E207,
        E208,
        E400,
        E4000,
        E4001,
        E4002,
        E4003,
        E4004,
        E401,
        E402, // Invalid email or password
        E403,
        E404,
        E405,// Invalid bank account number
        E500,
        E700, // No client found
        E701, // No ploicy found
        E501, // Email not found in staff
        E502, // Invalid signature
        E702,
    };

    public enum EnumMemberType
    {
        individual,
        ruby,
        rubyelite,
        corporate
    }

    public enum EnumCategoryType
    {
        activity,
        promotion
    }

    public enum EnumPropositionBenefit
    {
        both,
        member,
        ruby
    }

    public enum EnumBlogStatus
    {
        pending,
        started,
        expired
    }

    public enum EnumIdenType
    {
        Nrc,
        Passport,
        Others
    }

    public enum EnumIndividualMemberType
    {
        Ruby,
        Member,
        All
    }

    public enum EnumPolicyStatus
    {
        IF,
        IN,
        LA,
        PS
    }

    public enum EnumPremiumStatus
    {
        PU
    }

    public enum EnumPropositionType
    {
        partner,
        aia
    }

    public enum EnumHotlineType
    {
        call_sher,
        custom_number
	}

    public enum EnumPolicyPersonType
    {
        PolicyHolder,
        Insured,
        Beneficiary,
        Payer
    }

    public enum EnumStatusType
    {
        Policy,
        Premium,
    }

    public enum EnumRoleModule
    {
        Settings = 0,
        Member_Policy_Info = 1,
        Member_Proposition = 2,
        Claim = 3,
        Claim_Log = 4,
        Service = 5,
        Service_Log = 6,
        Activity_And_Promotions = 7,
        Products = 8,
        Dashboard = 9,
        SystemConfig = 99,
        HelpAndSupport = 10,
    }

    public enum EnumDeviceType
    { 
        Android,
        iOS,
    }

    public enum EnumAuthorizationType
    {
        Person,
        Product,
        Status
    }

    public enum EnumPushMessageCode
    {
        // for claim
        ClaimPushMsg_RC //Received
        , ClaimPushMsg_AL //Approved
            , ClaimPushMsg_FU //Followup
            , ClaimPushMsg_PD //Paid
             , ClaimPushMsg_CS // Close
            , ClaimPushMsg_WD // Withdrawn
            , ClaimPushMsg_RJ // Rejected
             //, ClaimPushMsg_BT //Approved
        // for servce request


        // for system and others
        , Product
        , Promotion
        , Proposition
        , Activity
        , UpcomingPremiums
    }

    public enum EnumClaimStatus
    {
        AL, //	Approved
        BT, //	Settled
        CE, //	CofE Sent
        CL, //	Cancelled
        CS, //	Closed
        DC, //	Declined
        DF, //	Deferred
        DT, //	DeathClaim
        EX, //	Ex-gratia
        FU, //	Followed-up
        IP, //	In Payment
        IR, //	In Review
        MT, //	Terminated
        PA, //	Amended
        PN, //	Pending
        PR, //	Recommend
        PW, //	Pend Wdl
        RJ, //	Rejected
        WD,	//	Withdrawn
        PD, // Paid
        RC, // Received
    }


    public enum EnumNotificationType
    {
        All,
        Claim,
        Service,
        Others,
    }

    public enum EnumHomeRecentType
    {
        Claim,
        Service,
    }

    public enum EnumServiceStatus
    {
        All,
        Received,
        Approved,
        NotApproved,
        Paid,
        Pending,
    }


    public enum EnumFileType
    {
        Profile,
        Blog,
        Bank,
        Coverage,
        Product,
        Proposition,
        PropositionCategory,

    }

    public enum EnumSystemNotiType
    { 
        Alert,
        PasswordChange,
        Product,
        Promotion,
        Proposition,
        UpcomingPremiums,
        Announcement,
        Payment,
    }

    public enum EnumClaimStatusDesc
    {
        All,
        Approved,
        Received,
        [Description("Followed-up")]
        Followedup,
        Withdrawn,
        Closed,
        Paid,
        Rejected,
    }

    public enum EnumGender
    {
        Male, Female
    }

    public enum EnumObjectGroup
    {

        Others, Coverages, Products, Bank, Blogs, Staffs, Roles, Members, MemberPolicy, Propositions, PropositionsRequest, PropositionCategory, Localizations, 
        Hospital, ClaimIncurredLocation, Diagnosis, PartialDisability, PermanentDisability, CriticalIllness, Death, Holiday,
        AppConfig, AppVersion, Auth, Claim, Servicing, PaymentChangeConfig, DocConfig, Notification,Faq,

    }

    public enum EnumObjectAction
    {
        View, Create, Update, UpdateFeature, UpdateSort, Delete, ChangeStatus, Import, Export, Download, ADLogin, PasswordLogin, ToggleStatus, List
    }

    public enum EnumOtpClaims
    {
        /// <summary>
        /// member id
        /// </summary>
        mid,
        /// <summary>
        /// okta user id
        /// </summary>
        oktaid,
        /// <summary>
        /// register, resetpassword, changeemail, changephone
        /// </summary>
        type,
        /// <summary>
        /// value of email or phone
        /// </summary>
        target
    }

    public enum EnumOtpType
    {
        register, resetpassword, changeemail, changephone
            , claim
    }

    public enum EnumBenefitFormType
    {
        DentalCare,
        CriticalIllnessBenefit,
        DeathAndAccidentalDeath,
        VisionCare,
        Vaccination,
        Inpatient,
        MaternityCare,
        PhysicalCheckup,
        OutpatientAndAmbulatoryCare,
        AcceleratedCancerBenefit,
        TotalPermanentDisability,
        PartialDisabilityAndInjury,
    }

    public enum EnumClaimType
    {
        DentalCare,
        CriticalIllnessBenefit,
        AccidentalDeath,
        Death,
        VisionCare,
        Vaccination,
        Inpatient,
        MaternityCare,
        PhysicalCheckup,
        AmbulatoryCare,
        OutpatientCare,
        AcceleratedCancerBenefit,
        TotalPermanentDisability,
        PartialDisabilityAndInjury,
        SurgeryOrMiscarriage,
    }

    public enum EnumClaimFormProductType
    {
       MER,
       OHI,
       OHG,

       MER_OHI_OHG,

       MER_OHG,

       MER_OHI,

       OHI_OHG,
    }

    public enum EnumBankDigitType
    {
        Range,
        OR,
        Custom
    }

    public enum EnumBankAccountType
    {
        Both,
        OnlySaving,
        OnlySpecial
    }

    public enum EnumClaimSetting
    {
        Hospital, ClaimIncurredLocation, Diagnosis, PartialDisability, PermanentDisability, CriticalIllness, Death, Relationship
    }

    public enum EnumCauseByType
    {
        Death, CriticalIllness, PermanentDisability, PartialDisability
    }

    public enum EnumILClaimApi
    {
        Health,
        NonHealth,
        Death,
        CI,
        TPD
    };

    public enum EnumClaimDoc
    {
        Death_Certificate, Medical_Record, Medical_Bill, Injury_Photo
    }

    public enum EnumILCustomDate
    {
        Claim,
        Servicing,
    }

    public enum EnumServicingType
    {
        PolicyHolderInformation,
        InsuredPersonInformation
    }

    public enum EnumCommonServicingType
    {
        LapseReinstatement,
        HealthRenewal,
        ACPLoanRepayment,
        AdhocTopup,
        PolicyLoanRepayment,
        SumAssuredChange

    }

    public enum EnumCommonWithdrawServicingType
    {
        PartialWithdraw,
        PolicyLoan,
        PolicySurrender,
        PolicyPaidup,
        RefundOfPayment

    }

    public enum EnumServicingStatus
    {
        Received,
        Rejected, //Don't, Use! in Servicing..
        Approved,
        Paid,
        NotApproved,
    }

    public enum EnumMainServiceType
    {
        MemberInformationChange,
        Reinstatement_Renewal,
        RepaymentAdHocTopup,
        OtherRequest
    }

    public enum EnumServiceType
    {
        PolicyHolderInformation,
        InsuredPersonInformation,
        BeneficiaryInformation,

        LapseReinstatement,
        HealthRenewal,

        PolicyLoanRepayment,
        AcpLoanRepayment,
        AdHocTopup,

        PartialWithdraw,
        PolicyLoan,
        PolicyPaidUp,
        PolicySurrender,
        PaymentFrequency,
        SumAssuredChange,
        RefundOfPayment,

    }

    public enum EnumMarriedStatus
    {
        Single,
        Married,
        Divorced,
    }


    public enum EnumPaymentFrequency
    {
        Monthly,
        Quarterly,
        SemiAnnually,
        Annually
    }



    public enum EnumProgressType
    {
        Claim,
        Service,
    }

    public enum EnumQueryStringType
    {
        List,
        Export
    }

    public enum EnumBeneficiaryShareInfoType
    {
        New,
        Update,
        Remove
    }

    public enum EnumPaymentChangeConfigValidationType
    {
        Rule,
        Alert
    }

    public enum EnumChartType
    {
        ClaimType,
        ClaimProductType,
        ClaimPerformance,
        ClaimStatus,
        ClaimFailLog,
        ServiceType,
        ServicePerformance,
        ServiceStatus,
        ServiceFailLog
    }

    public enum EnumDocShowingFor
    {
        All,
        Latest
    }

    public enum EnumILStatus
    {
        Success,
        Failed,
    }

    public enum EnumCashlessClaimProcedureInfo
    {
        LOCAL,
        OVERSEAS,
    }

    public enum  EnumAuthPersonType
    {
        PolicyHolder,
        Insured,
        Beneficiary,
        Payer,
    }

    public enum RateLimitOtpType
    {
        ForgotPassword,
        ProfileOtpRequest,
        OtpRequest,
        ClaimOtpRequest,
        ServiceOtpRequest,
        OtpVerify,
        GetOktaUserName,
    }

    // Test
}