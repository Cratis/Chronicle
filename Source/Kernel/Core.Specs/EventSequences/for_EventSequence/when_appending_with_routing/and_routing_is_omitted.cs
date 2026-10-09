// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_routing;

public class and_routing_is_omitted : given.a_routed_event_sequence
{
    AppendResult _result;

    async Task Because() => _result = await new Sequences.Append(
        EventStore,
        EventStoreNamespace,
        SequenceId,
        _eventSourceId,
        null!,
        null!,
        null!,
        new Sequences.EventType(_eventType.Id.Value, 1, false),
        "{}")
        .Handle(_grainFactory, _causation, _principal);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_default_event_source_type() => _storedRoutes.Single().SourceType.Value.ShouldEqual("Default");
    [Fact] void should_store_the_all_event_stream_type() => _storedRoutes.Single().StreamType.Value.ShouldEqual("All");
    [Fact] void should_store_the_default_event_stream_id() => _storedRoutes.Single().StreamId.Value.ShouldEqual("Default");
}
