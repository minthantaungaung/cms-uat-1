using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Mobile.Request
{
    public class PropositionRequestModel
    {
      public Guid PropositionID { get; set; }
      public DateTime AppointmentDate { get; set; }
      public string? AppointmentSpecialist { get; set; }
      public Guid? BranchID { get; set; }
      public List<string> Benefits { get; set; }
    }
}
