// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCacheKey;

public class when_normalizing_special_decimal_parts : Specification
{
    [Fact] void should_normalize_signaling_nan() => AssertEquivalent(Decimal128.SNaN, Decimal128.QNaN);
    [Fact] void should_normalize_negative_quiet_nan() => AssertEquivalent(Decimal128.FromIEEEBits(0xFC00000000000000, 0), Decimal128.QNaN);
    [Fact] void should_normalize_negative_signaling_nan() => AssertEquivalent(Decimal128.FromIEEEBits(0xFE00000000000000, 0), Decimal128.QNaN);
    [Fact] void should_normalize_quiet_nan_payloads() => AssertEquivalent(Decimal128.FromIEEEBits(0x7C00000000000001, 42), Decimal128.QNaN);
    [Fact] void should_normalize_signaling_nan_payloads() => AssertEquivalent(Decimal128.FromIEEEBits(0xFE00000000000001, 42), Decimal128.QNaN);
    [Fact] void should_normalize_positive_infinity_payloads() => AssertEquivalent(Decimal128.FromIEEEBits(0x7800000000000001, 42), Decimal128.PositiveInfinity);
    [Fact] void should_normalize_negative_infinity_payloads() => AssertEquivalent(Decimal128.FromIEEEBits(0xF800000000000001, 42), Decimal128.NegativeInfinity);
    [Fact] void should_preserve_infinity_sign() => SinkCacheKey.For(new BsonDecimal128(Decimal128.PositiveInfinity)).ShouldNotEqual(SinkCacheKey.For(new BsonDecimal128(Decimal128.NegativeInfinity)));
    [Fact] void should_distinguish_nan_from_infinity() => SinkCacheKey.For(new BsonDecimal128(Decimal128.QNaN)).ShouldNotEqual(SinkCacheKey.For(new BsonDecimal128(Decimal128.PositiveInfinity)));

    static void AssertEquivalent(Decimal128 value, Decimal128 canonical)
    {
        Decimal128.Compare(value, canonical).ShouldEqual(0);
        var scalar = new BsonDecimal128(value);
        var composite = new BsonDocument("amount", new BsonArray { scalar });
        var originalBytes = composite.ToBson();
        SinkCacheKey.For(scalar).ShouldEqual(SinkCacheKey.For(new BsonDecimal128(canonical)));
        SinkCacheKey.For(composite).ShouldEqual(SinkCacheKey.For(new BsonDocument("amount", new BsonArray { new BsonDecimal128(canonical) })));
        composite.ToBson().ShouldEqual(originalBytes);
    }
}
