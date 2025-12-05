// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value;

[Collection(MongoDBCollection.Name)]
public class and_child_is_in_single_level_array(ChronicleInProcessFixture fixture) : given.a_sink_with_test_data(fixture)
{
    Guid _rootKey;
    Guid _childKey;
    Option<Key> _result;

    void Establish()
    {
        _rootKey = Guid.NewGuid();
        _childKey = Guid.NewGuid();

        // Insert a document with a child in a single-level array
        var document = new BsonDocument
        {
            ["name"] = "Root Document",
            ["children"] = new BsonArray
            {
                new BsonDocument { ["childId"] = _childKey.ToString(), ["name"] = "Child 1" },
                new BsonDocument { ["childId"] = Guid.NewGuid().ToString(), ["name"] = "Child 2" }
            }
        };
        InsertDocument(_rootKey, document);
    }

    async Task Because() => _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), _childKey.ToString());

    [Fact] void should_return_a_value() => _result.HasValue.ShouldBeTrue();
    [Fact]
    void should_return_the_root_key()
    {
        _result.TryGetValue(out var key).ShouldBeTrue();
        key.Value.ToString().ShouldEqual(_rootKey.ToString());
    }
}
