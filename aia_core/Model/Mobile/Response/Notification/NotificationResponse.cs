using aia_core.Entities;
using aia_core.UnitOfWork;
using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Notification
{
    public class NotificationCount
    {
        public long SelectCount { get; set; }
    }

    public class NotiUnreadCount
    {
        public int UnreadCount { get; set; }
    }

    public class ExtendedPagedList<T> : PagedList<T>
    {
        public int UnreadCount { get; }

        public ExtendedPagedList(IEnumerable<T> source, int pageIndex, int pageSize, int unreadCount)
            : base(source, pageIndex, pageSize)
        {
            this.UnreadCount = unreadCount;
        }

        public ExtendedPagedList(IEnumerable<T> source, long totalCount, int pageNumber, int pageSize, int unreadCount)
            : base(source, totalCount, pageNumber, pageSize)
        {
            this.UnreadCount = unreadCount;
        }
    }

    public class NotificationResponse
    {
        public Guid? Id { get; set; }

        public string? Message { get; set; } = null!;

        public string? MessageMm { get; set; }

        [JsonIgnore]
        public string? TitleEn { get; set; }

        [JsonIgnore]
        public string? TitleMm { get; set; }

        public string? Icon { get; set; } = null!;

        public string? Type { get; set; } = null!;

        public DateTime? CreatedDate { get; set; }

        public bool? IsRead { get; set; }

        public string? Status { get; set; }

        public bool? IsSytemNoti { get; set; }

        public string? SystemNotiType { get; set; }
        public string? ProductId { get; set; }
        public string? PromotionId { get; set; }
        public string? PropositionId { get; set; }
        public string? CommonKeyId { get; set; }
        public string? PremiumPolicyNo { get; set; }

        public ClaimData? ClaimData { get; set; }

        [JsonIgnore]
        public string? ClaimId { get; set; }

        [JsonIgnore]
        public string? InsuredId { get; set; }
        
        public string? PolicyNumber { get; set; }

        public ServicingData? ServicingData { get; set; }

        [JsonIgnore]
        public string? ServicingId { get; set; }

        [JsonIgnore]
        public string? ServiceType { get; set; }

        [JsonIgnore]
        public string? ServiceStatus { get; set; }

        public NotificationResponse() { }
        public NotificationResponse(Entities.MemberNotification entity) 
        { 
            Id = entity.Id;
            Message = entity.Message;
            Type = entity.Type;
            CreatedDate = entity.CreatedDate;
            IsRead = entity.IsRead;

            if (entity.Type == EnumNotificationType.Claim.ToString())
            {
                

                var claimStatus = "";
                switch (entity.Claim.Status)
                {
                    case "RC": claimStatus = EnumClaimStatusDesc.Received.ToString(); break;
                    case "AL": claimStatus = EnumClaimStatusDesc.Approved.ToString(); break;
                    case "FU": claimStatus = EnumClaimStatusDesc.Followedup.ToString(); break;
                    case "WD": claimStatus = EnumClaimStatusDesc.Withdrawn.ToString(); break;
                    case "CS": claimStatus = EnumClaimStatusDesc.Closed.ToString(); break;
                    case "PD": claimStatus = EnumClaimStatusDesc.Paid.ToString(); break;
                    case "RJ": claimStatus = EnumClaimStatusDesc.Rejected.ToString(); break;
                }

                Status = claimStatus;

                ClaimData = new ClaimData()
                {
                    ClaimId = entity.ClaimId,
                    InsuredId = entity.Claim.ClaimentClientNo,
                    PolicyNumber = entity.Claim.PolicyNo,
                };
            }


            IsSytemNoti = entity.IsSytemNoti;
            SystemNotiType = entity.SystemNotiType;
            ProductId = entity.ProductId;
            PromotionId = entity.PromotionId;
            PropositionId = entity.PropositionId;


            if (entity.Type == EnumNotificationType.Service.ToString())
            {
                ServicingData = new ServicingData
                {
                    ServicingId = entity?.ServicingId,
                    ServiceType = (EnumServiceType)Enum.Parse(typeof(EnumServiceType), entity.ServiceType),
                    ServiceStatus = entity.ServiceStatus,
                };
            }

            CommonKeyId = entity.CommonKeyId;
            PremiumPolicyNo = entity.PremiumPolicyNo;

        }
    }

    public class ClaimData
    {
        public string? ClaimId { get; set; }

        public string? InsuredId { get; set; }

        public string? PolicyNumber { get; set; }
    }


    public class ServicingData
    {
        public string? ServicingId { get; set; }
        public EnumServiceType? ServiceType { get; set; }

        public string? ServiceStatus  { get; set; }
    }

    public class NotiStatus
    {
        public string? StatusCode { get; set; }

        public string? StatusDesc { get; set; }

        public string? NotiType { get; set; }
    }


    public class NotiType
    {
        public string? NotiTypeCode { get; set; }

        public string? NotiTypeDesc { get; set; }
    }

    public class NotiTypeAndStatusResponse
    {
        public List<NotiType>? NotiTypes { get; set; }

        public List<NotiStatus>? NotiStatuses { get; set; }
    }


    public class NotiCommonDetailResponse
    {
        public Guid? Id { get; set; }

        public string? TitleEn { get; set; }

        public string? TitleMm { get; set; }
        public string? MessageEn { get; set; }

        public string? MessageMm { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
    }
}
