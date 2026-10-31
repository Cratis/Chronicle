// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Creates cache-only identities without changing the BSON used for persistence or queries.
/// </summary>
internal static class SinkCacheKey
{
    /// <summary>
    /// Encodes a BSON key with scale-independent decimal parts and type-preserving structure.
    /// </summary>
    /// <param name="value">The original BSON key.</param>
    /// <returns>The cache identity.</returns>
    public static string For(BsonValue value) => Convert.ToBase64String(new BsonDocument("key", Normalize(value)).ToBson());

    static BsonValue Normalize(BsonValue value) => value switch
    {
        BsonDocument document => new BsonDocument(document.Select(element => new BsonElement(element.Name, Normalize(element.Value)))),
        BsonArray array => new BsonArray(array.Select(Normalize)),
        BsonDecimal128 number => NormalizeDecimal(number.Value),
        _ => value
    };

    static BsonDecimal128 NormalizeDecimal(Decimal128 value)
    {
        if (Decimal128.IsNaN(value)) return new(Decimal128.QNaN);
        if (Decimal128.IsInfinity(value)) return new(Decimal128.IsNegative(value) ? Decimal128.NegativeInfinity : Decimal128.PositiveInfinity);
        if (Decimal128.IsZero(value)) return new(Decimal128.Zero);

        var significand = ((BigInteger)Decimal128.GetSignificandHighBits(value) << 64) | Decimal128.GetSignificandLowBits(value);
        var exponent = Decimal128.GetExponent(value);
        while (significand % 10 == 0)
        {
            significand /= 10;
            exponent++;
        }

        var sign = Decimal128.IsNegative(value) ? "-" : string.Empty;
        return new(Decimal128.Parse($"{sign}{significand.ToString(CultureInfo.InvariantCulture)}E{exponent.ToString(CultureInfo.InvariantCulture)}"));
    }
}
