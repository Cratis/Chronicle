// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;

namespace Cratis.Json;

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
                if (conceptValueType == typeof(byte))
                {
                    value = reader.GetByte();
                }
                else if (conceptValueType == typeof(sbyte))
                {
                    value = reader.GetSByte();
                }
                else if (conceptValueType == typeof(short))
                {
                    value = reader.GetInt16();
                }
                else if (conceptValueType == typeof(ushort))
                {
                    value = reader.GetUInt16();
                }
                else if (conceptValueType == typeof(int))
                {
                    value = reader.GetInt32();
                }
                else if (conceptValueType == typeof(uint))
                {
                    value = reader.GetUInt32();
                }
                else if (conceptValueType == typeof(long))
                {
                    value = reader.GetInt64();
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
                else if (conceptValueType == typeof(bool))
                {
                    value = reader.GetBoolean();
                }
                else if (conceptValueType == typeof(Guid))
                {
                    value = reader.GetGuid();
                }
                else if (conceptValueType.IsEnum)
                {
                    value = Enum.Parse(conceptValueType, reader.GetInt32().ToString());
                }
                break;

            default:
                value = reader.GetString();
                if (conceptValueType.IsEnum)
                {
                    value = Enum.Parse(conceptValueType, (value as string)!);
                }
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
        if (actualValue is null) return;

        var conceptValueType = typeof(T).GetConceptValueType();
        if (conceptValueType == typeof(DateOnly))
        {
            writer.WriteStringValue(((DateOnly)actualValue).ToString("O"));
        }
        else if (conceptValueType == typeof(TimeOnly))
        {
            writer.WriteStringValue(((TimeOnly)actualValue).ToString("O"));
        }
        else if (conceptValueType == typeof(byte))
        {
            writer.WriteNumberValue((byte)actualValue);
        }
        else if (conceptValueType == typeof(sbyte))
        {
            writer.WriteNumberValue((sbyte)actualValue);
        }
        else if (conceptValueType == typeof(short))
        {
            writer.WriteNumberValue((short)actualValue);
        }
        else if (conceptValueType == typeof(ushort))
        {
            writer.WriteNumberValue((ushort)actualValue);
        }
        else if (conceptValueType == typeof(int))
        {
            writer.WriteNumberValue((int)actualValue);
        }
        else if (conceptValueType == typeof(uint))
        {
            writer.WriteNumberValue((uint)actualValue);
        }
        else if (conceptValueType == typeof(long))
        {
            writer.WriteNumberValue((long)actualValue);
        }
        else if (conceptValueType == typeof(ulong))
        {
            writer.WriteNumberValue((ulong)actualValue);
        }
        else if (conceptValueType == typeof(float))
        {
            writer.WriteNumberValue((float)actualValue);
        }
        else if (conceptValueType == typeof(double))
        {
            writer.WriteNumberValue((double)actualValue);
        }
        else if (conceptValueType == typeof(decimal))
        {
            writer.WriteNumberValue((decimal)actualValue);
        }
        else if (conceptValueType == typeof(bool))
        {
            writer.WriteBooleanValue((bool)actualValue);
        }
        else if (conceptValueType == typeof(Guid))
        {
            writer.WriteStringValue(actualValue.ToString());
        }
        else if (conceptValueType.IsEnum)
        {
            writer.WriteNumberValue((int)actualValue);
        }
        else
        {
            var rawValue = JsonSerializer.Serialize(actualValue, conceptValueType, options);
            writer.WriteRawValue(rawValue);
        }
    }
}
