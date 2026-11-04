// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexShape;

public class when_simple_collation_is_omitted : Specification
{
    BsonDocument _expected;
    BsonDocument _listed;
    bool _matches;

    void Establish()
    {
        _expected = new() { { "key", new BsonDocument("a", 1) }, { "collation", Collation.Simple.ToBsonDocument() } };
        _listed = new("key", new BsonDocument("a", 1));
    }

    void Because() => _matches = MongoDBIndexShape.Matches(_expected, _listed);

    [Fact] void should_match() => _matches.ShouldBeTrue();
}
