// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_event_sources_that_differ_only_in_spelling;

public abstract class conformance : Specification
{
    protected static readonly string[] _spellings = ["Order-1", "order-1", "Order-1 ", "ＯＲＤＥＲ-1", "Ørder-1"];

    readonly List<ulong[]> _bySource = [];
    readonly List<ulong[]> _byStream = [];
    IEventSequenceStorage _storage;

    protected IIdentityStorage Identities { get; } = Substitute.For<IIdentityStorage>();
    protected IEventTypesStorage EventTypes { get; } = Substitute.For<IEventTypesStorage>();

    protected abstract Task<IEventSequenceStorage> OpenStorage();

    async Task Establish()
    {
        Identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _storage = await OpenStorage();
        for (var index = 0; index < _spellings.Length; index++)
        {
            var appended = await _storage.Append(
                (ulong)index,
                EventSourceType.Default,
                _spellings[index],
                "Orders",
                _spellings[index],
                new("order-placed", 1),
                CorrelationId.NotSet,
                [],
                [],
                [],
                DateTimeOffset.UtcNow,
                new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new() },
                new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = EventHash.NotSet });
            appended.IsSuccess.ShouldBeTrue();
        }
    }

    async Task Because()
    {
        foreach (var spelling in _spellings)
        {
            _bySource.Add(await Read(await _storage.GetFromSequenceNumber(EventSequenceNumber.First, spelling)));
            _byStream.Add(await Read(await _storage.GetFromSequenceNumber(EventSequenceNumber.First, eventStreamType: "Orders", eventStreamId: spelling)));
        }
    }

    static async Task<ulong[]> Read(IEventCursor cursor)
    {
        using (cursor)
        {
            var numbers = new List<ulong>();
            while (await cursor.MoveNext())
            {
                numbers.AddRange(cursor.Current.Select(_ => _.Context.SequenceNumber.Value));
            }

            return [.. numbers];
        }
    }

    [Fact]
    void should_read_only_the_events_of_the_exact_event_source()
    {
        for (var index = 0; index < _spellings.Length; index++)
        {
            _bySource[index].ShouldEqual([(ulong)index]);
        }
    }

    [Fact]
    void should_read_only_the_events_of_the_exact_stream()
    {
        for (var index = 0; index < _spellings.Length; index++)
        {
            _byStream[index].ShouldEqual([(ulong)index]);
        }
    }
}
