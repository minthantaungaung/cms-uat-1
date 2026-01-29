using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.OcrApi
{
    public class Detection
    {
        public float x1 { get; set; }
        public float y1 { get; set; }
        public float x2 { get; set; }
        public float y2 { get; set; }
        public float confidence { get; set; }
    }

    public class Page
    {
        public int page_number { get; set; }
        public bool blur_warning { get; set; }
        public float blur_variance { get; set; }
        public int number_of_bills { get; set; }
        public bool detection_passed { get; set; }
        public List<Detection> detections { get; set; }
    }

    public class AiaValidateDocApiResponseModel
    {
        public string file_type { get; set; }
        public List<Page> pages { get; set; }
    }
}

