// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexShape;

public class when_key_fields_are_in_a_different_order : Specification
{
    BsonDocument _expected;
    BsonDocument _listed;
    bool _matches;

    void Establish()
    {
        _expected = new("key", new BsonDocument { { "a", 1 }, { "b", -1 } });
        _listed = new("key", new BsonDocument { { "b", -1 }, { "a", 1 } });
    }

    void Because() => _matches = MongoDBIndexShape.Matches(_expected, _listed);

    [Fact] void should_not_match() => _matches.ShouldBeFalse();
}
