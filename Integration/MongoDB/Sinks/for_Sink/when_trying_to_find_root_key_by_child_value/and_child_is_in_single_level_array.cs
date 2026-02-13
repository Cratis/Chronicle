// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using MongoDB.Bson;
using context = Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value.and_child_is_in_single_level_array.context;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value;

[Collection(MongoDBCollection.Name)]
public class and_child_is_in_single_level_array(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_sink_with_test_data(fixture)
    {
        public Guid RootKey;
        public Guid ChildKey;
        public Option<Key> Result;

        void Establish()
        {
            RootKey = Guid.NewGuid();
            ChildKey = Guid.NewGuid();

            // Insert a document with a child in a single-level array
            var document = new BsonDocument
            {
                ["name"] = "Root Document",
                ["children"] = new BsonArray
                {
                    new BsonDocument { ["childId"] = ChildKey.ToString(), ["name"] = "Child 1" },
                    new BsonDocument { ["childId"] = Guid.NewGuid().ToString(), ["name"] = "Child 2" }
                }
            };
            InsertDocument(RootKey, document);
        }

        async Task Because() => Result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), ChildKey.ToString());
    }

    [Fact] void should_return_a_value() => Context.Result.HasValue.ShouldBeTrue();
    [Fact]
    void should_return_the_root_key()
    {
        Context.Result.TryGetValue(out var key).ShouldBeTrue();
        key.Value.ToString().ShouldEqual(Context.RootKey.ToString());
    }
}
