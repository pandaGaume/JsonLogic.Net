using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueForest.JsonLogic
{
    public class DelegateJsonConverter<T> : JsonConverter<Delegate>
    {
        public override Delegate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonLogic.Parse<bool>(ref reader, typeof(T)).Compile();

        public override void Write(Utf8JsonWriter writer, Delegate value, JsonSerializerOptions options) => JsonLogic.Assemble(writer, value, options);
    }
}
