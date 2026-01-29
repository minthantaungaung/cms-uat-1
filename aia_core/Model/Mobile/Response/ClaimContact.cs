using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response
{
    public class ClaimContact
    {
        public DateTime AppliedDate { get; set; }
        public List<DateTime>? HolidayList { get; set; } = new List<DateTime>();
        public DateTime CurrentDate { get; set; }

        public DateTime CompletedDate { get; set; }

        public int Percent { get; set; }
        public string Hours { get; set; }
    }
}
