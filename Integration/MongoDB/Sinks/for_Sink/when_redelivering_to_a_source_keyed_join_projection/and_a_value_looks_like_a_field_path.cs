// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_value_looks_like_a_field_path(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(5UL), DirectText(("label", "$count")), ResolvedOf(JoinEvent(3UL), ("count", 2)));
        _root = await Read("root");
    }

    [Fact] void should_store_the_text_as_it_is() => _root["label"].AsString.ShouldEqual("$count");
}
