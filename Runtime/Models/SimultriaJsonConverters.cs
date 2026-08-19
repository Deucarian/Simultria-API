using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Deucarian.Simultria.API.Models
{
    internal sealed class SimultriaFlexibleStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null ||
                reader.TokenType == JsonToken.Undefined)
            {
                return null;
            }

            return Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            writer.WriteValue(value as string);
        }
    }

    internal sealed class SimultriaModelVersionReferenceConverter :
        JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SimultriaModelVersionDto);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null ||
                reader.TokenType == JsonToken.Undefined)
            {
                return null;
            }

            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Object)
            {
                var value = new SimultriaModelVersionDto();
                using (JsonReader tokenReader = token.CreateReader())
                {
                    serializer.Populate(tokenReader, value);
                }

                return value;
            }

            if (int.TryParse(
                    token.ToString(Formatting.None).Trim('"'),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int id))
            {
                return new SimultriaModelVersionDto { Id = id };
            }

            return null;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
