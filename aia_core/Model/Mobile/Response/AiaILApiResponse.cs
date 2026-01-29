using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.AiaILApiResponse
{
    public class Data
    {
        public string status { get; set; }
        public string errorMessage { get; set; }
    }

    public class Message
    {
        public string type { get; set; }
        public string code { get; set; }
        public string text { get; set; }
        public string receivedTime { get; set; }
        public string completedTime { get; set; }
        public List<object> errors { get; set; }
    }


    #region #TPD
    public class TPDRegisterResponse
    {
        public Message message { get; set; }
        public Data data { get; set; }
    }
    #endregion


    #region CommonResponse
    public class CommonRegisterResponse
    {
        public Message message { get; set; }
        public Data data { get; set; }
    }
    #endregion
}
