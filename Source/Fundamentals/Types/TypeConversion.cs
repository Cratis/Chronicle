// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Types;

/// <summary>
/// Helper methods for type conversion.
/// </summary>
public static class TypeConversion
{
    /// <summary>
    /// Convert a value to specified type.
    /// </summary>
    /// <param name="type">Type to convert to.</param>
    /// <param name="value">Input value.</param>
    /// <returns>Converted instance.</returns>
    public static object Convert(Type type, object value)
    {
        var val = new object();

        if (type.IsGuid())
        {
            if (value.GetType().IsGuid())
            {
                val = value;
            }
            else if (value.GetType().IsString())
            {
                val = Guid.Parse(value.ToString()!);
            }
            else
            {
                val = value.GetType() == typeof(byte[]) ? new Guid((value as byte[])!) : (object)Guid.Empty;
            }
        }
        else if (type.IsString())
        {
            val = value ?? string.Empty;
        }
        else if (type.IsDate())
        {
            val = value ?? default(DateTime);
        }
        else if (type.IsDateOnly())
        {
            if (value is DateOnly)
            {
                val = value;
            }
            else if (DateOnly.TryParse(value.ToString(), out var dateOnlyValue))
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
            else if (TimeOnly.TryParse(value.ToString(), out var timeOnlyValue))
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
            else if (DateTimeOffset.TryParse(value.ToString(), out var dateTimeOffsetValue))
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
            val = System.Convert.ChangeType(val, type, null);
        }
        return val!;
    }

    static bool IsGuidFromString(Type genericArgumentType, object value)
    {
        return genericArgumentType == typeof(Guid) && value.GetType() == typeof(string);
    }
}
