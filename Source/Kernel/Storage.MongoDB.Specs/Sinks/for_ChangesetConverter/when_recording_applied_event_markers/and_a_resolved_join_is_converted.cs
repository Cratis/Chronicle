// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

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

    [Fact] void should_collect_one_slot() => _result.ResolvedJoinSlots.Count.ShouldEqual(1);
    [Fact] void should_identify_the_definition_by_the_event_type_and_the_property() => _result.ResolvedJoinSlots[0].DefinitionId.ShouldEqual(AppliedEventMarkers.DefinitionId("join-type", "joinId"));
    [Fact] void should_identify_the_stream_of_the_join_event() => _result.ResolvedJoinSlots[0].Stream.ShouldEqual(AppliedEventMarkers.StreamHash("other-ns", "join-source"));
    [Fact] void should_carry_the_sequence_number_of_the_join_event() => _result.ResolvedJoinSlots[0].SequenceNumber.ShouldEqual((EventSequenceNumber)7UL);
    [Fact] void should_collect_nothing_when_the_source_is_not_an_event() => _withoutEvent.ResolvedJoinSlots.ShouldBeEmpty();
}
