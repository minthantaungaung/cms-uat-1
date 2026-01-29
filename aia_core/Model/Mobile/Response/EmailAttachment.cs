using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response
{
    public class EmailAttachment
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string Format { get; set; }
    }
}
