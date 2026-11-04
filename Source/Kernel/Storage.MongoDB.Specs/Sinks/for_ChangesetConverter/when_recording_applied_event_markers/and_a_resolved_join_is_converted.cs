// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

public class and_a_resolved_join_is_converted : given.a_changeset_converter_recording_markers
{
    UpdateDefinitionAndArrayFilters _result;
    UpdateDefinitionAndArrayFilters _withoutEvent;

    async Task Because()
    {
        var source = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = new EventType("join-type", 1),
                EventSourceId = "join-source",
                SequenceNumber = 7UL,
                Namespace = "other-ns"
            }
        };
        _result = await Convert([new ResolvedJoin(new System.Dynamic.ExpandoObject(), "k", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct()], source)], Context());
        _withoutEvent = await Convert([new ResolvedJoin(new System.Dynamic.ExpandoObject(), "k", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct()])], Context());
    }

    [Fact] void should_write_one_guarded_pipeline() => _result.IsConditional.ShouldBeTrue();
    [Fact] void should_record_the_slot_of_the_join_definition() => Json(_result).ShouldContain($"\"__applied.j.{AppliedEventMarkers.DefinitionId("join-type", "joinId")}\"");
    [Fact] void should_identify_the_stream_of_the_join_event() => Json(_result).ShouldContain(AppliedEventMarkers.StreamHash("other-ns", EventSequence, "join-source"));
    [Fact] void should_carry_the_sequence_number_of_the_join_event() => Json(_result).ShouldContain("\"n\" : 7");
    [Fact] void should_write_the_join_unguarded_by_a_slot_when_the_source_is_not_an_event() => Json(_withoutEvent).ShouldNotContain("__applied.j.");

    static string Json(UpdateDefinitionAndArrayFilters result) => result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).ToJson();
}
