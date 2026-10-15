// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_round_tripping_routing;

public class and_the_stream_id_contains_delimiters_and_non_ascii_text : given.a_storage_for_routing
{
    async Task Because()
    {
        _appendSucceeded = (await AppendWithRouting("Ærø|50%")).IsSuccess;
        _readBack = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_succeed() => _appendSucceeded.ShouldBeTrue();
    [Fact] void should_read_back_the_event_source_type() => _readBack.Context.EventSourceType.Value.ShouldEqual("Account");
    [Fact] void should_read_back_the_event_stream_type() => _readBack.Context.EventStreamType.Value.ShouldEqual("Payments");
    [Fact] void should_read_back_the_exact_stream_id() => _readBack.Context.EventStreamId.Value.ShouldEqual("Ærø|50%");
}
