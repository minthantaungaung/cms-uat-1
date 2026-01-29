using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace aia_core.Entities;

public partial class Context : DbContext
{
    public Context()
    {
    }

    public Context(DbContextOptions<Context> options)
        : base(options)
    {
    }

    public virtual DbSet<RateLimitOtpBruteForceAttempts> RateLimitOtpBruteForceAttempts { get; set; }
    public virtual DbSet<RateLimitControlOtpAttempts> RateLimitControlOtpAttempts { get; set; }
    public virtual DbSet<BufferTxnsPayment> BufferTxnsPayments { get; set; }
    public virtual DbSet<TxnsPayment> TxnsPayments { get; set; }
    public virtual DbSet<ApiKeys> ApiKeys { get; set; }
    public virtual DbSet<PolicyExcludedList> PolicyExcludedLists { get; set; }

    public virtual DbSet<OnetimeToken> OnetimeToken { get; set; }

    public virtual DbSet<PlanData> PlanData { get; set; }

    public virtual DbSet<CashlessClaimConfig> CashlessClaimConfig { get; set; }

    public virtual DbSet<FaqQuestion> FaqQuestion { get; set; }
    public virtual DbSet<FaqTopic> FaqTopic { get; set; }

    public virtual DbSet<PartialDisabilityProduct> PartialDisabilityProduct { get; set; }
    public virtual DbSet<CmsNotificationJobLocker> CmsNotificationJobLocker { get; set; }
    public virtual DbSet<CmsNotification> CmsNotification { get; set; }
    public virtual DbSet<PushNotificationLog> PushNotificationLog { get; set; }

    public virtual DbSet<DefaultCmsImage> DefaultCmsImage { get; set; }
    public virtual DbSet<BeneficiaryCheckList> BeneficiaryCheckLists { get; set; }
    public virtual DbSet<CmsUserSession> CmsUserSessions { get; set; }
    public virtual DbSet<ServiceMainDoc> ServiceMainDocs { get; set; }
    public virtual DbSet<PolicyAdditionalAmt> PolicyAdditionalAmts { get; set; }
    public virtual DbSet<ClaimValidateMessage> ClaimValidateMessages { get; set; }
    public virtual DbSet<ServicePaymentFrequencyValidateMessage> ServicePaymentFrequencyValidateMessages { get; set; }
    public virtual DbSet<ServicePolicyMapping> ServicePolicyMappings { get; set; }
    public virtual DbSet<DocConfig> DocConfigs { get; set; }

