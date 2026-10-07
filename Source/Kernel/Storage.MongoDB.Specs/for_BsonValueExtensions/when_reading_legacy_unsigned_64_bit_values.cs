// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_reading_legacy_unsigned_64_bit_values : Specification
{
    object? _decimal;
    object? _integer;

    void Because()
    {
        _decimal = new BsonDecimal128(-1m).ToTargetType(typeof(ulong));
        _integer = new BsonInt64(-1).ToTargetType(typeof(ulong));
    }

    [Fact] void should_recover_the_maximum_from_negative_decimal128() => _decimal.ShouldEqual(ulong.MaxValue);
    [Fact] void should_recover_the_maximum_from_negative_int64() => _integer.ShouldEqual(ulong.MaxValue);
}
