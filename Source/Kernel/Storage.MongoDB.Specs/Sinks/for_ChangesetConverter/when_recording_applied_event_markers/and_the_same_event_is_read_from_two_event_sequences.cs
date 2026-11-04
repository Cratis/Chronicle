// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

public class and_the_same_event_is_read_from_two_event_sequences : given.a_changeset_converter_recording_markers
{
    string _fromLog;
    string _fromOutbox;

    async Task Because()
    {
        _fromLog = Json(await Convert([Direct()], Context()));
        _fromOutbox = Json(await Convert([Direct()], Context() with { EventSequence = "outbox" }));
    }

    [Fact] void should_record_the_stream_of_the_sequence_it_was_read_from() => _fromLog.ShouldContain(Stream);
    [Fact] void should_not_share_the_stream_with_the_other_sequence() => _fromOutbox.ShouldNotContain(Stream);
    [Fact] void should_record_the_stream_of_the_other_sequence() => _fromOutbox.ShouldContain(Cratis.Chronicle.Storage.Sinks.AppliedEventMarkers.StreamHash(Namespace, "outbox", SourceValue));

    static string Json(UpdateDefinitionAndArrayFilters result) => result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).ToJson();
}
