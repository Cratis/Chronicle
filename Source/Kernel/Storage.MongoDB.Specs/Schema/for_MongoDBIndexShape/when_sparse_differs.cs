// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexShape;

public class when_sparse_differs : Specification
{
    BsonDocument _expected;
    BsonDocument _listed;
    bool _matches;

    void Establish()
    {
        _expected = new() { { "key", new BsonDocument("a", 1) }, { "sparse", true } };
        _listed = new() { { "key", new BsonDocument("a", 1) }, { "sparse", false } };
    }

    void Because() => _matches = MongoDBIndexShape.Matches(_expected, _listed);

    [Fact] void should_not_match() => _matches.ShouldBeFalse();
}
