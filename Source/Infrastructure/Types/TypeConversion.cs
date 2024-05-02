// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Concepts;
using Cratis.Json;
using Cratis.Reflection;

namespace Cratis.Types;

/// <summary>
/// Helper methods for type conversion.
/// </summary>
public static class TypeConversion
{
    static readonly MethodInfo _getValueMethod = typeof(JsonValue).GetMethod(nameof(JsonValue.GetValue))!;

    /// <summary>
    /// Convert a value to specified type.
    /// </summary>
    /// <param name="type">Type to convert to.</param>
    /// <param name="value">Input value.</param>
    /// <returns>Converted instance.</returns>
    public static object Convert(Type type, object value)
    {
        if (value is null)
        {
            return value!;
        }

        if (value is JsonElement jsonElement)
        {
            try
            {
                return jsonElement.Deserialize(type, Globals.JsonSerializerOptions)!;
            }
            catch
            {
                value = jsonElement.ToString();
            }
        }

        if (value is JsonValue)
        {
            try
            {
                return _getValueMethod.MakeGenericMethod(type).Invoke(value, [])!;
            }
            catch
            {
                try
                {
                    return System.Convert.ChangeType(value.ToString(), type, CultureInfo.InvariantCulture)!;
                }
                catch
                {
                }
            }
        }

        var val = new object();

        if (value.IsConcept())
        {
            value = value.GetConceptValue();
        }

        var valueType = value.GetType();
        if (valueType == type)
        {
            return value;
        }

        if (type.IsGuid())
        {
            if (valueType.IsGuid())
            {
                val = value;
            }
            else if (valueType.IsString())
            {
                val = Guid.Parse(value.ToString()!);
            }
            else
            {
                val = valueType == typeof(byte[]) ? new Guid((value as byte[])!) : (object)Guid.Empty;
            }
        }
        else if (type.IsString())
        {
            val = value ?? string.Empty;
        }
        else if (type.IsDate())
        {
            var hasDate = false;

            if (value is DateTime)
            {
                val = value;
            }
            else if (value is string valueAsString)
            {
                hasDate = true;

                if (valueAsString.Contains('+') && DateTime.TryParse(valueAsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTimeValue))
                {
                    val = dateTimeValue;
                    hasDate = true;
                }
                else if (valueAsString.EndsWith('Z') && DateTime.TryParse(valueAsString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dateTimeValueUniversal))
                {
                    val = dateTimeValueUniversal;
                    hasDate = true;
                }
                else if (DateTime.TryParse(valueAsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTimeValueNoInfo))
                {
                    val = dateTimeValueNoInfo;
                    hasDate = true;
                }
            }
            if (!hasDate)
            {
                val = default(DateTime);
            }
        }
        else if (type.IsDateOnly())
        {
            if (value is DateOnly)
            {
                val = value;
            }
            else if (DateOnly.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue))
            {
                val = dateOnlyValue;
            }
            else
            {
                val = default(DateOnly);
            }
        }
        else if (type.IsTimeOnly())
        {
            if (value is TimeOnly)
            {
                val = value;
            }
            else if (TimeOnly.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue))
            {
                val = timeOnlyValue;
            }
            else
            {
                val = default(TimeOnly);
            }
        }
        else if (type.IsDateTimeOffset())
        {
            if (value is DateTimeOffset)
            {
                val = value;
            }
            else if (DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetValue))
            {
                val = dateTimeOffsetValue;
            }
            else
            {
                val = default(DateTimeOffset);
            }
        }
        else if (type.IsAPrimitiveType())
        {
            val = value ?? Activator.CreateInstance(type);
        }

        if (val is not null && val.GetType() != type && !IsGuidFromString(type, val))
        {
            if (type == typeof(string))
            {
                val = val.ToString();
            }
            else
            {
                val = System.Convert.ChangeType(val, type, CultureInfo.InvariantCulture);
            }
        }
        return val!;
    }

    static bool IsGuidFromString(Type genericArgumentType, object value)
    {
        return genericArgumentType == typeof(Guid) && value.GetType() == typeof(string);
    }
}
