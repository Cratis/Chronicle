// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_CompositeKeyPermutations.when_permuting;

public class and_there_are_three_parts : Specification
{
    BsonDocument _id = new() { { "b", 2 }, { "a", "a" }, { "c", new BsonDateTime(DateTime.UnixEpoch) } };
    IReadOnlyList<BsonDocument> _result;
    void Because() => _result = CompositeKeyPermutations.For(_id);
    [Fact] void should_return_every_order() => _result.Count.ShouldEqual(6);
    [Fact] void should_return_distinct_orders() => _result.Select(_ => _.ToJson()).Distinct().Count().ShouldEqual(6);
    [Fact] void should_put_the_exact_identity_first() => _result[0].ToBson().ShouldEqual(_id.ToBson());
}
