// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aksio.Cratis.Strings;

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for converting types that are adorned with the <see cref="DerivedTypeAttribute"/>.
/// </summary>
/// <typeparam name="T">The interface the derived type implements to convert.</typeparam>
public class DerivedTypeJsonConverter<T> : JsonConverter<T>
{
    /// <summary>
    /// The property used in JSON to identify the derived type id.
    /// </summary>
    public const string DerivedTypeIdProperty = "_derivedTypeId";

    readonly IDerivedTypes _derivedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedTypeJsonConverter{T}"/> class.
    /// </summary>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> to use for discovering correct type.</param>
    public DerivedTypeJsonConverter(IDerivedTypes derivedTypes)
    {
        _derivedTypes = derivedTypes;
    }

    /// <inheritdoc/>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var document = JsonDocument.ParseValue(ref reader);

        if (document.RootElement.TryGetProperty(DerivedTypeIdProperty, out var value))
        {
            var derivedTypeId = (DerivedTypeId)value.GetGuid();
            var derivedType = _derivedTypes.GetDerivedTypeFor(typeToConvert, derivedTypeId);
            var instance = document.Deserialize(derivedType, options);
            return (T)instance!;
        }
        return default!;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                JsonSerializer.Serialize(writer, null!, options);
                break;

            default:
                var actualValue = new ExpandoObject();
                var actualValueAsDictionary = actualValue as IDictionary<string, object>;
                var type = value.GetType();

                foreach (var property in type.GetProperties())
                {
                    actualValueAsDictionary[property.Name.ToCamelCase()] = property.GetValue(value)!;
                }

                var derivedTypeAttribute = type.GetCustomAttribute<DerivedTypeAttribute>();
                if (derivedTypeAttribute is not null)
                {
                    actualValueAsDictionary[DerivedTypeIdProperty] = derivedTypeAttribute.Identifier.ToString();
                }

                JsonSerializer.Serialize(writer, actualValueAsDictionary, actualValueAsDictionary.GetType(), options);
                break;
        }
    }
}
