// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_unsigned_64_bit_values : Specification
{
    BsonValue _maximum;
    BsonValue _array;

    void Because()
    {
        _maximum = ulong.MaxValue.ToBsonValue();
        _array = new[] { ulong.MaxValue }.ToBsonValue();
    }

    [Fact] void should_preserve_the_maximum_value() => _maximum.AsDecimal.ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_array_elements() => _array.AsBsonArray[0].AsDecimal.ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_the_maximum_from_decimal128() => new BsonDecimal128((decimal)ulong.MaxValue).ToTargetType(typeof(ulong)).ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_existing_int64_values() => new BsonInt64(long.MaxValue).ToTargetType(typeof(ulong)).ShouldEqual((ulong)long.MaxValue);
}
