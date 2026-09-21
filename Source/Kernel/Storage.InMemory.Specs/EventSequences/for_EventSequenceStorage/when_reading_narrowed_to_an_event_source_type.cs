// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

/// <summary>
/// Two event source types share one event source identifier here, which is the situation the filter exists
/// for: without it a read narrowed to one of them silently returns the other's events too. The parameter was
/// accepted by every client and dropped before it reached storage - see #4049.
/// </summary>
public class when_reading_narrowed_to_an_event_source_type : given.a_storage_with_events_of_two_source_types
{
    IEnumerable<AppendedEvent> _orders;
    IEnumerable<AppendedEvent> _customers;
    IEnumerable<AppendedEvent> _everything;

    async Task Because()
    {
        _orders = await Read(_orderSourceType);
        _customers = await Read(_customerSourceType);

        // The sentinel still has to mean "do not narrow" rather than "match the unspecified type".
        _everything = await Read(EventSourceType.Unspecified);
    }

    [Fact] void should_return_only_the_events_of_that_source_type() => _orders.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 2UL]);
    [Fact] void should_return_only_the_other_source_types_events_when_narrowed_to_it() => _customers.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([1UL]);
    [Fact] void should_return_every_event_when_the_source_type_is_unspecified() => _everything.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([0UL, 1UL, 2UL]);

    async Task<IEnumerable<AppendedEvent>> Read(EventSourceType eventSourceType)
    {
        var events = new List<AppendedEvent>();
        using var cursor = await _storage.GetFromSequenceNumber(
            EventSequenceNumber.First,
            EventSourceId.Unspecified,
            eventSourceType,
            EventStreamType.All,
            EventStreamId.Default,
            []);

        while (await cursor.MoveNext())
        {
            events.AddRange(cursor.Current);
        }

        return events;
    }
}
