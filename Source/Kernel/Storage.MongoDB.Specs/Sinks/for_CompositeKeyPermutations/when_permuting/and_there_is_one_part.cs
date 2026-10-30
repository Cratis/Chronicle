// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_CompositeKeyPermutations.when_permuting;

public class and_there_is_one_part : Specification
{
    IReadOnlyList<BsonDocument> _result;
    void Because() => _result = CompositeKeyPermutations.For(new BsonDocument("one", 1));
    [Fact] void should_return_only_the_identity() => _result.Single().ShouldEqual(new BsonDocument("one", 1));
}
