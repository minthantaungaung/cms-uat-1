using aia_core.Entities;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Response
{
    public class PropositionRequestModelResponse
    {
        public Guid? ID { get; set; }
        public string MemberCode {get;set;}
        public string MemberType {get;set;}
        public string MemberRole {get;set;}
        public string CategoryName {get;set;}
        public string PartnerName { get; set; }
        public string AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string? AppointmentSpecialist { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string BranchName { get; set; }
        public string MemberEmail { get; set; }
        public string MemberPhone { get; set; }
        public string MemberName { get; set; }
        public List<string> Benefits { get; set; }
        public PropositionRequestModelResponse(Entities.PropositionRequest entity, Func<EnumFileType, string, string> blobUrl)
        {
            ID = entity.ID;
            MemberCode = entity.ClientNo;
            MemberType = entity.MemberType;
            MemberRole = entity.MemberRole;
            CategoryName = entity.Proposition?.PropositionCategory!=null?entity.Proposition?.PropositionCategory?.NameEn:"";
            PartnerName = entity.Proposition.NameEn;
            AppointmentDate = entity.AppointmentDate.ToString("dd MMM yyyy");
            AppointmentTime = entity.AppointmentDate.ToString("hh:mm tt");
            AppointmentSpecialist = entity.AppointmentSpecialist;
            SubmissionDate = entity.SubmissionDate;
            BranchName =  entity.PropositionBranches !=null ? entity.PropositionBranches.NameEn: "";
            MemberEmail = entity.Member?.Email;
            MemberPhone = entity.Member?.Mobile;
            MemberName = entity.Member?.Name;
            Benefits = entity.Benefits?.Split('|').ToList();
        }
    }


    public class PartnerItemResponse
    {
        public Guid? PartnerId { get; set; }
        public string? PartnerName { get; set; }
    }

    }
