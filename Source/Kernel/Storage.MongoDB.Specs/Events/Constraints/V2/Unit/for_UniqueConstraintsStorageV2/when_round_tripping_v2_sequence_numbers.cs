// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_round_tripping_v2_sequence_numbers
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(9223372036854775808UL)]
    [InlineData(18446744073709551614UL)]
    [InlineData(ulong.MaxValue)]
    public void should_use_lossless_decimal128(ulong value)
    {
        var encoded = V2Documents.Sequence(new EventSequenceNumber(value));
        encoded.BsonType.ShouldEqual(BsonType.Decimal128);
        V2Documents.Sequence(encoded).Value.ShouldEqual(value);
    }

    [Fact]
    public void should_refuse_fractional_or_narrowed_storage()
    {
        Assert.Throws<UniqueConstraintV2SchemaMismatch>(() => V2Documents.Sequence(new BsonDecimal128(1.5m)));
        Assert.Throws<UniqueConstraintV2SchemaMismatch>(() => V2Documents.Sequence(new BsonInt64(1)));
        var roundedFraction = new BsonDecimal128(Decimal128.Parse("1.000000000000000000000000000000001"));
        Assert.Throws<UniqueConstraintV2SchemaMismatch>(() => V2Documents.Sequence(roundedFraction));
    }
}
