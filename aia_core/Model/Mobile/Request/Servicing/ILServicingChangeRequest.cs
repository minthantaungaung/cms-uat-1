using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{
    public class ILServicingChangeRequest
    {
        public ILServicingClientInfo client { get; set; }
        public string requestType { get; set; }
        public string policyNumber { get; set; }
        public string townshipCode { get; set; }
    }

    public class ILServicingClientInfo
    {
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string address5 { get; set; }
        public string clientNumber { get; set; }
        public string country { get; set; }
        public DateTime dob { get; set; }
        public string email { get; set; }
        public string fathersName { get; set; }
        public string gender { get; set; }
        public string idnumber { get; set; }
        public string idtype { get; set; }
        public string maritalStatus { get; set; }
        public string name { get; set; }
        public string occupation { get; set; }
        public string phone { get; set; }
        public string updateClientLevel { get; set; }
    }
}