using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace aia_core.Entities;

public partial class PropositionRequest
{
      public Guid ID { get; set; }
      public Guid PropositionID { get; set; }
      public Guid? MemberID { get; set; }
      public string? ClientNo { get; set; }
      public string? MemberRole { get; set; }
      public string? MemberType { get; set; }
      public DateTime AppointmentDate { get; set; }
      public string? AppointmentSpecialist { get; set; }
      public DateTime SubmissionDate { get; set; }
      public Guid? BranchID { get; set; }
      public string? Benefits { get; set; }

      public virtual Proposition? Proposition { get; set; }
      public virtual Member? Member { get; set; }
      [ForeignKey("BranchID")]
      public virtual PropositionBranch? PropositionBranches { get; set; }
}
