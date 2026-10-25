// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

/// <summary>
/// Revision repairs each affected observer without narrowing full replay to the revised type.
/// </summary>
public class and_observers_require_different_replay_scopes : given.a_content_mutation_with_observers
{
    Task Because() => _eventSequence.Revise(42UL, _eventType with { Generation = new(2) }, new JsonObject(), CorrelationId.New(), [], Identity.System);

    [Fact] Task should_replay_the_implicit_partition_once() => ShouldReplayPartition("implicit");
    [Fact] Task should_replay_the_explicit_partition_once() => ShouldReplayPartition("explicit");
    [Fact] Task should_fully_replay_an_arbitrary_key_once() => ShouldReplayAll("arbitrary");
    [Fact] Task should_fully_replay_a_derived_key_once() => ShouldReplayAll("derived");
    [Fact] Task should_fully_replay_a_join_once() => ShouldReplayAll("joined");
    [Fact] Task should_fully_replay_a_parent_key_once() => ShouldReplayAll("parent");
    [Fact] Task should_fully_replay_children_once() => ShouldReplayAll("children");
    [Fact] Task should_fully_replay_a_legacy_definition_once() => ShouldReplayAll("legacy");
    [Fact] Task should_fully_replay_an_unknown_definition_once() => ShouldReplayAll("unknown");
    [Fact] Task should_fully_replay_a_missing_definition_once() => ShouldReplayAll("missing");
    [Fact] Task should_keep_reducer_partition_replay() => ShouldReplayPartition("reducer");
    [Fact] Task should_keep_reactor_partition_replay() => ShouldReplayPartition("reactor");
    [Fact] Task should_keep_external_partition_replay() => ShouldReplayPartition("external");
    [Fact] void should_leave_unrelated_observers_untouched() => ShouldLeaveUntouched("unrelated");
    [Fact] void should_leave_non_replayable_observers_untouched() => ShouldLeaveUntouched("non-replayable");
    [Fact] Task should_select_observers_using_the_original_event_type() => _observerDefinitions.Received(1)
        .GetReplayableObserversForEventTypes(Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(new[] { _eventType })));
    [Fact] void should_list_projection_definitions_once() => ShouldListProjectionDefinitionsOnce();
    [Fact] void should_read_definitions_in_the_mutated_event_store() => ShouldUseEventStoreScope();
}
