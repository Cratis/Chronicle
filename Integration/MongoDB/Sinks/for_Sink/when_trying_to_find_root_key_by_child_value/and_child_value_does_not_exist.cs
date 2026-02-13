// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using MongoDB.Bson;
using context = Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value.and_child_value_does_not_exist.context;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_trying_to_find_root_key_by_child_value;

[Collection(MongoDBCollection.Name)]
public class and_child_value_does_not_exist(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_sink_with_test_data(fixture)
    {
        public Guid RootKey;
        public Guid NonExistentId;
        public Option<Key> Result;

        void Establish()
        {
            RootKey = Guid.NewGuid();
            NonExistentId = Guid.NewGuid();

            // Insert a document but search for a non-existent child
            var document = new BsonDocument
            {
                ["name"] = "Root Document",
                ["children"] = new BsonArray
                {
                    new BsonDocument { ["childId"] = Guid.NewGuid().ToString(), ["name"] = "Child 1" }
                }
            };
            InsertDocument(RootKey, document);
        }

        async Task Because() => Result = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), NonExistentId.ToString());
    }

    [Fact] void should_return_no_value() => Context.Result.HasValue.ShouldBeFalse();
}
