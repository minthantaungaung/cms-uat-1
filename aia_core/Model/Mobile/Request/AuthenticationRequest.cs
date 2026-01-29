using aia_core.Converter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request
{
    
    public class LoginRequest
    {
        [Required]
        public string? PushToken { get; set; }

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EnumDeviceType DeviceType { get; set; }

    }

    public class DeviceRequest : LoginRequest
    {
        public string? MemberId { get; set; }
    }

}
