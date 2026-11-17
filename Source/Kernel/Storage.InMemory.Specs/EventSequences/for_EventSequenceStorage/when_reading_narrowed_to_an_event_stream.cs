// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_reading_narrowed_to_an_event_stream : given.a_storage_with_events_of_two_event_streams
{
    IEnumerable<ulong> _byType;
    IEnumerable<ulong> _byId;
    IEnumerable<ulong> _byBoth;
    IEnumerable<ulong> _everything;

    async Task Because()
    {
        _byType = await Read(_auditStreamType, EventStreamId.Default);
        _byId = await Read(EventStreamType.All, _secondStreamId);
        _byBoth = await Read(_auditStreamType, _secondStreamId);
        _everything = await Read(EventStreamType.All, EventStreamId.Default);
    }

    [Fact] void should_return_only_the_events_of_the_event_stream_type() => _byType.ShouldEqual([0UL, 2UL]);
    [Fact] void should_return_only_the_events_of_the_event_stream_id() => _byId.ShouldEqual([1UL, 2UL]);
    [Fact] void should_return_only_the_events_matching_both() => _byBoth.ShouldEqual([2UL]);
    [Fact] void should_return_every_event_for_the_sentinels() => _everything.ShouldEqual([0UL, 1UL, 2UL]);
}
