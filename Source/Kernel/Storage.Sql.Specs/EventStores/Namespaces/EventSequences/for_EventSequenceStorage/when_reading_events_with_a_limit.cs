// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_reading_events_with_a_limit : Specification
{
    given.EventSequenceStorageHost _host;
    given.RecordingCommands _recorder;
    IEventSequenceStorage _storage;
    readonly List<AppendedEvent> _read = [];
    string _select;

    async Task Establish()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _recorder = new();
        _host = given.EventSequenceStorageHost.ForSqlite(identities, Substitute.For<IEventTypesStorage>(), _recorder);
        _storage = _host.Open();
        (await _storage.AppendMany(new ulong[] { 5, 1, 4, 2, 3 }.Select(number => new EventToAppendToStorage(number, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("limited", 1), CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new(), EventHash.NotSet)))).IsSuccess.ShouldBeTrue();
        _recorder.Commands.Clear();
    }

    async Task Because()
    {
        using var cursor = await _storage.GetEventsWithLimit(new EventSequenceNumber(2), 2);
        while (await cursor.MoveNext())
        {
            _read.AddRange(cursor.Current);
        }
        _select = _recorder.Commands.Single(command => command.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) && command.Contains("FROM \"events_", StringComparison.OrdinalIgnoreCase));
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_return_only_the_lowest_matching_sequence_numbers_in_order() => _read.Select(@event => @event.Context.SequenceNumber.Value).SequenceEqual([2UL, 3UL]).ShouldBeTrue();
    [Fact]
    void should_order_by_sequence_number_before_taking_the_limit()
    {
        var orderBy = _select.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        var limit = _select.LastIndexOf("LIMIT @", StringComparison.OrdinalIgnoreCase);
        var sequenceNumber = _select.IndexOf("\"SequenceNumber\"", Math.Max(0, orderBy), StringComparison.OrdinalIgnoreCase);
        (orderBy >= 0 && sequenceNumber > orderBy && sequenceNumber < limit).ShouldBeTrue();
    }
}
