
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;

namespace aia_core.Converter
{
    public class JsonStringEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(EnumDeviceType) 
            || t == typeof(EnumIdenType)
            || t == typeof(EnumIndividualMemberType)
            || t == typeof(EnumAuthorizationType)
            ;

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            if (t == typeof(EnumDeviceType))
            {
                return Enum.Parse(typeof(EnumDeviceType), value);
            }
            if (t == typeof(EnumIdenType))
            {
                return Enum.Parse(typeof(EnumIdenType), value);
            }
            if (t == typeof(EnumIndividualMemberType))
            {
                return Enum.Parse(typeof(EnumIndividualMemberType), value);
            }
            if (t == typeof(EnumAuthorizationType))
            {
                return Enum.Parse(typeof(EnumAuthorizationType), value);
            }

            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            else if (untypedValue.GetType() == typeof(EnumDeviceType)) {
                serializer.Serialize(writer, ((EnumDeviceType)untypedValue).ToString());
                return;
            }
            else if (untypedValue.GetType() == typeof(EnumIdenType))
            {
                serializer.Serialize(writer, ((EnumIdenType)untypedValue).ToString());
                return;
            }
            else if (untypedValue.GetType() == typeof(EnumIndividualMemberType))
            {
                serializer.Serialize(writer, ((EnumIndividualMemberType)untypedValue).ToString());
                return;
            }
            else if (untypedValue.GetType() == typeof(EnumAuthorizationType))
            {
                serializer.Serialize(writer, ((EnumAuthorizationType)untypedValue).ToString());
                return;
            }
        }
    }
}
