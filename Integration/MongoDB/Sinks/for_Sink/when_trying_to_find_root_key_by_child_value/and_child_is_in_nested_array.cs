// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value;

[Collection(MongoDBCollection.Name)]
public class and_child_is_in_nested_array(ChronicleInProcessFixture fixture) : given.a_sink_with_test_data(fixture)
{
    Guid _rootKey;
    Guid _configurationId;
    Guid _hubId;
    Option<Key> _result;

    void Establish()
    {
        _rootKey = Guid.NewGuid();
        _configurationId = Guid.NewGuid();
        _hubId = Guid.NewGuid();

        // Insert a document with nested arrays (configurations -> hubs)
        var document = new BsonDocument
        {
            ["name"] = "Simulation Dashboard",
            ["configurations"] = new BsonArray
            {
                new BsonDocument
                {
                    ["configurationId"] = _configurationId.ToString(),
                    ["name"] = "Config 1",
                    ["hubs"] = new BsonArray
                    {
                        new BsonDocument { ["hubId"] = _hubId.ToString(), ["name"] = "Hub 1" },
                        new BsonDocument { ["hubId"] = Guid.NewGuid().ToString(), ["name"] = "Hub 2" }
                    }
                },
                new BsonDocument
                {
                    ["configurationId"] = Guid.NewGuid().ToString(),
                    ["name"] = "Config 2",
                    ["hubs"] = new BsonArray
                    {
                        new BsonDocument { ["hubId"] = Guid.NewGuid().ToString(), ["name"] = "Hub 3" }
                    }
                }
            }
        };
        InsertDocument(_rootKey, document);
    }

    async Task Because() => _result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("configurations.hubs.hubId"), _hubId.ToString());

    [Fact] void should_return_a_value() => _result.HasValue.ShouldBeTrue();
    [Fact]
    void should_return_the_root_key()
    {
        _result.TryGetValue(out var key).ShouldBeTrue();
        key.Value.ToString().ShouldEqual(_rootKey.ToString());
    }
}
