// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> that writes read-only dictionaries with complex keys exactly as
/// <see cref="Cratis.Json.ComplexKeyDictionaryJsonConverterFactory"/> writes the dictionaries that can be changed: each key as its own JSON text, used as the property name.
/// </summary>
/// <remarks>
/// The identity of a definition is a hash of the text written for it, so the text of a definition must not change when its dictionaries became read-only.
/// </remarks>
internal sealed class ReadOnlyComplexKeyDictionaryJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) &&
        IsComplexType(typeToConvert.GetGenericArguments()[0]);

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var arguments = typeToConvert.GetGenericArguments();
        return (JsonConverter)Activator.CreateInstance(typeof(Converter<,>).MakeGenericType(arguments[0], arguments[1]))!;
    }

    static bool IsComplexType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return !type.IsPrimitive &&
               !type.IsEnum &&
               type != typeof(string) &&
               type != typeof(Guid) &&
               type != typeof(DateTime) &&
               type != typeof(DateTimeOffset) &&
               type != typeof(DateOnly) &&
               type != typeof(TimeOnly) &&
               type != typeof(TimeSpan) &&
               type != typeof(decimal) &&
               type != typeof(Uri);
    }

    sealed class Converter<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
        where TKey : notnull
    {
        public override IReadOnlyDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);
            var dictionary = new Dictionary<TKey, TValue>();
            var keyOptions = new JsonSerializerOptions(options) { WriteIndented = false };
            foreach (var property in document.RootElement.EnumerateObject())
            {
                dictionary[JsonSerializer.Deserialize<TKey>(property.Name, keyOptions)!] = property.Value.Deserialize<TValue>(options)!;
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            var keyOptions = new JsonSerializerOptions(options) { WriteIndented = false };
            foreach (var (key, item) in value)
            {
                writer.WritePropertyName(JsonSerializer.Serialize(key, keyOptions));
                JsonSerializer.Serialize(writer, item, options);
            }

            writer.WriteEndObject();
        }
    }
}
