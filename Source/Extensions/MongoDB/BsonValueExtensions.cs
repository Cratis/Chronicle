// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;
using MongoDB.Bson;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="BsonValue"/>.
/// </summary>
public static class BsonValueExtensions
{
    /// <summary>
    /// Convert a CLR value to the correct <see cref="BsonValue"/>.
    /// </summary>
    /// <param name="input">Input value to convert from.</param>
    /// <returns>Converted <see cref="BsonValue"/>.</returns>
    public static BsonValue ToBsonValue(this object? input)
    {
        if (input is null)
        {
            return BsonNull.Value;
        }

        if (input.IsConcept())
        {
            input = input.GetConceptValue();
        }

        switch (input)
        {
            case short actualValue:
                return new BsonInt32(actualValue);

            case ushort actualValue:
                return new BsonInt32(actualValue);

            case int actualValue:
                return new BsonInt32(actualValue);

            case uint actualValue:
                return new BsonInt32((int)actualValue);

            case long actualValue:
                return new BsonInt64(actualValue);

            case ulong actualValue:
                return new BsonInt64((long)actualValue);

            case float actualValue:
                return new BsonDouble(actualValue);

            case double actualValue:
                return new BsonDouble(actualValue);

            case decimal actualValue:
                return new BsonDecimal128(actualValue);

            case bool actualValue:
                return new BsonBoolean(actualValue);

            case string actualValue:
                if (Guid.TryParse(actualValue, out var actualValueAsGuid))
                {
                    return new BsonBinaryData(actualValueAsGuid, GuidRepresentation.Standard);
                }
                return new BsonString(actualValue);

            case DateTime actualValue:
                return new BsonDateTime(actualValue);

            case DateTimeOffset actualValue:
                return new BsonDateTime(actualValue.ToUnixTimeMilliseconds());

            case DateOnly actualValue:
                return new BsonDateTime(actualValue.ToDateTime(new TimeOnly(12, 0)));

            case TimeOnly actualValue:
                return new BsonDateTime(BsonUtils.ToMillisecondsSinceEpoch(DateTime.UnixEpoch + actualValue.ToTimeSpan()));

            case Guid actualValue:
                return new BsonBinaryData(actualValue, GuidRepresentation.Standard);
        }

        return BsonNull.Value;
    }
}
