// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for <see cref="ConceptAs{T}"/>.
/// </summary>
/// <typeparam name="T">Underlying concept type.</typeparam>
public class ConceptAsJsonConverter<T> : JsonConverter<T>
{
    /// <inheritdoc/>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        object? value = null;
        var conceptValueType = typeof(T).GetConceptValueType();
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
            case JsonTokenType.False:
                value = reader.GetBoolean();
                break;

            case JsonTokenType.Number:
                if (conceptValueType == typeof(sbyte))
                {
                    value = reader.GetSByte();
                }
                if (conceptValueType == typeof(short))
                {
                    value = reader.GetInt16();
                }
                else if (conceptValueType == typeof(int))
                {
                    value = reader.GetInt32();
                }
                else if (conceptValueType == typeof(long))
                {
                    value = reader.GetInt64();
                }
                else if (conceptValueType == typeof(byte))
                {
                    value = reader.GetByte();
                }
                else if (conceptValueType == typeof(ushort))
                {
                    value = reader.GetUInt16();
                }
                else if (conceptValueType == typeof(uint))
                {
                    value = reader.GetUInt32();
                }
                else if (conceptValueType == typeof(ulong))
                {
                    value = reader.GetUInt64();
                }
                else if (conceptValueType == typeof(float))
                {
                    value = (float)reader.GetDouble();
                }
                else if (conceptValueType == typeof(double))
                {
                    value = reader.GetDouble();
                }
                else if (conceptValueType == typeof(decimal))
                {
                    value = reader.GetDecimal();
                }
                break;

            default:
                value = reader.GetString();
                break;
        }

        if (value is null)
        {
            return default!;
        }

        return (T)ConceptFactory.CreateConceptInstance(typeToConvert, value)!;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var actualValue = value?.GetConceptValue();
        var conceptValueType = typeof(T).GetConceptValueType();
        if (conceptValueType == typeof(DateOnly))
        {
            writer.WriteStringValue(((DateOnly)actualValue!).ToString("O"));
        }
        else if (conceptValueType == typeof(TimeOnly))
        {
            writer.WriteStringValue(((TimeOnly)actualValue!).ToString("O"));
        }
        else
        {
            var rawValue = JsonSerializer.Serialize(actualValue, conceptValueType, options);
            writer.WriteRawValue(rawValue);
        }
    }
}
