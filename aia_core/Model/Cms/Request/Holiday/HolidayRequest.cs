using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.Holiday
{
    public class HolidayRequest
    {
        public string Name { get; set; }
        public DateTime HolidayDate { get; set; }
    }

    public class CreateHolidayRequest: HolidayRequest
    {
        
    }

    public class UpdateHolidayRequest : HolidayRequest
    {
        public Guid ID { get; set; }
    }
}