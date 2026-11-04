// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCacheKey;

public class when_normalizing_decimal_parts : Specification
{
    BsonDocument _source;
    string _identity;

    void Establish() => _source = new BsonDocument("amount", new BsonArray { new BsonDocument("value", new BsonDecimal128(1.00m)) });

    void Because() => _identity = SinkCacheKey.For(_source);

    [Fact] void should_ignore_decimal_scale() => _identity.ShouldEqual(SinkCacheKey.For(new BsonDocument("amount", new BsonArray { new BsonDocument("value", new BsonDecimal128(1.0m)) })));
    [Fact] void should_preserve_the_source_scale() => _source["amount"][0]["value"].AsDecimal128.ToString().ShouldEqual("1.00");
    [Fact] void should_distinguish_values() => SinkCacheKey.For(new BsonDecimal128(1.01m)).ShouldNotEqual(SinkCacheKey.For(new BsonDecimal128(1.00m)));
    [Fact] void should_preserve_strings() => SinkCacheKey.For(new BsonString("1.00")).ShouldNotEqual(SinkCacheKey.For(new BsonDecimal128(1.00m)));
    [Fact] void should_preserve_part_order() => SinkCacheKey.For(new BsonDocument { { "a", 1 }, { "b", 2 } }).ShouldNotEqual(SinkCacheKey.For(new BsonDocument { { "b", 2 }, { "a", 1 } }));
    [Fact] void should_preserve_structure() => SinkCacheKey.For(new BsonArray { 1 }).ShouldNotEqual(SinkCacheKey.For(new BsonDocument("0", 1)));
    [Fact] void should_support_values_beyond_decimal_precision() => SinkCacheKey.For(new BsonDecimal128(Decimal128.Parse("1234567890123456789012345678901230"))).ShouldEqual(SinkCacheKey.For(new BsonDecimal128(Decimal128.Parse("123456789012345678901234567890123E1"))));
    [Fact] void should_support_the_maximum_exponent() => SinkCacheKey.For(new BsonDecimal128(Decimal128.Parse("1E6144"))).ShouldEqual(SinkCacheKey.For(new BsonDecimal128(Decimal128.Parse("10E6143"))));
    [Fact] void should_normalize_signed_zero() => SinkCacheKey.For(new BsonDecimal128(Decimal128.Parse("-0.00"))).ShouldEqual(SinkCacheKey.For(new BsonDecimal128(0m)));
    [Fact] void should_preserve_negative_values() => SinkCacheKey.For(new BsonDecimal128(-1m)).ShouldNotEqual(SinkCacheKey.For(new BsonDecimal128(1m)));

    [Fact]
    void should_be_independent_of_current_culture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;
            SinkCacheKey.For(_source).ShouldEqual(_identity);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
