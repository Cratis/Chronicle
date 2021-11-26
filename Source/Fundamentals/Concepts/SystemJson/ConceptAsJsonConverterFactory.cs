using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Concepts.SystemJson
{
    /// <summary>
    /// Represents a <see cref="JsonConverterFactory"/> for providing <see cref="ConceptAsJsonConverter{T}"/> for concept types.
    /// </summary>
    public class ConceptAsJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsConcept();

        /// <inheritdoc/>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(ConceptAsJsonConverter<>).MakeGenericType(typeToConvert);
            return (Activator.CreateInstance(converterType) as JsonConverter)!;
        }
    }
}