    public virtual DbSet<ServiceType> ServiceTypes { get; set; }
    public virtual DbSet<OktaServiceToken> OktaServiceTokens { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<JobId> JobId { get; set; }

    public virtual DbSet<ClaimsStatusUpdate> ClaimsStatusUpdates { get; set; }

    public virtual DbSet<MemberNotification> MemberNotifications { get; set; }
    public virtual DbSet<MemberDevice> MemberDevice { get; set; }
    public virtual DbSet<AppConfig> AppConfigs { get; set; }

    public virtual DbSet<AppVersion> AppVersions { get; set; }

    public virtual DbSet<AuthorizationPerson> AuthorizationPeople { get; set; }

    public virtual DbSet<AuthorizationProduct> AuthorizationProducts { get; set; }

    public virtual DbSet<AuthorizationStatus> AuthorizationStatuses { get; set; }
    public virtual DbSet<Bank> Banks { get; set; }
    public virtual DbSet<MemberBank> MemberBanks { get; set; }

    public virtual DbSet<Beneficiary> Beneficiaries { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<Claim> Claims { get; set; }

    public virtual DbSet<ClaimStatus> ClaimStatuses { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Coverage> Coverages { get; set; }

    public virtual DbSet<ErrorLogCms> ErrorLogCms { get; set; }

    public virtual DbSet<ErrorLogMobile> ErrorLogMobile { get; set; }

    public virtual DbSet<Localization> Localizations { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberClient> MemberClients { get; set; }

    public virtual DbSet<MemberSession> MemberSessions { get; set; }

    public virtual DbSet<Occupation> Occupations { get; set; }

    public virtual DbSet<Policy> Policies { get; set; }

    public virtual DbSet<PolicyStatus> PolicyStatuses { get; set; }

    public virtual DbSet<PremiumStatus> PremiumStatuses { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductBenefit> ProductBenefits { get; set; }

    public virtual DbSet<ProductCoverage> ProductCoverages { get; set; }

    public virtual DbSet<ProductType> ProductTypes { get; set; }

    public virtual DbSet<PromotionProduct> PromotionProducts { get; set; }

    public virtual DbSet<Proposition> Propositions { get; set; }

    public virtual DbSet<PropositionAddress> PropositionAddresses { get; set; }

    public virtual DbSet<PropositionBenefit> PropositionBenefits { get; set; }

    public virtual DbSet<PropositionBranch> PropositionBranches { get; set; }

    public virtual DbSet<PropositionCategory> PropositionCategories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Staff> Staffs { get; set; }
    public virtual DbSet<ClientCorporate> ClientCorporates { get; set; }

    public virtual DbSet<InsuranceBenefit> InsuranceBenefits { get; set; }

    public virtual DbSet<InsuranceMapping> InsuranceMappings { get; set; }

    public virtual DbSet<InsuranceType> InsuranceTypes { get; set; }

    public virtual DbSet<ClaimDocumentMapping> ClaimDocumentMappings { get; set; }

    public virtual DbSet<InsuranceClaimDocument> InsuranceClaimDocuments { get; set; }


    public virtual DbSet<Hospital> Hospitals { get; set; }
    public virtual DbSet<ClaimIncurredLocation> ClaimIncurredLocations { get; set; }
    public virtual DbSet<Diagnosis> Diagnosis { get; set; }
    public virtual DbSet<PartialDisability> PartialDisability { get; set; }
    public virtual DbSet<PermanentDisability> PermanentDisability { get; set; }
    public virtual DbSet<CriticalIllness> CriticalIllness { get; set; }
    public virtual DbSet<Death> Death { get; set; }
    public virtual DbSet<Relationship> Relationship { get; set; }
    
    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<ClaimBenefit> ClaimBenefits { get; set; }

    public virtual DbSet<ClaimDocument> ClaimDocuments { get; set; }

    public virtual DbSet<ClaimTran> ClaimTrans { get; set; }

    public virtual DbSet<ClaimSaveBank> ClaimSaveBanks { get; set; }
    public virtual DbSet<CommonOtp> CommonOtps { get; set; }

    public virtual DbSet<PropositionRequest> PropositionRequest { get; set; }
    public virtual DbSet<ClaimDocType> ClaimDocType { get; set; }
    public virtual DbSet<Holiday> Holiday { get; set; }
    public virtual DbSet<CrmSignature> CrmSignature { get; set; }

    public virtual DbSet<CrmClaimCode> CrmClaimCodes { get; set; }
    public virtual DbSet<ReasonCode> ReasonCodes { get; set; }

    public virtual DbSet<InOutPatientReasonBenefitCode> InOutPatientReasonBenefitCodes { get; set; }

    public virtual DbSet<ClaimFollowup> ClaimFollowups { get; set; }

    public virtual DbSet<Country> Country { get; set; }
    public virtual DbSet<Province> Province { get; set; }
    public virtual DbSet<District> District { get; set; }
    public virtual DbSet<Township> Township { get; set; }
    public virtual DbSet<ServicingRequest> ServicingRequest { get; set; }
    public virtual DbSet<ServiceLapseReinstatement> ServiceLapseReinstatement { get; set; }
    public virtual DbSet<ServiceLapseReinstatementDoc> ServiceLapseReinstatementDoc { get; set; }

    public virtual DbSet<ServiceHealthRenewal> ServiceHealthRenewal { get; set; }
    public virtual DbSet<ServiceHealthRenewalDoc> ServiceHealthRenewalDoc { get; set; }

    public virtual DbSet<ServiceACPLoanRepayment> ServiceACPLoanRepayment { get; set; }
    public virtual DbSet<ServiceACPLoanRepaymentDoc> ServiceACPLoanRepaymentDoc { get; set; }
    public virtual DbSet<ServiceAdhocTopup> ServiceAdhocTopup { get; set; }
    public virtual DbSet<ServiceAdhocTopupDoc> ServiceAdhocTopupDoc { get; set; }
    public virtual DbSet<ServicePolicyLoanRepayment> ServicePolicyLoanRepayment { get; set; }
    public virtual DbSet<ServicePolicyLoanRepaymentDoc> ServicePolicyLoanRepaymentDoc { get; set; }
    public virtual DbSet<ServicePartialWithdraw> ServicePartialWithdraw { get; set; }
    public virtual DbSet<ServicePartialWithdrawDoc> ServicePartialWithdrawDoc { get; set; }
    public virtual DbSet<ServicePolicyLoan> ServicePolicyLoan { get; set; }
    public virtual DbSet<ServicePolicyLoanDoc> ServicePolicyLoanDoc { get; set; }
    public virtual DbSet<ServicePolicySurrender> ServicePolicySurrender { get; set; }
    public virtual DbSet<ServicePolicySurrenderDoc> ServicePolicySurrenderDoc { get; set; }
    public virtual DbSet<ServicePolicyPaidUp> ServicePolicyPaidUp { get; set; }
    public virtual DbSet<ServicePolicyPaidUpDoc> ServicePolicyPaidUpDoc { get; set; }
    public virtual DbSet<ServiceRefundOfPayment> ServiceRefundOfPayment { get; set; }
    public virtual DbSet<ServiceRefundOfPaymentDoc> ServiceRefundOfPaymentDoc { get; set; }
    public virtual DbSet<ServicePaymentFrequency> ServicePaymentFrequency { get; set; }
    public virtual DbSet<ServiceBeneficiary> ServiceBeneficiary { get; set; }
    public virtual DbSet<ServiceBeneficiaryPersonalInfo> ServiceBeneficiaryPersonalInfo { get; set; }
    public virtual DbSet<ServiceBeneficiaryShareInfo> ServiceBeneficiaryShareInfo { get; set; }
    public virtual DbSet<ServiceMain> ServiceMain { get; set; }
    public virtual DbSet<PaymentChangeConfig> PaymentChangeConfigs { get; set; }
    public virtual DbSet<RulesMatrix> RulesMatrices { get; set; }
    public virtual DbSet<CrmApiLog> CrmApiLog { get; set; }
    public virtual DbSet<ServiceStatusUpdate> ServiceStatusUpdate { get; set; }
    public virtual DbSet<ServicingStatus> ServicingStatus { get; set; }
    public virtual DbSet<UsersTemp> UsersTemps { get; set; }

    public virtual DbSet<CI_Product> CI_Product { get; set; }

    public virtual DbSet<ClaimDocumentsMedicaBillApiLog> ClaimDocumentsMedicaBillApiLog { get; set; }

    public virtual DbSet<TestUsers> TestUsers { get; set; }

    public virtual DbSet<Claim_Service_Otp_Setup> Claim_Service_Otp_Setup { get; set; }

    //     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
    //         => optionsBuilder.UseSqlServer("Data Source=aia-plus-staging.database.windows.net;Initial Catalog=aia-plus-staging;User ID=aiaplusstaging;Password=aia123!@#360;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var guidConverter = new ValueConverter<string, Guid>(
            v => new Guid(v),
            v => v.ToString()
        );

        modelBuilder.Entity<Claim_Service_Otp_Setup>(entity =>
        {
            entity.ToTable("Claim_Service_Otp_Setup");
            entity.HasKey(e => e.FormName);
        });

        modelBuilder.Entity<TestUsers>(entity =>
        {
            entity.ToTable("TestUsers");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<BufferTxnsPayment>(entity =>
        {
            entity.ToTable("BufferTxnsPayment");
            entity.HasKey(e => e.TransactionId);
        });
        modelBuilder.Entity<PolicyExcludedList>(entity =>
        {
            entity.ToTable("PolicyExcludedList");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<TxnsPayment>(entity =>
        {
            entity.ToTable("TxnsPayment");
            entity.HasKey(e => e.TransactionID);
        });

        modelBuilder.Entity<ApiKeys>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.ApiKey);
        });
        modelBuilder.Entity<PolicyExcludedList>(entity =>
        {
            entity.ToTable("PolicyExcludedList");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ClaimDocumentsMedicaBillApiLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("ClaimDocuments_MedicalBill_ApiLog");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.ReceivedAt).HasColumnType("datetime");

        });

        modelBuilder.Entity<CI_Product>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("CI_Product");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.IsDeleted)
            .HasColumnName("IsDeleted");
        });

        modelBuilder.Entity<OnetimeToken>(entity =>
        {
            entity.ToTable("OnetimeToken");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PlanData>(entity =>
        {
            entity.ToTable("PlanData");
            entity.HasKey(e => e.PlanCode);
        });

        modelBuilder.Entity<CashlessClaimConfig>(entity =>
        {
            entity.ToTable("CashlessClaimConfig");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FaqTopic>(entity =>
        {
            entity.ToTable("FaqTopic");
            entity.HasKey(e => e.Id);

            entity.HasMany(e => e.FaqQuestions)
                .WithOne(e => e.FaqTopic)
                .HasForeignKey(e => e.FaqTopicId);

        });

        modelBuilder.Entity<FaqQuestion>(entity =>
        {
            entity.ToTable("FaqQuestion");
            entity.HasKey(e => e.Id);
        });


        modelBuilder.Entity<PartialDisabilityProduct>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("PartialDisabilityProduct");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.IsDeleted)
            .HasColumnName("IsDeleted");
        });

        modelBuilder.Entity<CmsNotificationJobLocker>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CmsNotificationJobLocker");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<CmsNotification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("CmsNotification");
            entity.Property(e => e.SendDateAndTime).HasColumnType("datetime");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.IsActive)
            .HasColumnName("IsActive");

            entity.Property(e => e.IsDeleted)
            .HasColumnName("IsDeleted");
        });

        modelBuilder.Entity<PushNotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("push_notification_log");

            entity.Property(e => e.SentOn)
                .HasColumnName("sent_on")
                .HasColumnType("datetime");            

            entity.Property(e => e.CreatedOn)
                .HasColumnName("created_on")
                .HasColumnType("datetime");

            entity.Property(e => e.DeviceModel)
                .HasColumnName("device_model");

            entity.Property(e => e.DeviceType)
                .HasColumnName("device_type");

            entity.Property(e => e.NotificationId)
                .HasColumnName("notification_id")
                .HasMaxLength(36);

            entity.Property(e => e.PushToken)
                .HasColumnName("push_token");            

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(36);

            entity.Property(e => e.NotiType)
               .HasColumnName("noti_type");

            entity.Property(e => e.IsSendSuccess)
            .HasColumnName("is_sendsuccess");

            entity.Property(e => e.FirebaseResult)
                .HasColumnName("firebase_result");

        });

        modelBuilder.Entity<DefaultCmsImage>(entity =>
        {
            entity.HasKey(e => e.id);
            entity.ToTable("DefaultCmsImage");
            entity.Property(e => e.created_at).HasColumnType("datetime");
        });

        modelBuilder.Entity<BeneficiaryCheckList>(entity =>
        {
            entity.ToTable("BeneficiaryCheckList");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<CmsUserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);

            entity.ToTable("CmsUserSession");

            entity.Property(e => e.SessionId).ValueGeneratedNever();
            entity.Property(e => e.ExpiredOn).HasColumnType("datetime");
            entity.Property(e => e.GeneratedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<ServiceMainDoc>(entity =>
        {
            entity.ToTable("ServiceMainDoc");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CmsRequestOn).HasColumnType("datetime");
            entity.Property(e => e.CmsResponseOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<PolicyAdditionalAmt>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("policy_additional_amt");

            entity.Property(e => e.AcpInterestAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("acp_interest_amount");
            entity.Property(e => e.AcpPrincipalAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("acp_principal_amount");
            entity.Property(e => e.HealthRenewalAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("health_renewal_amount");
            entity.Property(e => e.LoanInterestAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("loan_interest_amount");
            entity.Property(e => e.LoanPrincipalAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("loan_principal_amount");
            entity.Property(e => e.PolicyNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("policy_no");
            entity.Property(e => e.PremiumDueAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("premium_due_amount");
            entity.Property(e => e.ReinstatementInterestAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("reinstatement_interest_amount");
            entity.Property(e => e.ReinstatementPremiumAmount)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("reinstatement_premium_amount");
        });

        modelBuilder.Entity<ClaimValidateMessage>(entity =>
        {
            entity.ToTable("ClaimValidateMessage");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ClaimType).HasMaxLength(500);
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.MemberId)
                .HasMaxLength(500)
                .HasColumnName("MemberID");
            entity.Property(e => e.MemberName).HasMaxLength(500);
            entity.Property(e => e.MemberPhone).HasMaxLength(500);
            entity.Property(e => e.PolicyNumber).HasMaxLength(500);
        });

        modelBuilder.Entity<ServicePaymentFrequencyValidateMessage>(entity =>
        {
            entity.ToTable("ServicePaymentFrequencyValidateMessage");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ClientNo).HasMaxLength(500);
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.MobileNumber).HasMaxLength(500);
            entity.Property(e => e.New).HasMaxLength(500);
            entity.Property(e => e.Old).HasMaxLength(500);
            entity.Property(e => e.PolicyNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<ServicePolicyMapping>(entity =>
        {
            entity.ToTable("ServicePolicyMapping");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<DocConfig>(entity =>
        {
            entity.ToTable("DocConfig");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DocName).HasMaxLength(1000);
            entity.Property(e => e.DocType).HasMaxLength(1000);
            entity.Property(e => e.DocTypeId).HasMaxLength(1000);
            entity.Property(e => e.ShowingFor).HasMaxLength(1000);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<RulesMatrix>(entity =>
        {
            entity.ToTable("RulesMatrix");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Annually).HasMaxLength(1000);
            entity.Property(e => e.FrequencyName).HasMaxLength(1000);
            entity.Property(e => e.FromFrequency).HasMaxLength(10);
            entity.Property(e => e.Monthly).HasMaxLength(1000);
            entity.Property(e => e.Quarterly).HasMaxLength(1000);
            entity.Property(e => e.SemiAnnually).HasMaxLength(1000);
        });

        modelBuilder.Entity<PaymentChangeConfig>(entity =>
        {
            entity.ToTable("PaymentChangeConfig");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Status).HasDefaultValueSql("((1))");
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Value).HasColumnType("numeric(18, 0)");
        });

        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.HasKey(e => e.ServiceTypeId).HasName("PK__ServiceT__8ADFAA0C19EB440D");

            entity.ToTable("ServiceType");

            entity.Property(e => e.ServiceTypeId)
                .ValueGeneratedNever()
                .HasColumnName("ServiceTypeID");
            entity.Property(e => e.MainServiceTypeId).HasColumnName("MainServiceTypeID");
        });

        modelBuilder.Entity<ClaimFollowup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Table_2");

            entity.ToTable("ClaimFollowup");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<InOutPatientReasonBenefitCode>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("InOutPatientReasonBenefitCode");
        });

        modelBuilder.Entity<ReasonCode>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ReasonCode");

            entity.Property(e => e.ClaimType)
                .HasMaxLength(1000)
                .IsFixedLength();
            entity.Property(e => e.ComponentCode)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.ProductCode)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.ReasonCode1)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("ReasonCode");
        });

        modelBuilder.Entity<CrmClaimCode>(entity =>
        {
            entity.ToTable("CrmClaimCode");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ClaimCode).HasMaxLength(500);
            entity.Property(e => e.ClaimType).HasMaxLength(500);
        });

        modelBuilder.Entity<CommonOtp>(entity =>
        {
            entity.ToTable("CommonOtp");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.OtpCode).HasMaxLength(50);
            entity.Property(e => e.OtpExpiry).HasColumnType("datetime");
            entity.Property(e => e.OtpTo).HasMaxLength(50);
            entity.Property(e => e.OtpType).HasMaxLength(50);
        });

        modelBuilder.Entity<ClaimSaveBank>(entity =>
        {
            entity.ToTable("ClaimSaveBank");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
        });

        modelBuilder.Entity<ClaimBenefit>(entity =>
        {
            entity.ToTable("ClaimBenefit");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BenefitAmount).HasColumnType("numeric(18, 0)");
            entity.Property(e => e.BenefitFromDate).HasColumnType("datetime");
            entity.Property(e => e.BenefitToDate).HasColumnType("datetime");

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimBenefits)
                .HasForeignKey(d => d.ClaimId)
                .HasConstraintName("FK_ClaimBenefit_Claim");
        });

        modelBuilder.Entity<ClaimDocument>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CmsRequestOn).HasColumnType("datetime");
            entity.Property(e => e.CmsResponseOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimDocuments)
                .HasForeignKey(d => d.ClaimId)
                .HasConstraintName("FK_ClaimDocuments_Claim");
        });

        modelBuilder.Entity<ClaimTran>(entity =>
        {
            entity.HasKey(e => e.ClaimId).HasName("PK_Claim_ClaimId");

            entity.ToTable("ClaimTran");

            entity.Property(e => e.ClaimId).ValueGeneratedNever();
            entity.Property(e => e.CausedByDate).HasColumnType("datetime");
            entity.Property(e => e.HolderClientNo);
            entity.Property(e => e.ClaimantDob).HasColumnType("datetime");
            entity.Property(e => e.Ferequest).HasColumnName("FERequest");
            entity.Property(e => e.FerequestOn)
                .HasColumnType("datetime")
                .HasColumnName("FERequestOn");
            entity.Property(e => e.Feresponse).HasColumnName("FEResponse");
            entity.Property(e => e.FeresponseOn)
                .HasColumnType("datetime")
                .HasColumnName("FEResponseOn");
            entity.Property(e => e.IlerrorMessage).HasColumnName("ILErrorMessage");
            entity.Property(e => e.Ilrequest).HasColumnName("ILRequest");
            entity.Property(e => e.IlrequestOn)
                .HasColumnType("datetime")
                .HasColumnName("ILRequestOn");
            entity.Property(e => e.Ilresponse).HasColumnName("ILResponse");
            entity.Property(e => e.IlresponseOn)
                .HasColumnType("datetime")
                .HasColumnName("ILResponseOn");
            entity.Property(e => e.Ilstatus).HasColumnName("ILStatus");
            entity.Property(e => e.IncurredAmount).HasColumnType("numeric(18, 0)");
            entity.Property(e => e.TreatmentFromDate).HasColumnType("datetime");
            entity.Property(e => e.TreatmentToDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.Property(e => e.SentSmsAt)
                .HasColumnType("datetime")
                .HasColumnName("SentSmsAt");

            //entity.Property(e => e.EstimatedCompletedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Client).WithMany(p => p.ClaimTran)
                .HasForeignKey(d => d.HolderClientNo)
                .HasConstraintName("FK_ClaimTran_ClaimTran");

           


        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.ToTable("Route");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Permission)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Route1)
                .HasMaxLength(1000)
                .IsFixedLength()
                .HasColumnName("Route");
        });

        modelBuilder.Entity<ClaimDocumentMapping>(entity =>
        {
            entity.ToTable("ClaimDocumentMapping");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.BenefitFormType).HasMaxLength(500);
            entity.Property(e => e.DocTypeIdlist)
                .HasMaxLength(500)
                .HasColumnName("DocTypeIDList");
        });

        modelBuilder.Entity<InsuranceClaimDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId);

            entity.ToTable("InsuranceClaimDocument");

            entity.Property(e => e.DocumentId)
                .ValueGeneratedNever()
                .HasColumnName("DocumentID");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DocTypeId)
                .HasMaxLength(100)
                .HasColumnName("DocTypeID");
            entity.Property(e => e.DocTypeName).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.IsDeleted).HasColumnName("Is_Deleted");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<InsuranceBenefit>(entity =>
        {

            entity.HasKey(e => e.ClaimId);

            entity.ToTable("InsuranceBenefit");

            entity.Property(e => e.ClaimId)
                .ValueGeneratedNever()
                .HasColumnName("ClaimID");
            entity.Property(e => e.BenefitId).HasColumnName("BenefitID");
            entity.Property(e => e.InsuranceTypeId).HasColumnName("InsuranceTypeID");

            entity.HasOne(d => d.InsuranceType).WithMany(p => p.InsuranceBenefits)
                .HasForeignKey(d => d.InsuranceTypeId)
                .HasConstraintName("FK_InsuranceBenefit_InsuranceType");
        });

        modelBuilder.Entity<InsuranceMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BenefitProductComponet");

            entity.ToTable("InsuranceMapping");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ClaimId).HasColumnName("ClaimId");
            entity.Property(e => e.ComponentCode).IsUnicode(false);
            entity.Property(e => e.ProductCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Benefit).WithMany(p => p.InsuranceMappings)
                .HasForeignKey(d => d.ClaimId)
                .HasConstraintName("FK_InsuranceMapping_InsuranceBenefit");
        });

        modelBuilder.Entity<InsuranceType>(entity =>
        {
            entity.ToTable("InsuranceType");

            entity.Property(e => e.InsuranceTypeId)
                .ValueGeneratedNever()
                .HasColumnName("InsuranceTypeID");
        });


    modelBuilder.Entity<OktaServiceToken>(entity =>
        {
            entity.ToTable("Okta_ServiceToken");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.TokenType).HasColumnName("Token_Type").HasMaxLength(50);
            entity.Property(e => e.ExpiresIn).HasColumnName("Expires_In");
            entity.Property(e => e.AccessToken).HasColumnName("Access_Token");
            entity.Property(e => e.Scope).HasColumnName("Scope");
            entity.Property(e => e.CreatedDate).HasColumnName("Created_Date").HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnName("Updated_Date").HasColumnType("datetime");
        });

        modelBuilder.Entity<ClientCorporate>(entity =>
        {
            entity.HasKey(e => e.ClientNo).HasName("PK_Client_Coporate");

            entity.ToTable("Client_Corporate");

            entity.Property(e => e.ClientNo)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.CorporateClientNo)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("date");
            entity.Property(e => e.Dob).HasColumnType("date");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Gender)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.MemberTierType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MemberType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Nrc)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.Other)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.PassportNo)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNo)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.ScheduledDate).HasColumnType("date");
            entity.Property(e => e.UpdatedDate).HasColumnType("date");
        });


        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.LogDate).HasColumnType("datetime");
            entity.Property(e => e.ObjectGroup).HasMaxLength(500);
            entity.Property(e => e.ObjectId).HasColumnName("ObjectID");
            entity.Property(e => e.ObjectName).HasMaxLength(500);
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
        });
        modelBuilder.Entity<JobId>(entity =>
        {
            entity.ToTable("JobID");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.JobId1)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("JobID");
            entity.Property(e => e.PromotionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PromotionID");
        });

        modelBuilder.Entity<MemberNotification>(entity =>
        {
            entity.ToTable("MemberNotification");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ClaimId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ClaimID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.MemberId).HasColumnName("MemberID");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Claim).WithMany(p => p.MemberNotifications)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MemberNotification_claims");

            entity.Property(e => e.IsSytemNoti)
                .HasMaxLength(100)
                .HasColumnName("IsSytemNoti");
            entity.Property(e => e.SystemNotiType)
                .HasMaxLength(100)
                .HasColumnName("SystemNotiType");

            entity.Property(e => e.PromotionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PromotionID");

            entity.Property(e => e.PropositionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PropositionID");

            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ProductID");

            entity.Property(e => e.ActivityId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ActivityID");

            entity.Property(e => e.PublishedDate).HasColumnType("datetime");

            entity.Property(e => e.IsScheduledDone)
                .HasColumnName("IsScheduledDone");

            entity.Property(e => e.IsScheduled)
                .HasColumnName("IsScheduled");

            entity.Property(e => e.JobId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("JobID");
        });

        modelBuilder.Entity<ClaimsStatusUpdate>(entity =>
        {
            entity.ToTable("ClaimsStatusUpdate");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ClaimId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ClaimID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.NewStatus).HasMaxLength(500);
            entity.Property(e => e.OldStatus).HasMaxLength(500);

            entity.HasOne(d => d.Claim).WithMany(p => p.ClaimsStatusUpdates)
                .HasForeignKey(d => d.ClaimId)
                .HasConstraintName("FK_ClaimsStatusUpdate_claims");
        });

        modelBuilder.Entity<MemberDevice>(entity =>
        {
            entity.ToTable("MemberDevice");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.DeviceType)
                .HasMaxLength(36)
                .IsUnicode(false);
            entity.Property(e => e.MemberId)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("MemberID");
            entity.Property(e => e.PushToken)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
        });

        modelBuilder.Entity<AppConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_AppConfigs");

            entity.ToTable("App_Configs");

            entity.Property(e => e.Id)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ID");
            entity.Property(e => e.AiaCustomerCareEmail)
                .HasMaxLength(250)
                .HasColumnName("Aia_Customer_Care_Email");

            entity.Property(e => e.AiaMyanmarAddresses)
                .HasMaxLength(2000)
                .HasColumnName("Aia_Myanmar_Addresses");

            entity.Property(e => e.AiaMyanmarFacebookUrl)
                .HasMaxLength(250)
                .HasColumnName("Aia_Myanmar_FacebookUrl");

            entity.Property(e => e.AiaMyanmarInstagramUrl)
                .HasMaxLength(250)
                .HasColumnName("Aia_Myanmar_InstagramUrl");

            entity.Property(e => e.AiaMyanmarWebsite)
                .HasMaxLength(250)
                .HasColumnName("Aia_Myanmar_Website");

            entity.Property(e => e.ClaimArchiveFrequency)
                .HasMaxLength(250)
                .HasColumnName("Claim_Archive_Frequency");

            entity.Property(e => e.ServicingArchiveFrequency)
                .HasMaxLength(250)
                .HasColumnName("Servicing_Archive_Frequency");

            entity.Property(e => e.ClaimTatHours)
                .HasMaxLength(250)
                .HasColumnName("Claim_TAT_Hours");

            entity.Property(e => e.ServicingTatHours)
                .HasMaxLength(250)
                .HasColumnName("Servicing_TAT_Hours");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");

            entity.Property(e => e.ImagingIndividualFileSizeLimit)
                .HasMaxLength(250)
                .HasColumnName("Imaging_Individual_File_Size_Limit");

            

            entity.Property(e => e.SherContactNumber)
                .HasMaxLength(250)
                .HasColumnName("Sher_Contact_Number");
            entity.Property(e => e.ClaimEmail)
                .HasMaxLength(250)
                .HasColumnName("ClaimEmail");
            entity.Property(e => e.ServicingEmail)
                .HasMaxLength(250)
                .HasColumnName("ServicingEmail");
            entity.Property(e => e.ImagingTotalFileSizeLimit)
                .HasMaxLength(250)
                .HasColumnName("ImagingTotalFileSizeLimit");
            entity.Property(e => e.ServicingArchiveFrequency)
                .HasMaxLength(250)
                .HasColumnName("Servicing_Archive_Frequency");
            entity.Property(e => e.Maintenance_On)
                .HasColumnName("Maintenance_On");
            entity.Property(e => e.Maintenance_Title)
                .HasColumnName("Maintenance_Title");
            entity.Property(e => e.Maintenance_Desc)
                .HasColumnName("Maintenance_Desc");
            entity.Property(e => e.UpdatedBy).HasColumnName("Updated_By");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");

            
        });

        modelBuilder.Entity<AppVersion>(entity =>
        {
            entity.ToTable("App_Versions");

            entity.Property(e => e.Id)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.LatestAndroidVersion)
                .HasMaxLength(10)
                .HasColumnName("Latest_Android_Version");
            entity.Property(e => e.LatestIosVersion)
                .HasMaxLength(10)
                .HasColumnName("Latest_Ios_Version");
            entity.Property(e => e.MinimumAndroidVersion)
                .HasMaxLength(10)
                .HasColumnName("Minimum_Android_Version");
            entity.Property(e => e.MinimumIosVersion)
                .HasMaxLength(10)
                .HasColumnName("Minimum_Ios_Version");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");
        });

        modelBuilder.Entity<AuthorizationPerson>(entity =>
        {
            entity.ToTable("Authorization_Person");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Acp).HasColumnName("ACP");
            entity.Property(e => e.PersonType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuthorizationProduct>(entity =>
        {
            entity.ToTable("Authorization_Product");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Acp).HasColumnName("ACP");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuthorizationStatus>(entity =>
        {
            entity.ToTable("Authorization_Status");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Acp).HasColumnName("ACP");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StatusType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Beneficiary>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("beneficiaries");

            
            entity.Property(e => e.BeneficiaryClientNo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("beneficiary_client_no");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("date")
                .HasColumnName("created_date");
            entity.Property(e => e.Percentage)
                .HasColumnType("numeric(5, 2)")
                .HasColumnName("percentage");
            entity.Property(e => e.PolicyNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("policy_no");
            entity.Property(e => e.Relationship)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("relationship");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("date")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.BeneficiaryClientNoNavigation).WithMany()
                .HasForeignKey(d => d.BeneficiaryClientNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__beneficia__benef__40F9A68C");

            entity.HasOne(d => d.PolicyNoNavigation).WithMany()
                .HasForeignKey(d => d.PolicyNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__beneficia__polic__41EDCAC5");
        });
        modelBuilder.Entity<Bank>(entity => entity.ToTable("Bank"));
        modelBuilder.Entity<MemberBank>(entity => entity.ToTable("MemberBank"));
        modelBuilder.Entity<Hospital>(entity => entity.ToTable("Hospital"));
        modelBuilder.Entity<ClaimIncurredLocation>(entity => entity.ToTable("ClaimIncurredLocation"));
        modelBuilder.Entity<Diagnosis>(entity => entity.ToTable("Diagnosis"));
        modelBuilder.Entity<PartialDisability>(entity => entity.ToTable("PartialDisability"));
        modelBuilder.Entity<PermanentDisability>(entity => entity.ToTable("PermanentDisability"));
        modelBuilder.Entity<CriticalIllness>(entity => entity.ToTable("CriticalIllness"));
        modelBuilder.Entity<Death>(entity => entity.ToTable("Death"));
        modelBuilder.Entity<Relationship>(entity => entity.ToTable("Relationship"));
        modelBuilder.Entity<PropositionRequest>(entity => entity.ToTable("PropositionRequest"));
        modelBuilder.Entity<ClaimDocType>(entity => entity.ToTable("ClaimDocType"));
        modelBuilder.Entity<Holiday>(entity => entity.ToTable("Holiday"));
        modelBuilder.Entity<CrmSignature>(entity => entity.ToTable("CrmSignature"));

        modelBuilder.Entity<Country>(entity => entity.ToTable("country"));
        modelBuilder.Entity<Province>(entity => entity.ToTable("province"));
        modelBuilder.Entity<District>(entity => entity.ToTable("district"));
        modelBuilder.Entity<Township>(entity => entity.ToTable("township"));
        modelBuilder.Entity<ServicingRequest>(entity => entity.ToTable("ServicingRequest").ToTable(tb => tb.HasTrigger("trg_UpdateServiceMainStatus")));
        modelBuilder.Entity<ServiceLapseReinstatement>(entity => entity.ToTable("ServiceLapseReinstatement"));
        modelBuilder.Entity<ServiceLapseReinstatementDoc>(entity => entity.ToTable("ServiceLapseReinstatementDoc"));
        modelBuilder.Entity<ServiceHealthRenewal>(entity => entity.ToTable("ServiceHealthRenewal"));
        modelBuilder.Entity<ServiceHealthRenewalDoc>(entity => entity.ToTable("ServiceHealthRenewalDoc"));
        modelBuilder.Entity<ServiceACPLoanRepayment>(entity => entity.ToTable("ServiceACPLoanRepayment"));
        modelBuilder.Entity<ServiceACPLoanRepaymentDoc>(entity => entity.ToTable("ServiceACPLoanRepaymentDoc"));
        modelBuilder.Entity<ServiceAdhocTopup>(entity => entity.ToTable("ServiceAdhocTopup"));
        modelBuilder.Entity<ServiceAdhocTopupDoc>(entity => entity.ToTable("ServiceAdhocTopupDoc"));
        modelBuilder.Entity<ServicePolicyLoanRepayment>(entity => entity.ToTable("ServicePolicyLoanRepayment"));
        modelBuilder.Entity<ServicePolicyLoanRepaymentDoc>(entity => entity.ToTable("ServicePolicyLoanRepaymentDoc"));
        modelBuilder.Entity<ServiceSumAssuredChange>(entity => entity.ToTable("ServiceSumAssuredChange"));
        modelBuilder.Entity<ServiceSumAssuredChangeDoc>(entity => entity.ToTable("ServiceSumAssuredChangeDoc"));
        modelBuilder.Entity<ServicePartialWithdraw>(entity => entity.ToTable("ServicePartialWithdraw"));
        modelBuilder.Entity<ServicePartialWithdrawDoc>(entity => entity.ToTable("ServicePartialWithdrawDoc"));
        modelBuilder.Entity<ServicePolicyLoan>(entity => entity.ToTable("ServicePolicyLoan"));
        modelBuilder.Entity<ServicePolicyLoanDoc>(entity => entity.ToTable("ServicePolicyLoanDoc"));
        modelBuilder.Entity<ServicePolicySurrender>(entity => entity.ToTable("ServicePolicySurrender"));
        modelBuilder.Entity<ServicePolicySurrenderDoc>(entity => entity.ToTable("ServicePolicySurrenderDoc"));
        modelBuilder.Entity<ServicePolicyLoan>(entity => entity.ToTable("ServicePolicyLoan"));
        modelBuilder.Entity<ServicePolicyLoanDoc>(entity => entity.ToTable("ServicePolicyLoanDoc"));
        modelBuilder.Entity<ServiceRefundOfPayment>(entity => entity.ToTable("ServiceRefundOfPayment"));
        modelBuilder.Entity<ServiceRefundOfPaymentDoc>(entity => entity.ToTable("ServiceRefundOfPaymentDoc"));
        modelBuilder.Entity<ServicePaymentFrequency>(entity => entity.ToTable("ServicePaymentFrequency"));
        modelBuilder.Entity<ServiceBeneficiary>(entity => entity.ToTable("ServiceBeneficiary"));
        modelBuilder.Entity<ServiceBeneficiaryPersonalInfo>(entity => entity.ToTable("ServiceBeneficiaryPersonalInfo"));
        modelBuilder.Entity<ServiceBeneficiaryShareInfo>(entity => entity.ToTable("ServiceBeneficiaryShareInfo"));
        modelBuilder.Entity<ServiceMain>(entity => entity.ToTable("ServiceMain")
        .ToTable(tb => tb.HasTrigger("service_status_update")));
        modelBuilder.Entity<CrmApiLog>(entity => entity.ToTable("CrmApiLog"));
        modelBuilder.Entity<ServiceStatusUpdate>(entity => entity.ToTable("ServiceStatusUpdate"));
        modelBuilder.Entity<ServicingStatus>(entity => entity.ToTable("servicing_status"));
        modelBuilder.Entity<UsersTemp>(entity => entity.ToTable("users_temp"));
        
        

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.ToTable("Blog");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.BodyEn).HasColumnName("Body_EN");
            entity.Property(e => e.BodyMm).HasColumnName("Body_MM");
            entity.Property(e => e.CategoryType).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.PromotionEnd)
                .HasColumnType("datetime")
                .HasColumnName("Promotion_End");
            entity.Property(e => e.PromotionStart)
                .HasColumnType("datetime")
                .HasColumnName("Promotion_Start");
            entity.Property(e => e.ReadMinEn)
                .HasMaxLength(500)
                .HasColumnName("ReadMin_EN");
            entity.Property(e => e.ReadMinMm)
                .HasMaxLength(500)
                .HasColumnName("ReadMin_MM");
            entity.Property(e => e.TitleEn)
                .HasMaxLength(500)
                .HasColumnName("Title_EN");
            entity.Property(e => e.TitleMm)
                .HasMaxLength(500)
                .HasColumnName("Title_MM");
            entity.Property(e => e.TopicEn)
                .HasMaxLength(500)
                .HasColumnName("Topic_EN");
            entity.Property(e => e.TopicMm)
                .HasMaxLength(500)
                .HasColumnName("Topic_MM");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.Property(e => e.ShareableLink)
                .HasColumnName("ShareableLink");
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity
                .HasKey(e => e.ClaimId);

            //entity
            //    .Property(e => e.ClaimId)
            //    .HasConversion(guidConverter);

            entity
                .ToTable("claims").ToTable(tb => tb.HasTrigger("claim_status_update"));;

            entity.Property(e => e.AccountNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("account_no");
            entity.Property(e => e.BankName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("bank_name");
            entity.Property(e => e.ClaimId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("claim_id");
            entity.Property(e => e.ClaimIdIl)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("claim_id_il");
            entity.Property(e => e.ClaimType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("claim_type");
            entity.Property(e => e.ClaimentClientNo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("claiment_client_no");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("date")
                .HasColumnName("created_date");
            entity.Property(e => e.PolicyNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("policy_no");
            entity.Property(e => e.ProductType)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("product_type");
            entity.Property(e => e.ReceivedDate)
                .HasColumnType("date")
                .HasColumnName("received_date");
            entity.Property(e => e.RejectReason)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reject_reason");
            entity.Property(e => e.Status)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("date")
                .HasColumnName("updated_date");

            entity.Property(e => e.FollowupReason)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("followup_reason");

            entity.HasOne(d => d.ClaimentClientNoNavigation).WithMany()
                .HasForeignKey(d => d.ClaimentClientNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__claims__claiment__42E1EEFE");

            entity.HasOne(d => d.PolicyNoNavigation).WithMany()
                .HasForeignKey(d => d.PolicyNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__claims__policy_n__43D61337");

            entity.HasOne(d => d.ProductTypeNavigation).WithMany()
                .HasForeignKey(d => d.ProductType)
                .HasConstraintName("FK__claims__product___44CA3770");

            entity.HasOne(d => d.StatusNavigation).WithMany()
                .HasForeignKey(d => d.Status)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__claims__status__45BE5BA9");

            //entity
            //.HasOne(p => p.ClaimTran)
            //.WithOne(a => a.Claim)
            //.HasForeignKey<ClaimTran>(a => a.ClaimId);

        });

        modelBuilder.Entity<ClaimStatus>(entity =>
        {
            entity.HasKey(e => e.ShortDesc).HasName("PK__claim_st__DE13CB88EDB9A2FD");

            entity.ToTable("claim_status");

            entity.Property(e => e.ShortDesc)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("short_desc");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(8, 0)")
                .HasColumnName("id");
            entity.Property(e => e.LongDesc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("long_desc");
            entity.Property(e => e.CrmCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("crm_code");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientNo).HasName("PK__clients__BF218C7F3F810D2C");

            entity.ToTable("clients");

            entity.Property(e => e.ClientNo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("client_no");
            entity.Property(e => e.Address1)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("address1");
            entity.Property(e => e.Address2)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("address2");
            entity.Property(e => e.Address3)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("address3");
            entity.Property(e => e.Address4)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("address4");
            entity.Property(e => e.Address5)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("address5");
            entity.Property(e => e.AgentCode)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("agent_code");
            entity.Property(e => e.AgentFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("agent_flag");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("date")
                .HasColumnName("created_date");
            entity.Property(e => e.Dob)
                .HasColumnType("date")
                .HasColumnName("dob");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FatherName)
                .HasMaxLength(75)
                .IsUnicode(false)
                .HasColumnName("father_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.MaritalStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("marital_status");
            entity.Property(e => e.MasterClientNo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("master_client_no");
            entity.Property(e => e.Name)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Nrc)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("nrc");
            entity.Property(e => e.Occupation)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("occupation");
            entity.Property(e => e.Other)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("other");
            entity.Property(e => e.PassportNo)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("passport_no");
            entity.Property(e => e.PhoneNo)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasColumnName("phone_no");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("date")
                .HasColumnName("updated_date");
            entity.Property(e => e.VipEffectiveDate)
                .HasColumnType("date")
                .HasColumnName("vip_effective_date");
            entity.Property(e => e.VipExpiryDate)
                .HasColumnType("date")
                .HasColumnName("vip_expiry_date");
            entity.Property(e => e.VipFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("vip_flag");

            entity.HasOne(d => d.OccupationNavigation).WithMany(p => p.Clients)
                .HasForeignKey(d => d.Occupation)
                .HasConstraintName("FK__clients__occupat__46B27FE2");
        });

        modelBuilder.Entity<Coverage>(entity =>
        {
            entity.ToTable("Coverage");

            entity.Property(e => e.CoverageId)
                .ValueGeneratedNever()
                .HasColumnName("Coverage_ID");
            entity.Property(e => e.CoverageIcon).HasColumnName("Coverage_Icon");
            entity.Property(e => e.CoverageNameEn)
                .HasMaxLength(500)
                .HasColumnName("Coverage_Name_EN");
            entity.Property(e => e.CoverageNameMm)
                .HasMaxLength(500)
                .HasColumnName("Coverage_Name_MM");
            entity.Property(e => e.CreatedBy).HasColumnName("Created_By");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.IsDelete).HasColumnName("Is_Delete");
            entity.Property(e => e.UpdatedBy).HasColumnName("Updated_By");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");
        });

        modelBuilder.Entity<ErrorLogCms>();
        modelBuilder.Entity<ErrorLogMobile>();

        modelBuilder.Entity<Localization>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("Localization");

            entity.Property(e => e.Key)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Burmese)
                .IsUnicode(true);
            entity.Property(e => e.English)
                .IsUnicode(false);

            entity.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Member");

            entity.Property(e => e.MemberId)
                .ValueGeneratedNever()
                .HasColumnName("Member_ID");
            entity.Property(e => e.Auth0Userid).HasColumnName("auth0_userid");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(500);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.LastActiveDate)
                .HasColumnType("datetime")
                .HasColumnName("Last_Active_Date");
            entity.Property(e => e.Mobile).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.Nrc)
                .HasMaxLength(500)
                .HasColumnName("NRC");
            entity.Property(e => e.Others).HasMaxLength(500);
            entity.Property(e => e.Passport).HasMaxLength(500);
            entity.Property(e => e.RegisterDate)
                .HasColumnType("datetime")
                .HasColumnName("Register_Date");
            entity.Property(e => e.RegisterDate)
                .HasColumnType("datetime")
                .HasColumnName("Register_Date");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");
            entity.Property(e => e.ProfileImage)
                .HasColumnName("Profile_Image")
                .HasMaxLength(255);
            entity.Property(e => e.IsVerified).HasColumnName("Is_Verified");
            entity.Property(e => e.OktaUserName).HasColumnName("Okta_UserName");
            entity.Property(e => e.OtpToken).HasColumnName("Otp_Token");
            entity.Property(e => e.OtpExpiry).HasColumnName("Otp_Expiry").HasColumnType("datetime");
            entity.Property(e => e.OtpCode).HasColumnName("Otp_Code");
            entity.Property(e => e.OtpType).HasColumnName("Otp_Type");
            entity.Property(e => e.OtpTo).HasColumnName("Otp_To");
            entity.Property(e => e.IsEmailVerified).HasColumnName("Is_Email_Verified");
            entity.Property(e => e.IsMobileVerified).HasColumnName("Is_Mobile_Verified");
        });

        modelBuilder.Entity<MemberClient>(entity =>
        {
            entity.ToTable("Member_Clients");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.ClientNo)
                .IsRequired()
                .HasMaxLength(8)
                .HasColumnName("client_no");
            entity.Property(e => e.MemberId)
                .IsRequired()
                .HasColumnName("Member_ID");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberClients)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Member_Clients_Member");

            entity.HasOne(d => d.Client).WithOne(p => p.MemberClient)
                  .HasForeignKey<MemberClient>(d => d.ClientNo);
        });

        modelBuilder.Entity<MemberSession>(entity =>
        {
            entity.ToTable("MemberSession");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.Auth0Userid).HasColumnName("auth0_userid");
            entity.Property(e => e.MemberId).HasColumnName("MemberID");
            entity.Property(e => e.SessionId).HasColumnName("SessionID");
        });

        modelBuilder.Entity<Occupation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__occupati__357D4CF81C1883B1");

            entity.ToTable("occupation");

            entity.Property(e => e.Code)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(8, 0)")
                .HasColumnName("id");
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.PolicyNo).HasName("PK__policies__47DA175095C08A02");

            entity.ToTable("policies");

            entity.Property(e => e.PolicyNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("policy_no");
            entity.Property(e => e.AcpModeFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("acp_mode_flag");
            entity.Property(e => e.AgentCode)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("agent_code");
            entity.Property(e => e.AnnualizedPremium)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("annualized_premium");
            entity.Property(e => e.Components)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("components");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("date")
                .HasColumnName("created_date");
            entity.Property(e => e.FirstIssueDate)
                .HasColumnType("date")
                .HasColumnName("first_issue_date");
            entity.Property(e => e.InstallmentPremium)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("installment_premium");
            entity.Property(e => e.InsuredPersonClientNo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("insured_person_client_no");
            entity.Property(e => e.NumberOfUnit)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("number_of_unit");
            entity.Property(e => e.OriginalCommencementDate)
                .HasColumnType("date")
                .HasColumnName("original_commencement_date");
            entity.Property(e => e.OutstandingInterest)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("outstanding_interest");
            entity.Property(e => e.OutstandingPremium)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("outstanding_premium");
            entity.Property(e => e.PaidToDate)
                .HasColumnType("date")
                .HasColumnName("paid_to_date");
            entity.Property(e => e.PaymentFrequency)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("payment_frequency");
            entity.Property(e => e.PolicyHolderClientNo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("policy_holder_client_no");
            entity.Property(e => e.PolicyIssueDate)
                .HasColumnType("date")
                .HasColumnName("policy_issue_date");
            entity.Property(e => e.PolicyStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("policy_status");
            entity.Property(e => e.PolicyTerm)
                .HasColumnName("policy_term");
            entity.Property(e => e.PremiumDue)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("premium_due");
            entity.Property(e => e.PremiumStatus)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("premium_status");
            entity.Property(e => e.PremiumTerm)
                .HasColumnName("premium_term");
            entity.Property(e => e.ProductType)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("product_type");
            entity.Property(e => e.RiskCommencementDate)
                .HasColumnType("date")
                .HasColumnName("risk_commencement_date");
            entity.Property(e => e.SumAssured)
                .HasColumnType("numeric(15, 2)")
                .HasColumnName("sum_assured");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("date")
                .HasColumnName("updated_date");

            entity.Property(e => e.PolicyExpiryDate)
                .HasColumnType("date")
                .HasColumnName("policy_expiry_date");

            entity.Property(e => e.PolicyLapsedDate)
                .HasColumnType("date")
                .HasColumnName("policy_lapsed_date");

            entity.HasOne(d => d.InsuredPersonClientNoNavigation).WithMany(p => p.PolicyInsured)
                .HasForeignKey(d => d.InsuredPersonClientNo)
                .HasConstraintName("FK__policies__insure__47A6A41B");

            entity.HasOne(d => d.PolicyHolderClientNoNavigation).WithMany(p => p.PolicyHolder)
                .HasForeignKey(d => d.PolicyHolderClientNo)
                .HasConstraintName("FK__policies__policy__489AC854");

            entity.HasOne(d => d.PolicyStatusNavigation).WithMany(p => p.Policies)
                .HasForeignKey(d => d.PolicyStatus)
                .HasConstraintName("FK__policies__policy__498EEC8D");

            entity.HasOne(d => d.PremiumStatusNavigation).WithMany(p => p.Policies)
                .HasForeignKey(d => d.PremiumStatus)
                .HasConstraintName("FK__policies__premiu__4A8310C6");

            entity.HasOne(d => d.ProductTypeNavigation).WithMany(p => p.Policies)
                .HasForeignKey(d => d.ProductType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__policies__produc__4B7734FF");
        });

        modelBuilder.Entity<PolicyStatus>(entity =>
        {
            entity.HasKey(e => e.ShortDesc).HasName("PK__policy_s__DE13CB882AFFF0DB");

            entity.ToTable("policy_status");

            entity.Property(e => e.ShortDesc)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("short_desc");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(8, 0)")
                .HasColumnName("id");
            entity.Property(e => e.LongDesc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("long_desc");
        });

        modelBuilder.Entity<PremiumStatus>(entity =>
        {
            entity.HasKey(e => e.ShortDesc).HasName("PK__premium___DE13CB88C6212390");

            entity.ToTable("premium_status");

            entity.Property(e => e.ShortDesc)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("short_desc");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(8, 0)")
                .HasColumnName("id");
            entity.Property(e => e.LongDesc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("long_desc");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");

            entity.Property(e => e.ProductId)
                .ValueGeneratedNever()
                .HasColumnName("Product_ID");
            entity.Property(e => e.Brochure).HasMaxLength(500);
            entity.Property(e => e.CoverImage)
                .HasMaxLength(500)
                .HasColumnName("Cover_Image");
            entity.Property(e => e.CreatedBy).HasColumnName("Created_By");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.CreditingLink)
                .HasMaxLength(500)
                .HasColumnName("Crediting_Link");
            entity.Property(e => e.IntroEn)
                .HasMaxLength(500)
                .HasColumnName("Intro_EN");
            entity.Property(e => e.IntroMm)
                .HasMaxLength(500)
                .HasColumnName("Intro_MM");
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.IsDelete).HasColumnName("Is_Delete");
            entity.Property(e => e.NotAllowedInProductList).HasColumnName("NotAllowedInProductList");
            entity.Property(e => e.IssuedAgeFrom)
                .HasMaxLength(50)
                .HasColumnName("Issued_Age_From");
            entity.Property(e => e.IssuedAgeTo)
                .HasMaxLength(50)
                .HasColumnName("Issued_Age_To");
            entity.Property(e => e.LogoImage)
                .HasMaxLength(500)
                .HasColumnName("Logo_Image");
            entity.Property(e => e.PolicyTermUpToEn)
                .HasMaxLength(50)
                .HasColumnName("Policy_Term_Up_To_EN");
            entity.Property(e => e.PolicyTermUpToMm)
                .HasMaxLength(50)
                .HasColumnName("Policy_Term_Up_To_MM");
            entity.Property(e => e.ProductTypeId)
                .HasColumnType("numeric(8, 0)")
                .HasColumnName("Product_Type_ID");
            entity.Property(e => e.ProductTypeShort)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Product_Type_Short");
            entity.Property(e => e.ShortEn)
                .HasMaxLength(500)
                .HasColumnName("Short_EN");
            entity.Property(e => e.ShortMm)
                .HasMaxLength(500)
                .HasColumnName("Short_MM");
            entity.Property(e => e.TaglineEn)
                .HasMaxLength(500)
                .HasColumnName("Tagline_EN");
            entity.Property(e => e.TaglineMm)
                .HasMaxLength(500)
                .HasColumnName("Tagline_MM");
            entity.Property(e => e.TitleEn)
                .HasMaxLength(500)
                .HasColumnName("Title_EN");
            entity.Property(e => e.TitleMm)
                .HasMaxLength(500)
                .HasColumnName("Title_MM");
            entity.Property(e => e.UpdatedBy).HasColumnName("Updated_By");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");
            entity.Property(e => e.WebsiteLink)
                .HasMaxLength(500)
                .HasColumnName("Website_Link");
            entity.Property(e => e.IssuedAgeFromMm).HasColumnName("Issued_Age_From_MM");
            entity.Property(e => e.IssuedAgeToMm).HasColumnName("Issued_Age_To_MM");
        });

        modelBuilder.Entity<ProductBenefit>(entity =>
        {
            entity.ToTable("Product_Benefit");

            entity.Property(e => e.ProductBenefitId)
                .ValueGeneratedNever()
                .HasColumnName("Product_Benefit_ID");
            entity.Property(e => e.CreatedBy).HasColumnName("Created_By");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.DescriptionEn)
                .HasMaxLength(500)
                .HasColumnName("Description_EN");
            entity.Property(e => e.DescriptionMm)
                .HasMaxLength(500)
                .HasColumnName("Description_MM");
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.IsDelete).HasColumnName("Is_Delete");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.TitleEn)
                .HasMaxLength(500)
                .HasColumnName("Title_EN");
            entity.Property(e => e.TitleMm)
                .HasMaxLength(500)
                .HasColumnName("Title_MM");
            entity.Property(e => e.UpdatedBy).HasColumnName("Updated_By");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductBenefits)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Product_Benefit_Product");
        });

        modelBuilder.Entity<ProductCoverage>(entity =>
        {
            entity.ToTable("Product_Coverage");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CoverageId).HasColumnName("Coverage_ID");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");

            entity.HasOne(d => d.Coverage).WithMany(p => p.ProductCoverages)
                .HasForeignKey(d => d.CoverageId)
                .HasConstraintName("FK_Product_Coverage_Coverage");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductCoverages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Product_Coverage_Product");
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.HasKey(e => e.ShortDesc).HasName("PK__product___DE13CB88D9FA055F");

            entity.ToTable("product_type");

            entity.Property(e => e.ShortDesc)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("short_desc");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(8, 0)")
                .HasColumnName("id");
            entity.Property(e => e.LongDesc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("long_desc");
        });

        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.ToTable("PromotionProduct");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.BlogId).HasColumnName("BlogID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Blog).WithMany(p => p.PromotionProducts)
                .HasForeignKey(d => d.BlogId)
                .HasConstraintName("FK_PromotionProduct_Blog");
        });

        modelBuilder.Entity<Proposition>(entity =>
        {
            entity.ToTable("Proposition");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasColumnName("Created_By");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.DescriptionEn).HasColumnName("Description_EN");
            entity.Property(e => e.DescriptionMm).HasColumnName("Description_MM");
            entity.Property(e => e.Eligibility).HasMaxLength(50);
            entity.Property(e => e.HotlineButtonTextEn)
                .HasMaxLength(500)
                .HasColumnName("HotlineButtonText_EN");
            entity.Property(e => e.HotlineButtonTextMm)
                .HasMaxLength(500)
                .HasColumnName("HotlineButtonText_MM");
            entity.Property(e => e.HotlineNumber).HasMaxLength(500);
            entity.Property(e => e.HotlineType).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.IsDelete).HasColumnName("Is_Delete");
            entity.Property(e => e.NameEn)
                .HasMaxLength(500)
                .HasColumnName("Name_EN");
            entity.Property(e => e.NameMm)
                .HasMaxLength(500)
                .HasColumnName("Name_MM");
            entity.Property(e => e.PartnerFacebookUrl).HasMaxLength(500);
            entity.Property(e => e.PartnerInstagramUrl).HasMaxLength(500);
            entity.Property(e => e.PartnerPhoneNumber).HasMaxLength(500);
            entity.Property(e => e.PartnerTwitterUrl).HasMaxLength(500);
            entity.Property(e => e.PartnerWebsiteLink).HasMaxLength(500);
            entity.Property(e => e.PropositionCategoryId).HasColumnName("PropositionCategoryID");
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasColumnName("Updated_By");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");

            entity.HasOne(d => d.PropositionCategory).WithMany(p => p.Propositions)
                .HasForeignKey(d => d.PropositionCategoryId)
                .HasConstraintName("FK_Proposition_PropositionCategory");
            entity.Property(e => e.AddressLabel).HasColumnName("Address_Label").HasMaxLength(500);
        });

        modelBuilder.Entity<PropositionAddress>(entity =>
        {
            entity.ToTable("PropositionAddress");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.AddressEn).HasColumnName("Address_EN");
            entity.Property(e => e.AddressMm).HasColumnName("Address_MM");
            entity.Property(e => e.NameEn)
                .HasMaxLength(500)
                .HasColumnName("Name_EN");
            entity.Property(e => e.NameMm)
                .HasMaxLength(500)
                .HasColumnName("Name_MM");
            entity.Property(e => e.PhoneNumberEn)
                .HasMaxLength(500)
                .HasColumnName("PhoneNumber_EN");
            entity.Property(e => e.PhoneNumberMm)
                .HasMaxLength(500)
                .HasColumnName("PhoneNumber_MM");
            entity.Property(e => e.PropositionId).HasColumnName("PropositionID");

            entity.HasOne(d => d.Proposition).WithMany(p => p.PropositionAddresses)
                .HasForeignKey(d => d.PropositionId)
                .HasConstraintName("FK_PropositionAddress_Proposition");
        });

        modelBuilder.Entity<PropositionBenefit>(entity =>
        {
            entity.ToTable("PropositionBenefit");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.GroupNameEn)
                .HasMaxLength(500)
                .HasColumnName("GroupName_EN");
            entity.Property(e => e.GroupNameMm)
                .HasMaxLength(500)
                .HasColumnName("GroupName_MM");
            entity.Property(e => e.NameEn).HasColumnName("Name_EN");
            entity.Property(e => e.NameMm).HasColumnName("Name_MM");
            entity.Property(e => e.PropositionId).HasColumnName("PropositionID");
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Proposition).WithMany(p => p.PropositionBenefits)
                .HasForeignKey(d => d.PropositionId)
                .HasConstraintName("FK_PropositionBenefit_Proposition");
        });

        modelBuilder.Entity<PropositionBranch>(entity =>
        {
            entity.ToTable("PropositionBranch");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.NameEn).HasColumnName("Name_EN");
            entity.Property(e => e.NameMm).HasColumnName("Name_MM");
            entity.Property(e => e.PropositionId).HasColumnName("PropositionID");

            entity.HasOne(d => d.Proposition).WithMany(p => p.PropositionBranches)
                .HasForeignKey(d => d.PropositionId)
                .HasConstraintName("FK_PropositionBranch_Proposition");
        });

        modelBuilder.Entity<PropositionCategory>(entity =>
        {
            entity.ToTable("PropositionCategory");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.BackgroundImage).HasMaxLength(500);
            entity.Property(e => e.IconImage).HasMaxLength(500);
            entity.Property(e => e.NameEn)
                .HasMaxLength(500)
                .HasColumnName("Name_EN");
            entity.Property(e => e.NameMm)
                .HasMaxLength(500)
                .HasColumnName("Name_MM");
            entity.Property(e => e.IsAiaBenefitCategory).HasColumnName("IsAiaBenefitCategory");
            entity.Property(e => e.IsDelete).HasColumnName("Is_Delete");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.Permissions).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Created_Date");
            entity.Property(e => e.Email).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasColumnName("Is_Active");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(250)
                .HasColumnName("Password_Hash");
            entity.Property(e => e.PasswordSalt)
                .HasMaxLength(250)
                .HasColumnName("Password_Salt");
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("Updated_Date");

            entity.HasOne(d => d.Role).WithMany(p => p.Staff)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Staffs__Role_ID__681373AD");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
