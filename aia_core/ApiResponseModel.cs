using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace aia_core
{
    /// <summary>
    /// aia api response model
    /// </summary>
    /// <typeparam name="T"></typeparam>

    public class ResponseModel<T>
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class ResponseModel
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; } = null;
    }
}
