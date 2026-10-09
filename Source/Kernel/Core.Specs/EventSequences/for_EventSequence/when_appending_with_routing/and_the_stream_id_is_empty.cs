// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_routing;

public class and_the_stream_id_is_empty : given.a_routed_event_sequence
{
    AppendResult _result;

    async Task Because() => _result = await new Sequences.Append(
        EventStore,
        EventStoreNamespace,
        SequenceId,
        _eventSourceId,
        "Account",
        "Payments",
        new EventStreamId(string.Empty),
        new Sequences.EventType(_eventType.Id.Value, 1, false),
        "{}")
        .Handle(_grainFactory, _causation, _principal);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_event_source_type() => _storedRoutes.Single().SourceType.Value.ShouldEqual("Account");
    [Fact] void should_preserve_the_event_stream_type() => _storedRoutes.Single().StreamType.Value.ShouldEqual("Payments");
    [Fact] void should_store_the_default_stream_id() => _storedRoutes.Single().StreamId.Value.ShouldEqual("Default");
}
