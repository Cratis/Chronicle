// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Concepts;
using Cratis.Json;
using Cratis.Types;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Validates scalar carriers before using the native schema conversion path.
/// </summary>
/// <param name="modelType">The seeded model type.</param>
/// <param name="property">The canonical part name.</param>
/// <param name="schema">The identifier part schema.</param>
internal sealed class ReadModelSeedKeyValues(Type modelType, PropertyPath property, JsonSchemaProperty schema)
{
    static readonly TypeFormats _formats = new();
    readonly Type? _target = schema.GetTargetTypeForJsonSchemaProperty(_formats);

    /// <summary>
    /// Converts a scalar without native conversion's permissive fallback.
    /// </summary>
    /// <param name="input">The supplied scalar.</param>
    /// <returns>The native scalar value.</returns>
    /// <exception cref="InvalidReadModelSeedKey">The value is not a compatible scalar.</exception>
    public object Convert(object? input)
    {
        if (input is null || _target is null) throw Invalid(input);
        input = Unwrap(input);
        if (!IsCompatibleCarrier(input)) throw Invalid(input);
        if (input is string text && !ValidFormattedText(text)) throw Invalid(input);
        return ConvertScalar(input);
    }

    static bool IsConversionFailure(Exception exception) =>
        exception is FormatException or InvalidCastException or NotSupportedException or OverflowException or ArgumentException;

    static bool IsNumber(Type type) => Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    object ConvertScalar(object input)
    {
        try
        {
            var converted = TypeConversion.Convert(_target!, input);
            if (!IsValidResult(input, converted)) throw Invalid(input);
            return converted;
        }
        catch (Exception exception) when (IsConversionFailure(exception))
        {
            throw Invalid(input);
        }
    }

    bool IsValidResult(object input, object converted) =>
        converted.GetType() == _target && IsLossless(input, converted) && IsAllowedEnumValue(converted);

    bool IsLossless(object input, object converted) =>
        !IsNumber(_target!) || System.Convert.ToDecimal(input, CultureInfo.InvariantCulture) == System.Convert.ToDecimal(converted, CultureInfo.InvariantCulture);

    object Unwrap(object input)
    {
        if (input.IsConcept()) input = input.GetConceptValue();
        if (input is not Enum enumeration) return input;
        if (!Enum.IsDefined(enumeration.GetType(), enumeration)) throw Invalid(input);
        var element = JsonSerializer.SerializeToElement(enumeration, Globals.JsonSerializerOptions);
        return element.ValueKind == JsonValueKind.String
            ? element.GetString()!
            : System.Convert.ChangeType(enumeration, Enum.GetUnderlyingType(enumeration.GetType()), CultureInfo.InvariantCulture);
    }

    bool IsCompatibleCarrier(object input)
    {
        if (_target == typeof(string)) return input is string;
        if (_target == typeof(bool)) return input is bool;
        if (IsNumber(_target!)) return IsNumber(input.GetType());
        return input is string || input.GetType() == _target;
    }

    bool IsAllowedEnumValue(object converted)
    {
        var enumeration = schema.Enumeration.Count > 0 ? schema : schema.ActualSchema;
        return enumeration.Enumeration.Count == 0 || enumeration.Enumeration.Contains(converted) ||
            enumeration.EnumerationNames.Contains(System.Convert.ToString(converted, CultureInfo.InvariantCulture)!);
    }

    bool ValidFormattedText(string text)
    {
        if (_target == typeof(DateOnly) || schema.Format == "date") return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        if (_target == typeof(DateTimeOffset) || schema.Format == "date-time") return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        if (_target == typeof(DateTime)) return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        if (_target == typeof(TimeOnly)) return TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        return true;
    }

    InvalidReadModelSeedKey Invalid(object? actual) =>
        new(modelType, $"key part '{property}' expects a non-null scalar {schema.Type}/{schema.Format}; actual type is '{actual?.GetType().FullName ?? "null"}'. Supply a compatible primitive, concept or defined enum value.");
}
