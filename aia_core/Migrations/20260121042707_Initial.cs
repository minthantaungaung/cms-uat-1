using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aia_core.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    ApiKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApiSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.ApiKey);
                });

            migrationBuilder.CreateTable(
                name: "App_Configs",
                columns: table => new
                {
                    ID = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    Sher_Contact_Number = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Aia_Customer_Care_Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Aia_Myanmar_Website = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Aia_Myanmar_FacebookUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Aia_Myanmar_InstagramUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Aia_Myanmar_Addresses = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Claim_TAT_Hours = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Servicing_TAT_Hours = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Claim_Archive_Frequency = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Servicing_Archive_Frequency = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ImagingTotalFileSizeLimit = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Imaging_Individual_File_Size_Limit = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ClaimEmail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ServicingEmail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Maintenance_On = table.Column<bool>(type: "bit", nullable: false),
                    Maintenance_Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Maintenance_Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Vitamin_Supply_Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Doc_Upload_Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bank_Info_Upload_Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Coast_Claim_IsSystemDate = table.Column<bool>(type: "bit", nullable: true),
                    Coast_Claim_CustomDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Coast_Servicing_IsSystemDate = table.Column<bool>(type: "bit", nullable: true),
                    Coast_Servicing_CustomDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Proposition_Request_Receiver = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConfigs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "App_Versions",
                columns: table => new
                {
                    ID = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    Minimum_Android_Version = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Latest_Android_Version = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Minimum_Ios_Version = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Latest_Ios_Version = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_App_Versions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectGroup = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ObjectID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObjectName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Authorization_Person",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Registration = table.Column<bool>(type: "bit", nullable: false),
                    Login = table.Column<bool>(type: "bit", nullable: false),
                    ViewMyPolicies = table.Column<bool>(type: "bit", nullable: false),
                    Proposition = table.Column<bool>(type: "bit", nullable: false),
                    Claim = table.Column<bool>(type: "bit", nullable: false),
                    PolicyHolderDetails = table.Column<bool>(type: "bit", nullable: false),
                    InsuredDetails = table.Column<bool>(type: "bit", nullable: false),
                    BeneficiaryInfo = table.Column<bool>(type: "bit", nullable: false),
                    PaymentFrequency = table.Column<bool>(type: "bit", nullable: false),
                    ACP = table.Column<bool>(type: "bit", nullable: false),
                    LoanRepayment = table.Column<bool>(type: "bit", nullable: false),
                    AdhocTopup = table.Column<bool>(type: "bit", nullable: false),
                    HealthRenewal = table.Column<bool>(type: "bit", nullable: false),
                    LapseReinstatement = table.Column<bool>(type: "bit", nullable: false),
                    PartialWithdrawal = table.Column<bool>(type: "bit", nullable: false),
                    PolicyLoan = table.Column<bool>(type: "bit", nullable: false),
                    PolicyPaidup = table.Column<bool>(type: "bit", nullable: false),
                    PolicySurrender = table.Column<bool>(type: "bit", nullable: false),
                    RefundofPayment = table.Column<bool>(type: "bit", nullable: false),
                    SumAssuredChange = table.Column<bool>(type: "bit", nullable: false),
                    PolicyLoanRepayment = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorization_Person", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Authorization_Product",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Registration = table.Column<bool>(type: "bit", nullable: false),
                    Login = table.Column<bool>(type: "bit", nullable: false),
                    ViewMyPolicies = table.Column<bool>(type: "bit", nullable: false),
                    Proposition = table.Column<bool>(type: "bit", nullable: false),
                    Claim = table.Column<bool>(type: "bit", nullable: false),
                    PolicyHolderDetails = table.Column<bool>(type: "bit", nullable: false),
                    InsuredDetails = table.Column<bool>(type: "bit", nullable: false),
                    BeneficiaryInfo = table.Column<bool>(type: "bit", nullable: false),
                    PaymentFrequency = table.Column<bool>(type: "bit", nullable: false),
                    ACP = table.Column<bool>(type: "bit", nullable: false),
                    LoanRepayment = table.Column<bool>(type: "bit", nullable: false),
                    AdhocTopup = table.Column<bool>(type: "bit", nullable: false),
                    HealthRenewal = table.Column<bool>(type: "bit", nullable: false),
                    LapseReinstatement = table.Column<bool>(type: "bit", nullable: false),
                    PartialWithdrawal = table.Column<bool>(type: "bit", nullable: false),
                    PolicyLoan = table.Column<bool>(type: "bit", nullable: false),
                    PolicyPaidup = table.Column<bool>(type: "bit", nullable: false),
                    PolicySurrender = table.Column<bool>(type: "bit", nullable: false),
                    RefundofPayment = table.Column<bool>(type: "bit", nullable: false),
                    SumAssuredChange = table.Column<bool>(type: "bit", nullable: false),
                    PolicyLoanRepayment = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorization_Product", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Authorization_Status",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StatusType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Registration = table.Column<bool>(type: "bit", nullable: false),
                    Login = table.Column<bool>(type: "bit", nullable: false),
                    ViewMyPolicies = table.Column<bool>(type: "bit", nullable: false),
                    Proposition = table.Column<bool>(type: "bit", nullable: false),
                    Claim = table.Column<bool>(type: "bit", nullable: false),
                    PolicyHolderDetails = table.Column<bool>(type: "bit", nullable: false),
                    InsuredDetails = table.Column<bool>(type: "bit", nullable: false),
                    BeneficiaryInfo = table.Column<bool>(type: "bit", nullable: false),
                    PaymentFrequency = table.Column<bool>(type: "bit", nullable: false),
                    ACP = table.Column<bool>(type: "bit", nullable: false),
                    LoanRepayment = table.Column<bool>(type: "bit", nullable: false),
                    AdhocTopup = table.Column<bool>(type: "bit", nullable: false),
                    HealthRenewal = table.Column<bool>(type: "bit", nullable: false),
                    LapseReinstatement = table.Column<bool>(type: "bit", nullable: false),
                    PartialWithdrawal = table.Column<bool>(type: "bit", nullable: false),
                    PolicyLoan = table.Column<bool>(type: "bit", nullable: false),
                    PolicyPaidup = table.Column<bool>(type: "bit", nullable: false),
                    PolicySurrender = table.Column<bool>(type: "bit", nullable: false),
                    RefundofPayment = table.Column<bool>(type: "bit", nullable: false),
                    SumAssuredChange = table.Column<bool>(type: "bit", nullable: false),
                    PolicyLoanRepayment = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorization_Status", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Bank",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankName_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DigitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DigitStartRange = table.Column<int>(type: "int", nullable: true),
                    DigitEndRange = table.Column<int>(type: "int", nullable: true),
                    DigitCustom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankLogo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true),
                    IlBankCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bank", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryCheckList",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShareItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: true),
                    UpdateValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateValueType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryCheckList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Blog",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CoverImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThumbnailImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Topic_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Topic_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReadMin_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReadMin_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Body_EN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Promotion_Start = table.Column<DateTime>(type: "datetime", nullable: true),
                    Promotion_End = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsFeature = table.Column<bool>(type: "bit", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true),
                    ShareableLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BufferTxnsPayment",
                columns: table => new
                {
                    TransactionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PremiumPolicyNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsGenereatePaymentLinkSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BufferTxnsPayment", x => x.TransactionId);
                });

            migrationBuilder.CreateTable(
                name: "CashlessClaimConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalTitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalTitleMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalDescriptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalDescriptionMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalButtonTextEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalButtonTextMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalDeeplink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasTitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasTitleMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasDescriptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasDescriptionMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasButtonTextEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasButtonTextMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverseasDeeplink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashlessClaimConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CI_Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisabiltiyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CI_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Claim_Service_Otp_Setup",
                columns: table => new
                {
                    FormName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsOtpRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claim_Service_Otp_Setup", x => x.FormName);
                });

            migrationBuilder.CreateTable(
                name: "claim_status",
                columns: table => new
                {
                    short_desc = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    id = table.Column<decimal>(type: "numeric(8,0)", nullable: false),
                    long_desc = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    crm_code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__claim_st__DE13CB88EDB9A2FD", x => x.short_desc);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocType",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameMm = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameSample = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameMmSample = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocType", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocumentMapping",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocTypeIDList = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BenefitFormType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TypeNameList = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocumentMapping", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocuments_MedicalBill_ApiLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claimId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    admissionDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billingDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dischargeDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    doctorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hospitalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    netAmount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    patientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocuments_MedicalBill_ApiLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimFollowup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocName2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Table_2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimIncurredLocation",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimIncurredLocation", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ClaimSaveBank",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccHolderIdValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccHolderDob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimSaveBank", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ClaimValidateMessage",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MemberID = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MemberName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MemberPhone = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimFormType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimValidateMessage", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Client_Corporate",
                columns: table => new
                {
                    ClientNo = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    CorporateClientNo = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    Name = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    PhoneNo = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true),
                    Nrc = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    PassportNo = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    Other = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    Dob = table.Column<DateTime>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "date", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "date", nullable: true),
                    MemberType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    MemberTierType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "date", nullable: true),
                    ClientNoList = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client_Coporate", x => x.ClientNo);
                });

            migrationBuilder.CreateTable(
                name: "CmsNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SendDateAndTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    Audience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudienceCount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Township = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobId = table.Column<int>(type: "int", nullable: true),
                    SendingStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsNotification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CmsNotificationJobLocker",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsNotificationJobLocker", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CmsUserSession",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpiredOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsUserSession", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "CommonOtp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OtpExpiry = table.Column<DateTime>(type: "datetime", nullable: true),
                    OtpType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OtpTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: true),
                    UsedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonOtp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "country",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    id = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bur_description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "Coverage",
                columns: table => new
                {
                    Coverage_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Coverage_Name_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Coverage_Name_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Coverage_Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    Is_Delete = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coverage", x => x.Coverage_ID);
                });

            migrationBuilder.CreateTable(
                name: "CriticalIllness",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriticalIllness", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CrmApiLog",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmApiLog", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CrmClaimCode",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmClaimCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmSignature",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignatureValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmSignature", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Death",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Death", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DefaultCmsImage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    image_for = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultCmsImage", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Diagnosis",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnosis", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "district",
                columns: table => new
                {
                    district_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    province_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    district_eng_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    district_bur_name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_district", x => x.district_code);
                });

            migrationBuilder.CreateTable(
                name: "DocConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocType = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DocTypeId = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DocName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ShowingFor = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogCms",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndPoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogCms", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogMobile",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndPoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogMobile", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "FaqTopic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicTitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopicTitleMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopicIcon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqTopic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holiday",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holiday", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Hospital",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hospital", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "InOutPatientReasonBenefitCode",
                columns: table => new
                {
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComponentCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckBenefit = table.Column<bool>(type: "bit", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaimDocument",
                columns: table => new
                {
                    DocumentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocTypeID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaimDocument", x => x.DocumentID);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceType",
                columns: table => new
                {
                    InsuranceTypeID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceTypeEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsuranceTypeMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsuranceTypeImage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceType", x => x.InsuranceTypeID);
                });

            migrationBuilder.CreateTable(
                name: "JobID",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    PromotionID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobID", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Localization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    English = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    Burmese = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Member",
                columns: table => new
                {
                    Member_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Profile_Image = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DOB = table.Column<DateTime>(type: "datetime", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NRC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Passport = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Others = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Register_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Last_Active_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    auth0_userid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Okta_UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Is_Verified = table.Column<bool>(type: "bit", nullable: true),
                    Otp_Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Otp_Expiry = table.Column<DateTime>(type: "datetime", nullable: true),
                    Otp_Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Otp_Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Otp_To = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Is_Email_Verified = table.Column<bool>(type: "bit", nullable: true),
                    Is_Mobile_Verified = table.Column<bool>(type: "bit", nullable: true),
                    MemberType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupMemberID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndividualMemberID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Township = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllClientNoListString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductCodeList = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyStatusList = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppOS = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member", x => x.Member_ID);
                });

            migrationBuilder.CreateTable(
                name: "MemberDevice",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberID = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    DeviceType = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    PushToken = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberDevice", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MemberSession",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    auth0_userid = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberSession", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "occupation",
                columns: table => new
                {
                    code = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    id = table.Column<decimal>(type: "numeric(8,0)", nullable: false),
                    description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__occupati__357D4CF81C1883B1", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "Okta_ServiceToken",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Token_Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Expires_In = table.Column<long>(type: "bigint", nullable: true),
                    Access_Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okta_ServiceToken", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OnetimeToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Otp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnetimeToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartialDisability",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialDisability", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PartialDisabilityProduct",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisabiltiyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialDisabilityProduct", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentChangeConfig",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    DescEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((1))"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentChangeConfig", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PermanentDisability",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermanentDisability", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PlanData",
                columns: table => new
                {
                    PlanCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlanDesc = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanData", x => x.PlanCode);
                });

            migrationBuilder.CreateTable(
                name: "policy_additional_amt",
                columns: table => new
                {
                    policy_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    premium_due_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    acp_principal_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    acp_interest_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    loan_principal_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    loan_interest_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    health_renewal_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    reinstatement_premium_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    reinstatement_interest_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "policy_status",
                columns: table => new
                {
                    short_desc = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    id = table.Column<decimal>(type: "numeric(8,0)", nullable: false),
                    long_desc = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__policy_s__DE13CB882AFFF0DB", x => x.short_desc);
                });

            migrationBuilder.CreateTable(
                name: "PolicyExcludedList",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyExcludedList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "premium_status",
                columns: table => new
                {
                    short_desc = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    id = table.Column<decimal>(type: "numeric(8,0)", nullable: false),
                    long_desc = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__premium___DE13CB88C6212390", x => x.short_desc);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Product_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Product_Type_ID = table.Column<decimal>(type: "numeric(8,0)", nullable: true),
                    Product_Type_Short = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Title_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Short_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Short_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Logo_Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Cover_Image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Intro_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Intro_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tagline_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tagline_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Issued_Age_From = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Issued_Age_To = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Issued_Age_From_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Issued_Age_To_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Policy_Term_Up_To_EN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Policy_Term_Up_To_MM = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Website_Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Brochure = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Crediting_Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    Is_Delete = table.Column<bool>(type: "bit", nullable: true),
                    NotAllowedInProductList = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Product_ID);
                });

            migrationBuilder.CreateTable(
                name: "product_type",
                columns: table => new
                {
                    short_desc = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    id = table.Column<decimal>(type: "numeric(8,0)", nullable: false),
                    long_desc = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__product___DE13CB88D9FA055F", x => x.short_desc);
                });

            migrationBuilder.CreateTable(
                name: "PropositionCategory",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BackgroundImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Is_Delete = table.Column<bool>(type: "bit", nullable: true),
                    IsAiaBenefitCategory = table.Column<bool>(type: "bit", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionCategory", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "province",
                columns: table => new
                {
                    province_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    country_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    province_eng_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    province_bur_name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_province", x => x.province_code);
                });

            migrationBuilder.CreateTable(
                name: "push_notification_log",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    push_token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    device_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    device_model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notification_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    noti_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sent_on = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_sendsuccess = table.Column<bool>(type: "bit", nullable: false),
                    firebase_result = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_notification_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitControlOtpAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RateLimitOtpType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitControlOtpAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitOtpBruteForceAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RateLimitOtpType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitOtpBruteForceAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReasonCode",
                columns: table => new
                {
                    ProductCode = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    ComponentCode = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    ClaimType = table.Column<string>(type: "nchar(1000)", fixedLength: true, maxLength: 1000, nullable: true),
                    ReasonCode = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Relationship",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationship", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Permissions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Route",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permission = table.Column<string>(type: "nchar(10)", fixedLength: true, maxLength: 10, nullable: false),
                    Route = table.Column<string>(type: "nchar(1000)", fixedLength: true, maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Route", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RulesMatrix",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrequencyName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FromFrequency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Monthly = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Quarterly = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SemiAnnually = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Annually = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulesMatrix", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceACPLoanRepayment",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceACPLoanRepayment", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceACPLoanRepaymentDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceACPLoanRepaymentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceACPLoanRepaymentDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceAdhocTopup",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAdhocTopup", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceAdhocTopupDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceAdhocTopupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAdhocTopupDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBeneficiary",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MainID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBeneficiary", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBeneficiaryPersonalInfo",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceBeneficiaryID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceBeneficiaryShareID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsNewBeneficiary = table.Column<bool>(type: "bit", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldMobileNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewMobileNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdFrontImageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdBackImageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdFrontImageDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdBackImageDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Front_CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Front_CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Front_CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Front_CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Back_CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Back_CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Back_CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Back_CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBeneficiaryPersonalInfo", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBeneficiaryShareInfo",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceBeneficiaryID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldRelationShipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewRelationShipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBeneficiaryShareInfo", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceHealthRenewal",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHealthRenewal", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceHealthRenewalDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceHealthRenewalID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHealthRenewalDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLapseReinstatement",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLapseReinstatement", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLapseReinstatementDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceLapseReinstatementID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLapseReinstatementDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceMain",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MainID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupMemberID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoginMemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FERequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalRemark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPending = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentSms = table.Column<bool>(type: "bit", nullable: true),
                    SentSmsAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMain", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceMainDoc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MainId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FormId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsReqeust = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NrcDocType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMainDoc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePartialWithdraw",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePartialWithdraw", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePartialWithdrawDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePartialWithdrawID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePartialWithdrawDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePaymentFrequency",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrequencyType_Old = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrequencyType_New = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount_Old = table.Column<int>(type: "int", nullable: false),
                    Amount_New = table.Column<int>(type: "int", nullable: false),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePaymentFrequency", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePaymentFrequencyValidateMessage",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Old = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    New = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    ClientNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePaymentFrequencyValidateMessage", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyLoan",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyLoan", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyLoanDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePolicyLoanID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyLoanDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyLoanRepayment",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyLoanRepayment", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyLoanRepaymentDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePolicyLoanRepaymentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyLoanRepaymentDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyMapping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PremiumStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyMapping", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyPaidUp",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyPaidUp", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicyPaidUpDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePolicyPaidUpID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicyPaidUpDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicySurrender",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicySurrender", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePolicySurrenderDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePolicySurrenderID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePolicySurrenderDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRefundOfPayment",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRefundOfPayment", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRefundOfPaymentDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceRefundOfPaymentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRefundOfPaymentDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStatusUpdate",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceMainID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStatusUpdate", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSumAssuredChange",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSumAssuredChange", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSumAssuredChangeDoc",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceSumAssuredChangeID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSumAssuredChangeDoc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceType",
                columns: table => new
                {
                    ServiceTypeID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceTypeEnum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceTypeNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceTypeNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainServiceTypeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ServiceT__8ADFAA0C19EB440D", x => x.ServiceTypeID);
                });

            migrationBuilder.CreateTable(
                name: "servicing_status",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    short_desc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    long_desc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    crm_code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicing_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ServicingRequest",
                columns: table => new
                {
                    ServicingID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaritalStatus_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaritalStatus_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FatherName_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FatherName_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Distinct_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Distinct_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Township_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Township_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Building_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Building_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street_Old = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street_New = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CMS_RequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CMS_ResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MainID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdateChannel = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicingRequest", x => x.ServicingID);
                });

            migrationBuilder.CreateTable(
                name: "TestUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "township",
                columns: table => new
                {
                    township_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    district_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    township_eng_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    township_bur_name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_township", x => x.township_code);
                });

            migrationBuilder.CreateTable(
                name: "users_temp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nrc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    passport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    others = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_no = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    okta_user_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    okta_user_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    registration_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    migration_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    migration_failed_reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_created_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    record_updated_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_done = table.Column<bool>(type: "bit", nullable: true),
                    migrate_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    migrate_log = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    migrate_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    okta_register_request = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_temp", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "MemberBank",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberBank", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MemberBank_Bank_BankID",
                        column: x => x.BankID,
                        principalTable: "Bank",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionProduct",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlogID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionProduct", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PromotionProduct_Blog",
                        column: x => x.BlogID,
                        principalTable: "Blog",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TxnsPayment",
                columns: table => new
                {
                    TransactionID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TxnsDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentChannel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TxnsPayment", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_TxnsPayment_BufferTxnsPayment_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "BufferTxnsPayment",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaqQuestion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuestionMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswerEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswerMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true),
                    FaqTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqQuestion_FaqTopic_FaqTopicId",
                        column: x => x.FaqTopicId,
                        principalTable: "FaqTopic",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InsuranceBenefit",
                columns: table => new
                {
                    ClaimID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BenefitNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsuranceTypeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BenefitFormType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceBenefit", x => x.ClaimID);
                    table.ForeignKey(
                        name: "FK_InsuranceBenefit_InsuranceType",
                        column: x => x.InsuranceTypeID,
                        principalTable: "InsuranceType",
                        principalColumn: "InsuranceTypeID");
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    client_no = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    master_client_no = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    nrc = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    passport_no = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    other = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: true),
                    gender = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    dob = table.Column<DateTime>(type: "date", nullable: false),
                    phone_no = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true),
                    email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    marital_status = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    father_name = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: true),
                    occupation = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    address1 = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    address2 = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    address3 = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    address4 = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    address5 = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Address6 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vip_flag = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    vip_effective_date = table.Column<DateTime>(type: "date", nullable: true),
                    vip_expiry_date = table.Column<DateTime>(type: "date", nullable: true),
                    agent_flag = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    agent_code = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    created_date = table.Column<DateTime>(type: "date", nullable: true),
                    updated_date = table.Column<DateTime>(type: "date", nullable: true),
                    client_certificate = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__clients__BF218C7F3F810D2C", x => x.client_no);
                    table.ForeignKey(
                        name: "FK__clients__occupat__46B27FE2",
                        column: x => x.occupation,
                        principalTable: "occupation",
                        principalColumn: "code");
                });

            migrationBuilder.CreateTable(
                name: "Product_Benefit",
                columns: table => new
                {
                    Product_Benefit_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Product_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    Is_Delete = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Benefit", x => x.Product_Benefit_ID);
                    table.ForeignKey(
                        name: "FK_Product_Benefit_Product",
                        column: x => x.Product_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID");
                });

            migrationBuilder.CreateTable(
                name: "Product_Coverage",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Product_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Coverage_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Coverage", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Product_Coverage_Coverage",
                        column: x => x.Coverage_ID,
                        principalTable: "Coverage",
                        principalColumn: "Coverage_ID");
                    table.ForeignKey(
                        name: "FK_Product_Coverage_Product",
                        column: x => x.Product_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID");
                });

            migrationBuilder.CreateTable(
                name: "Proposition",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PropositionCategoryID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogoImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackgroudImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description_EN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eligibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HotlineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PartnerPhoneNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartnerWebsiteLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartnerFacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartnerInstagramUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PartnerTwitterUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HotlineButtonText_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HotlineButtonText_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HotlineNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Created_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_By = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    Is_Delete = table.Column<bool>(type: "bit", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true),
                    Address_Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressLabelMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowToShowCashlessClaim = table.Column<bool>(type: "bit", nullable: true),
                    CashlessClaimProcedureInfo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposition", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Proposition_PropositionCategory",
                        column: x => x.PropositionCategoryID,
                        principalTable: "PropositionCategory",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Password_Hash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Password_Salt = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Is_Active = table.Column<bool>(type: "bit", nullable: true),
                    Created_Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Updated_Date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.ID);
                    table.ForeignKey(
                        name: "FK__Staffs__Role_ID__681373AD",
                        column: x => x.Role_ID,
                        principalTable: "Roles",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "InsuranceMapping",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    ComponentCode = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitProductComponet", x => x.ID);
                    table.ForeignKey(
                        name: "FK_InsuranceMapping_InsuranceBenefit",
                        column: x => x.ClaimId,
                        principalTable: "InsuranceBenefit",
                        principalColumn: "ClaimID");
                });

            migrationBuilder.CreateTable(
                name: "ClaimTran",
                columns: table => new
                {
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainClaimCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimFormType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimTypeMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CausedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CausedByNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CausedByNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CausedByCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CausedByDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    BankNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimForPolicyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiagnosisNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosisNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosisCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HospitalNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HospitalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationNameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationNameMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoctorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreatmentCount = table.Column<int>(type: "int", nullable: true),
                    TreatmentDates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreatmentFromDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    TreatmentToDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IncurredAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    IncidentSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantDob = table.Column<DateTime>(type: "datetime", nullable: true),
                    ClaimantGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantRelationship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantRelationshipMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantIdenType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimantIdenValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HolderClientNo = table.Column<string>(type: "varchar(8)", nullable: true),
                    InsuredClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimStatusCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemainingTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CausedByType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ILErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FERequestOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    FEResponseOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    ILRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ILRequestOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    ILResponseOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    FERequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FEResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccHolderIdValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccHolderDob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmRequestOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CrmResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndividualClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CrmResponseOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EligibleComponents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EligibleAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProgressAsPercent = table.Column<int>(type: "int", nullable: true),
                    ProgressAsHours = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MemberType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupMemberID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndividualMemberID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentSms = table.Column<bool>(type: "bit", nullable: true),
                    SentSmsAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claim_ClaimId", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_ClaimTran_ClaimTran",
                        column: x => x.HolderClientNo,
                        principalTable: "clients",
                        principalColumn: "client_no");
                });

            migrationBuilder.CreateTable(
                name: "Member_Clients",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Member_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    client_no = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member_Clients", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Member_Clients_Member",
                        column: x => x.Member_ID,
                        principalTable: "Member",
                        principalColumn: "Member_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Member_Clients_clients_client_no",
                        column: x => x.client_no,
                        principalTable: "clients",
                        principalColumn: "client_no",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "policies",
                columns: table => new
                {
                    policy_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    product_type = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    agent_code = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    number_of_unit = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    policy_holder_client_no = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    insured_person_client_no = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    payment_frequency = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    paid_to_date = table.Column<DateTime>(type: "date", nullable: true),
                    policy_status = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    premium_status = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    policy_term = table.Column<int>(type: "int", nullable: true),
                    premium_term = table.Column<int>(type: "int", nullable: true),
                    installment_premium = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    annualized_premium = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    sum_assured = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    first_issue_date = table.Column<DateTime>(type: "date", nullable: true),
                    policy_issue_date = table.Column<DateTime>(type: "date", nullable: true),
                    original_commencement_date = table.Column<DateTime>(type: "date", nullable: true),
                    risk_commencement_date = table.Column<DateTime>(type: "date", nullable: true),
                    components = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    acp_mode_flag = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true),
                    premium_due = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    outstanding_premium = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    outstanding_interest = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    created_date = table.Column<DateTime>(type: "date", nullable: true),
                    updated_date = table.Column<DateTime>(type: "date", nullable: true),
                    policy_expiry_date = table.Column<DateTime>(type: "date", nullable: true),
                    policy_lapsed_date = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__policies__47DA175095C08A02", x => x.policy_no);
                    table.ForeignKey(
                        name: "FK__policies__insure__47A6A41B",
                        column: x => x.insured_person_client_no,
                        principalTable: "clients",
                        principalColumn: "client_no");
                    table.ForeignKey(
                        name: "FK__policies__policy__489AC854",
                        column: x => x.policy_holder_client_no,
                        principalTable: "clients",
                        principalColumn: "client_no");
                    table.ForeignKey(
                        name: "FK__policies__policy__498EEC8D",
                        column: x => x.policy_status,
                        principalTable: "policy_status",
                        principalColumn: "short_desc");
                    table.ForeignKey(
                        name: "FK__policies__premiu__4A8310C6",
                        column: x => x.premium_status,
                        principalTable: "premium_status",
                        principalColumn: "short_desc");
                    table.ForeignKey(
                        name: "FK__policies__produc__4B7734FF",
                        column: x => x.product_type,
                        principalTable: "product_type",
                        principalColumn: "short_desc");
                });

            migrationBuilder.CreateTable(
                name: "PropositionAddress",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropositionID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address_EN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionAddress", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PropositionAddress_Proposition",
                        column: x => x.PropositionID,
                        principalTable: "Proposition",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PropositionBenefit",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropositionID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name_EN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GroupName_EN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GroupName_MM = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionBenefit", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PropositionBenefit_Proposition",
                        column: x => x.PropositionID,
                        principalTable: "Proposition",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PropositionBranch",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropositionID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name_EN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name_MM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sort = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionBranch", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PropositionBranch_Proposition",
                        column: x => x.PropositionID,
                        principalTable: "Proposition",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ClaimBenefit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MainClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BenefitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitFromDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    BenefitToDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    BenefitAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    TotalCalculatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BenefitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumOfDays = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimBenefit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimBenefit_Claim",
                        column: x => x.ClaimId,
                        principalTable: "ClaimTran",
                        principalColumn: "ClaimId");
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MainClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocTypeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    UploadStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsRequestOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CmsResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsResponseOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    DocName2 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_Claim",
                        column: x => x.ClaimId,
                        principalTable: "ClaimTran",
                        principalColumn: "ClaimId");
                });

            migrationBuilder.CreateTable(
                name: "beneficiaries",
                columns: table => new
                {
                    policy_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    beneficiary_client_no = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    relationship = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    created_date = table.Column<DateTime>(type: "date", nullable: true),
                    updated_date = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK__beneficia__benef__40F9A68C",
                        column: x => x.beneficiary_client_no,
                        principalTable: "clients",
                        principalColumn: "client_no");
                    table.ForeignKey(
                        name: "FK__beneficia__polic__41EDCAC5",
                        column: x => x.policy_no,
                        principalTable: "policies",
                        principalColumn: "policy_no");
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    claim_id = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    claim_id_il = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    policy_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    product_type = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    claim_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    bank_name = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    account_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    status = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    claiment_client_no = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    received_date = table.Column<DateTime>(type: "date", nullable: true),
                    reject_reason = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "date", nullable: true),
                    updated_date = table.Column<DateTime>(type: "date", nullable: true),
                    followup_reason = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.claim_id);
                    table.ForeignKey(
                        name: "FK__claims__claiment__42E1EEFE",
                        column: x => x.claiment_client_no,
                        principalTable: "clients",
                        principalColumn: "client_no");
                    table.ForeignKey(
                        name: "FK__claims__policy_n__43D61337",
                        column: x => x.policy_no,
                        principalTable: "policies",
                        principalColumn: "policy_no");
                    table.ForeignKey(
                        name: "FK__claims__product___44CA3770",
                        column: x => x.product_type,
                        principalTable: "product_type",
                        principalColumn: "short_desc");
                    table.ForeignKey(
                        name: "FK__claims__status__45BE5BA9",
                        column: x => x.status,
                        principalTable: "claim_status",
                        principalColumn: "short_desc");
                });

            migrationBuilder.CreateTable(
                name: "PropositionRequest",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropositionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MemberType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppointmentSpecialist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Benefits = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropositionRequest", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PropositionRequest_Member_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Member",
                        principalColumn: "Member_ID");
                    table.ForeignKey(
                        name: "FK_PropositionRequest_PropositionBranch_BranchID",
                        column: x => x.BranchID,
                        principalTable: "PropositionBranch",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_PropositionRequest_Proposition_PropositionID",
                        column: x => x.PropositionID,
                        principalTable: "Proposition",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimsStatusUpdate",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    OldStatus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsDone = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    ChangedByAiaPlus = table.Column<bool>(type: "bit", nullable: true),
                    NewStatusDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStatusDescMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemarkFromIL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayableAmountFromIL = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FormattedReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimsStatusUpdate", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ClaimsStatusUpdate_claims",
                        column: x => x.ClaimID,
                        principalTable: "claims",
                        principalColumn: "claim_id");
                });

            migrationBuilder.CreateTable(
                name: "MemberNotification",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleMm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ClaimID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ServicingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    IsSytemNoti = table.Column<bool>(type: "bit", maxLength: 100, nullable: true),
                    SystemNotiType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsScheduledDone = table.Column<bool>(type: "bit", nullable: true),
                    IsScheduled = table.Column<bool>(type: "bit", nullable: true),
                    ProductID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PromotionID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PropositionID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ActivityID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    JobID = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimStatusCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PremiumPolicyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CmsNotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommonKeyId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberNotification", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MemberNotification_claims",
                        column: x => x.ClaimID,
                        principalTable: "claims",
                        principalColumn: "claim_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_beneficiaries_beneficiary_client_no",
                table: "beneficiaries",
                column: "beneficiary_client_no");

            migrationBuilder.CreateIndex(
                name: "IX_beneficiaries_policy_no",
                table: "beneficiaries",
                column: "policy_no");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimBenefit_ClaimId",
                table: "ClaimBenefit",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_ClaimId",
                table: "ClaimDocuments",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_claiment_client_no",
                table: "claims",
                column: "claiment_client_no");

            migrationBuilder.CreateIndex(
                name: "IX_claims_policy_no",
                table: "claims",
                column: "policy_no");

            migrationBuilder.CreateIndex(
                name: "IX_claims_product_type",
                table: "claims",
                column: "product_type");

            migrationBuilder.CreateIndex(
                name: "IX_claims_status",
                table: "claims",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsStatusUpdate_ClaimID",
                table: "ClaimsStatusUpdate",
                column: "ClaimID");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimTran_HolderClientNo",
                table: "ClaimTran",
                column: "HolderClientNo");

            migrationBuilder.CreateIndex(
                name: "IX_clients_occupation",
                table: "clients",
                column: "occupation");

            migrationBuilder.CreateIndex(
                name: "IX_FaqQuestion_FaqTopicId",
                table: "FaqQuestion",
                column: "FaqTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceBenefit_InsuranceTypeID",
                table: "InsuranceBenefit",
                column: "InsuranceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceMapping_ClaimId",
                table: "InsuranceMapping",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_Member_Clients_client_no",
                table: "Member_Clients",
                column: "client_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Member_Clients_Member_ID",
                table: "Member_Clients",
                column: "Member_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberBank_BankID",
                table: "MemberBank",
                column: "BankID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberNotification_ClaimID",
                table: "MemberNotification",
                column: "ClaimID");

            migrationBuilder.CreateIndex(
                name: "IX_policies_insured_person_client_no",
                table: "policies",
                column: "insured_person_client_no");

            migrationBuilder.CreateIndex(
                name: "IX_policies_policy_holder_client_no",
                table: "policies",
                column: "policy_holder_client_no");

            migrationBuilder.CreateIndex(
                name: "IX_policies_policy_status",
                table: "policies",
                column: "policy_status");

            migrationBuilder.CreateIndex(
                name: "IX_policies_premium_status",
                table: "policies",
                column: "premium_status");

            migrationBuilder.CreateIndex(
                name: "IX_policies_product_type",
                table: "policies",
                column: "product_type");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Benefit_Product_ID",
                table: "Product_Benefit",
                column: "Product_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Coverage_Coverage_ID",
                table: "Product_Coverage",
                column: "Coverage_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Coverage_Product_ID",
                table: "Product_Coverage",
                column: "Product_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProduct_BlogID",
                table: "PromotionProduct",
                column: "BlogID");

            migrationBuilder.CreateIndex(
                name: "IX_Proposition_PropositionCategoryID",
                table: "Proposition",
                column: "PropositionCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionAddress_PropositionID",
                table: "PropositionAddress",
                column: "PropositionID");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionBenefit_PropositionID",
                table: "PropositionBenefit",
                column: "PropositionID");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionBranch_PropositionID",
                table: "PropositionBranch",
                column: "PropositionID");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionRequest_BranchID",
                table: "PropositionRequest",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionRequest_MemberID",
                table: "PropositionRequest",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_PropositionRequest_PropositionID",
                table: "PropositionRequest",
                column: "PropositionID");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Role_ID",
                table: "Staffs",
                column: "Role_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "App_Configs");

            migrationBuilder.DropTable(
                name: "App_Versions");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "Authorization_Person");

            migrationBuilder.DropTable(
                name: "Authorization_Product");

            migrationBuilder.DropTable(
                name: "Authorization_Status");

            migrationBuilder.DropTable(
                name: "beneficiaries");

            migrationBuilder.DropTable(
                name: "BeneficiaryCheckList");

            migrationBuilder.DropTable(
                name: "CashlessClaimConfig");

            migrationBuilder.DropTable(
                name: "CI_Product");

            migrationBuilder.DropTable(
                name: "Claim_Service_Otp_Setup");

            migrationBuilder.DropTable(
                name: "ClaimBenefit");

            migrationBuilder.DropTable(
                name: "ClaimDocType");

            migrationBuilder.DropTable(
                name: "ClaimDocumentMapping");

            migrationBuilder.DropTable(
                name: "ClaimDocuments");

            migrationBuilder.DropTable(
                name: "ClaimDocuments_MedicalBill_ApiLog");

            migrationBuilder.DropTable(
                name: "ClaimFollowup");

            migrationBuilder.DropTable(
                name: "ClaimIncurredLocation");

            migrationBuilder.DropTable(
                name: "ClaimSaveBank");

            migrationBuilder.DropTable(
                name: "ClaimsStatusUpdate");

            migrationBuilder.DropTable(
                name: "ClaimValidateMessage");

            migrationBuilder.DropTable(
                name: "Client_Corporate");

            migrationBuilder.DropTable(
                name: "CmsNotification");

            migrationBuilder.DropTable(
                name: "CmsNotificationJobLocker");

            migrationBuilder.DropTable(
                name: "CmsUserSession");

            migrationBuilder.DropTable(
                name: "CommonOtp");

            migrationBuilder.DropTable(
                name: "country");

            migrationBuilder.DropTable(
                name: "CriticalIllness");

            migrationBuilder.DropTable(
                name: "CrmApiLog");

            migrationBuilder.DropTable(
                name: "CrmClaimCode");

            migrationBuilder.DropTable(
                name: "CrmSignature");

            migrationBuilder.DropTable(
                name: "Death");

            migrationBuilder.DropTable(
                name: "DefaultCmsImage");

            migrationBuilder.DropTable(
                name: "Diagnosis");

            migrationBuilder.DropTable(
                name: "district");

            migrationBuilder.DropTable(
                name: "DocConfig");

            migrationBuilder.DropTable(
                name: "ErrorLogCms");

            migrationBuilder.DropTable(
                name: "ErrorLogMobile");

            migrationBuilder.DropTable(
                name: "FaqQuestion");

            migrationBuilder.DropTable(
                name: "Holiday");

            migrationBuilder.DropTable(
                name: "Hospital");

            migrationBuilder.DropTable(
                name: "InOutPatientReasonBenefitCode");

            migrationBuilder.DropTable(
                name: "InsuranceClaimDocument");

            migrationBuilder.DropTable(
                name: "InsuranceMapping");

            migrationBuilder.DropTable(
                name: "JobID");

            migrationBuilder.DropTable(
                name: "Localization");

            migrationBuilder.DropTable(
                name: "Member_Clients");

            migrationBuilder.DropTable(
                name: "MemberBank");

            migrationBuilder.DropTable(
                name: "MemberDevice");

            migrationBuilder.DropTable(
                name: "MemberNotification");

            migrationBuilder.DropTable(
                name: "MemberSession");

            migrationBuilder.DropTable(
                name: "Okta_ServiceToken");

            migrationBuilder.DropTable(
                name: "OnetimeToken");

            migrationBuilder.DropTable(
                name: "PartialDisability");

            migrationBuilder.DropTable(
                name: "PartialDisabilityProduct");

            migrationBuilder.DropTable(
                name: "PaymentChangeConfig");

            migrationBuilder.DropTable(
                name: "PermanentDisability");

            migrationBuilder.DropTable(
                name: "PlanData");

            migrationBuilder.DropTable(
                name: "policy_additional_amt");

            migrationBuilder.DropTable(
                name: "PolicyExcludedList");

            migrationBuilder.DropTable(
                name: "Product_Benefit");

            migrationBuilder.DropTable(
                name: "Product_Coverage");

            migrationBuilder.DropTable(
                name: "PromotionProduct");

            migrationBuilder.DropTable(
                name: "PropositionAddress");

            migrationBuilder.DropTable(
                name: "PropositionBenefit");

            migrationBuilder.DropTable(
                name: "PropositionRequest");

            migrationBuilder.DropTable(
                name: "province");

            migrationBuilder.DropTable(
                name: "push_notification_log");

            migrationBuilder.DropTable(
                name: "RateLimitControlOtpAttempts");

            migrationBuilder.DropTable(
                name: "RateLimitOtpBruteForceAttempts");

            migrationBuilder.DropTable(
                name: "ReasonCode");

            migrationBuilder.DropTable(
                name: "Relationship");

            migrationBuilder.DropTable(
                name: "Route");

            migrationBuilder.DropTable(
                name: "RulesMatrix");

            migrationBuilder.DropTable(
                name: "ServiceACPLoanRepayment");

            migrationBuilder.DropTable(
                name: "ServiceACPLoanRepaymentDoc");

            migrationBuilder.DropTable(
                name: "ServiceAdhocTopup");

            migrationBuilder.DropTable(
                name: "ServiceAdhocTopupDoc");

            migrationBuilder.DropTable(
                name: "ServiceBeneficiary");

            migrationBuilder.DropTable(
                name: "ServiceBeneficiaryPersonalInfo");

            migrationBuilder.DropTable(
                name: "ServiceBeneficiaryShareInfo");

            migrationBuilder.DropTable(
                name: "ServiceHealthRenewal");

            migrationBuilder.DropTable(
                name: "ServiceHealthRenewalDoc");

            migrationBuilder.DropTable(
                name: "ServiceLapseReinstatement");

            migrationBuilder.DropTable(
                name: "ServiceLapseReinstatementDoc");

            migrationBuilder.DropTable(
                name: "ServiceMain");

            migrationBuilder.DropTable(
                name: "ServiceMainDoc");

            migrationBuilder.DropTable(
                name: "ServicePartialWithdraw");

            migrationBuilder.DropTable(
                name: "ServicePartialWithdrawDoc");

            migrationBuilder.DropTable(
                name: "ServicePaymentFrequency");

            migrationBuilder.DropTable(
                name: "ServicePaymentFrequencyValidateMessage");

            migrationBuilder.DropTable(
                name: "ServicePolicyLoan");

            migrationBuilder.DropTable(
                name: "ServicePolicyLoanDoc");

            migrationBuilder.DropTable(
                name: "ServicePolicyLoanRepayment");

            migrationBuilder.DropTable(
                name: "ServicePolicyLoanRepaymentDoc");

            migrationBuilder.DropTable(
                name: "ServicePolicyMapping");

            migrationBuilder.DropTable(
                name: "ServicePolicyPaidUp");

            migrationBuilder.DropTable(
                name: "ServicePolicyPaidUpDoc");

            migrationBuilder.DropTable(
                name: "ServicePolicySurrender");

            migrationBuilder.DropTable(
                name: "ServicePolicySurrenderDoc");

            migrationBuilder.DropTable(
                name: "ServiceRefundOfPayment");

            migrationBuilder.DropTable(
                name: "ServiceRefundOfPaymentDoc");

            migrationBuilder.DropTable(
                name: "ServiceStatusUpdate");

            migrationBuilder.DropTable(
                name: "ServiceSumAssuredChange");

            migrationBuilder.DropTable(
                name: "ServiceSumAssuredChangeDoc");

            migrationBuilder.DropTable(
                name: "ServiceType");

            migrationBuilder.DropTable(
                name: "servicing_status");

            migrationBuilder.DropTable(
                name: "ServicingRequest");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "TestUsers");

            migrationBuilder.DropTable(
                name: "township");

            migrationBuilder.DropTable(
                name: "TxnsPayment");

            migrationBuilder.DropTable(
                name: "users_temp");

            migrationBuilder.DropTable(
                name: "ClaimTran");

            migrationBuilder.DropTable(
                name: "FaqTopic");

            migrationBuilder.DropTable(
                name: "InsuranceBenefit");

            migrationBuilder.DropTable(
                name: "Bank");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "Coverage");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Blog");

            migrationBuilder.DropTable(
                name: "Member");

            migrationBuilder.DropTable(
                name: "PropositionBranch");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "BufferTxnsPayment");

            migrationBuilder.DropTable(
                name: "InsuranceType");

            migrationBuilder.DropTable(
                name: "policies");

            migrationBuilder.DropTable(
                name: "claim_status");

            migrationBuilder.DropTable(
                name: "Proposition");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "policy_status");

            migrationBuilder.DropTable(
                name: "premium_status");

            migrationBuilder.DropTable(
                name: "product_type");

            migrationBuilder.DropTable(
                name: "PropositionCategory");

            migrationBuilder.DropTable(
                name: "occupation");
        }
    }
}
