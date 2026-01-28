// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using MongoDB.Bson;
using context = Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value.and_child_is_in_nested_array.context;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value;

[Collection(MongoDBCollection.Name)]
public class and_child_is_in_nested_array(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_sink_with_test_data(fixture)
    {
        public Guid RootKey;
        public Guid ConfigurationId;
        public Guid HubId;
        public Option<Key> Result;

        void Establish()
        {
            RootKey = Guid.NewGuid();
            ConfigurationId = Guid.NewGuid();
            HubId = Guid.NewGuid();

            // Insert a document with nested arrays (configurations -> hubs)
            var document = new BsonDocument
            {
                ["name"] = "Simulation Dashboard",
                ["configurations"] = new BsonArray
                {
                    new BsonDocument
                    {
                        ["configurationId"] = ConfigurationId.ToString(),
                        ["name"] = "Config 1",
                        ["hubs"] = new BsonArray
                        {
                            new BsonDocument { ["hubId"] = HubId.ToString(), ["name"] = "Hub 1" },
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
            InsertDocument(RootKey, document);
        }

        async Task Because() => Result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("configurations.hubs.hubId"), HubId.ToString());
    }

    [Fact] void should_return_a_value() => Context.Result.HasValue.ShouldBeTrue();
    [Fact]
    void should_return_the_root_key()
    {
        Context.Result.TryGetValue(out var key).ShouldBeTrue();
        key.Value.ToString().ShouldEqual(Context.RootKey.ToString());
    }
}
