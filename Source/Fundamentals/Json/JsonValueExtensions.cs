// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Json;

/// <summary>
/// Represents extension methods for <see cref="JsonNode"/>.
/// </summary>
public static class JsonValueExtensions
{
    /// <summary>
    /// Convert a <see cref="JsonValue"/> to a specific <see cref="Type"/>.
    /// </summary>
    /// <param name="value"><see cref="JsonValue"/> to convert.</param>
    /// <param name="targetType"><see cref="Type"/> to convert to.</param>
    /// <returns>The converted type.</returns>
    public static object? ToTargetTypeValue(this JsonValue value, Type targetType)
    {
        switch (Type.GetTypeCode(targetType))
        {
            case TypeCode.Int16:
                return value.GetValue<short>();

            case TypeCode.UInt16:
                return value.GetValue<ushort>();

            case TypeCode.Int32:
                return value.GetValue<int>();

            case TypeCode.UInt32:
                return value.GetValue<uint>();

            case TypeCode.Int64:
                return value.GetValue<long>();

            case TypeCode.UInt64:
                return value.GetValue<ulong>();

            case TypeCode.Single:
                return value.GetValue<float>();

            case TypeCode.Double:
                return value.GetValue<double>();

            case TypeCode.Decimal:
                return value.GetValue<decimal>();

            case TypeCode.DateTime:
                return value.GetValue<DateTime>();

            case TypeCode.Byte:
                return value.GetValue<byte>();
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(value.GetValue<string>());
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return value.GetValue<DateTimeOffset>();
        }

        if (targetType == typeof(DateOnly))
        {
            return value.GetValue<DateOnly>();
        }

        if (targetType == typeof(TimeOnly))
        {
            return value.GetValue<TimeOnly>();
        }

        return null;
    }

    /// <summary>
    /// Convert any <see cref="object"/> to a <see cref="JsonValue"/>.
    /// </summary>
    /// <param name="input">Input value.</param>
    /// <returns>Converted <see cref="JsonValue"/>.</returns>
    public static JsonValue? ToJsonValue(this object? input)
    {
        if (input is null)
        {
            return null;
        }

        if (input.IsConcept())
        {
            input = input.GetConceptValue();
        }

        switch (input)
        {
            case short actualValue:
                return JsonValue.Create<short>(actualValue);

            case ushort actualValue:
                return JsonValue.Create<ushort>(actualValue);

            case int actualValue:
                return JsonValue.Create<int>(actualValue);

            case uint actualValue:
                return JsonValue.Create<uint>(actualValue);

            case long actualValue:
                return JsonValue.Create<long>(actualValue);

            case ulong actualValue:
                return JsonValue.Create<ulong>(actualValue);

            case float actualValue:
                return JsonValue.Create<float>(actualValue);

            case double actualValue:
                return JsonValue.Create<double>(actualValue);

            case decimal actualValue:
                return JsonValue.Create<decimal>(actualValue);

            case bool actualValue:
                return JsonValue.Create<bool>(actualValue);

            case string actualValue:
                return JsonValue.Create<string>(actualValue);

            case DateTime actualValue:
                return JsonValue.Create<DateTime>(actualValue);

            case DateTimeOffset actualValue:
                return JsonValue.Create<DateTimeOffset>(actualValue);

            case DateOnly actualValue:
                return JsonValue.Create(actualValue);

            case TimeOnly actualValue:
                return JsonValue.Create(actualValue);

            case Guid actualValue:
                return JsonValue.Create<Guid>(actualValue);
        }

        return null;
    }
}
