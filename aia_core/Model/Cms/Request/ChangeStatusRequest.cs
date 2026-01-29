using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class ChangeStatusRequest
    {
        public Guid? ID { get; set; }
        public bool IsActive { get; set; }
    }
}