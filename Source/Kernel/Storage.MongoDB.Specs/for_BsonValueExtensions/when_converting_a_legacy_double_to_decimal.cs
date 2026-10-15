// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_a_legacy_double_to_decimal : Specification
{
    decimal _result;

    void Because() => _result = (decimal)new BsonDouble(193.58).ToTargetType(typeof(decimal))!;

    [Fact] void should_use_the_shortest_round_trip_representation() => decimal.GetBits(_result).ShouldEqual(decimal.GetBits(193.58m));
}
