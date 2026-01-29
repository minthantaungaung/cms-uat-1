using System.ComponentModel.DataAnnotations;
using aia_core.Services;

namespace aia_core.Model.Cms.Response.Holiday
{
    public class HolidayResponse
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public DateTime HolidayDate { get; set; }

        public HolidayResponse() { }
        public HolidayResponse(Entities.Holiday entity) 
        {
            ID = entity.ID;
            Name = entity.Name;
            HolidayDate = entity.HolidayDate;
        }
    }
}