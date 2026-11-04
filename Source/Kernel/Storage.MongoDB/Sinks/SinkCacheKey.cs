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
    const string DecimalIdentityMarker = "$sinkDecimalIdentity";

    /// <summary>
    /// Creates the cache identity of a BSON key. Keys without a Decimal128 keep the BSON value's string form;
    /// keys containing one get a scale-independent, type-preserving encoding.
    /// </summary>
    /// <param name="value">The original BSON key.</param>
    /// <returns>The cache identity.</returns>
    public static string For(BsonValue value) => ContainsDecimal(value)
        ? Convert.ToBase64String(new BsonDocument("key", NormalizeDecimals(value)).ToBson())
        : value.ToString()!;

    /// <summary>
    /// Gets an equality identity for grouping and matching keys: the BSON value itself when it contains no Decimal128
    /// (no allocation, same comparison as before), otherwise a marker document holding its <see cref="For"/> string.
    /// The marker name starts with '$', which MongoDB does not allow in an _id document, so it cannot equal a stored key.
    /// </summary>
    /// <param name="value">The original BSON key.</param>
    /// <returns>The identity value.</returns>
    public static BsonValue Identity(BsonValue value) => ContainsDecimal(value) ? new BsonDocument(DecimalIdentityMarker, For(value)) : value;

    /// <summary>
    /// Gets the cache identity string for a value produced by <see cref="Identity"/>.
    /// </summary>
    /// <param name="identity">The identity value.</param>
    /// <returns>The cache identity, equal to <see cref="For"/> of the original key.</returns>
    public static string ForIdentity(BsonValue identity) =>
        identity is BsonDocument { ElementCount: 1 } document && document.GetElement(0).Name == DecimalIdentityMarker
            ? document[0].AsString
            : identity.ToString()!;

    static bool ContainsDecimal(BsonValue value)
    {
        switch (value)
        {
            case BsonDecimal128:
                return true;
            case BsonDocument document:
                for (var i = 0; i < document.ElementCount; i++)
                {
                    if (ContainsDecimal(document.GetElement(i).Value)) return true;
                }

                return false;
            case BsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (ContainsDecimal(array[i])) return true;
                }

                return false;
            default:
                return false;
        }
    }

    static BsonValue NormalizeDecimals(BsonValue value) => value switch
    {
        BsonDocument document => new BsonDocument(document.Select(element => new BsonElement(element.Name, NormalizeDecimals(element.Value)))),
        BsonArray array => new BsonArray(array.Select(NormalizeDecimals)),
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
