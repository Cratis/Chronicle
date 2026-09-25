// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_hashes_are_provided : given.an_event_sequence_storage
{
    AppendedEvent[] _acknowledged;

    async Task Because()
    {
        var occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await _storage.AppendMany(
        [
            new EventToAppendToStorage(EventSequenceNumber.First, EventSourceType.Default, "first", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], occurred, new ExpandoObject(), "first-hash"),
            new EventToAppendToStorage(EventSequenceNumber.First.Next(), EventSourceType.Default, "second", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], occurred, new ExpandoObject(), "second-hash")
        ]);
        _acknowledged = result.AsT0.ToArray();
    }

    [Fact] void should_acknowledge_both_events() => _acknowledged.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_hashes_in_input_order() => _acknowledged.Select(_ => _.Context.Hash.Value).ShouldEqual(["first-hash", "second-hash"]);
    [Fact] void should_store_the_acknowledged_hashes() => _storage.Events.Select(_ => _.Context.Hash).ShouldEqual(_acknowledged.Select(_ => _.Context.Hash));
}
