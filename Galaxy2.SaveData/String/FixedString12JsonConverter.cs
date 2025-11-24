using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Galaxy2.SaveData.String
{
    public class FixedString12JsonConverter : JsonConverter<FixedString12>
    {
        public override FixedString12 ReadJson(JsonReader reader, Type objectType, FixedString12 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = JToken.Load(reader).ToString();
            return new FixedString12(s);
        }

        public override void WriteJson(JsonWriter writer, FixedString12 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}

