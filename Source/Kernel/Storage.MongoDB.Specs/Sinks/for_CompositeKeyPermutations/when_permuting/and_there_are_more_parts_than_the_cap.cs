// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_CompositeKeyPermutations.when_permuting;

public class and_there_are_more_parts_than_the_cap : Specification
{
    IReadOnlyList<BsonDocument> _result;
    void Because() => _result = CompositeKeyPermutations.For(new BsonDocument(Enumerable.Range(0, 6).Select(_ => new BsonElement($"part{_}", _))));
    [Fact] void should_not_generate_permutations() => _result.Count.ShouldEqual(0);
}
