// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_the_reference_is_stored_as_another_bson_type(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _int32;
    BsonDocument _int64;
    BsonDocument _text;
    BsonDocument _missing;

    async Task Because()
    {
        await _documents.InsertManyAsync(
        [
            new BsonDocument { ["_id"] = "int32", ["ref"] = new BsonInt32(5) },
            new BsonDocument { ["_id"] = "int64", ["ref"] = new BsonInt64(5) },
            new BsonDocument { ["_id"] = "text", ["ref"] = "5" },
            new BsonDocument { ["_id"] = "missing" }
        ]);
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        foreach (var id in new[] { "int32", "int64", "text", "missing" })
        {
            var resolved = new ResolvedJoin(new System.Dynamic.ExpandoObject(), 5, new PropertyPath("ref"), ArrayIndexers.NoIndexers, [Direct(("count", 9))], JoinEvent(3UL));
            await sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), ChangesetOf(OwnEvent(10UL, id), Direct(("count", 1)), resolved), 10UL, SinkWriteMode.OncePerSourceEvent);
        }

        _int32 = await Read("int32");
        _int64 = await Read("int64");
        _text = await Read("text");
        _missing = await Read("missing");
    }

    [Fact] void should_apply_the_join_when_the_numbers_are_equal_whatever_their_width() => (_int32["count"].ToInt32(), _int64["count"].ToInt32()).ShouldEqual((9, 9));
    [Fact] void should_not_apply_the_join_when_the_stored_value_has_another_type() => _text["count"].ToInt32().ShouldEqual(1);
    [Fact] void should_not_apply_the_join_when_the_reference_is_missing() => _missing["count"].ToInt32().ShouldEqual(1);
}
