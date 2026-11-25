// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Seeds 205 matching events at the even sequence numbers 0..408 interleaved with 205 distractors at the odd numbers, appended in shuffled order.
/// </summary>
public class a_recorded_multi_batch_sequence : Specification
{
    protected const int MatchingCount = 205;
    protected static readonly EventSourceId _matching = "matching";
    protected EventSequenceStorageHost _host;
    protected RecordingCommands _recorder;
    protected IEventSequenceStorage _storage;
    protected List<int> _batchSizes = [];
    protected List<ulong> _read = [];

    protected IEnumerable<string> RecordedEventSelects =>
        _recorder.Commands.Where(command => command.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) && command.Contains("FROM \"events_", StringComparison.OrdinalIgnoreCase));

    protected static ulong[] Expected(ulong from, ulong to) => Enumerable.Range(0, MatchingCount).Select(index => (ulong)index * 2).Where(number => number >= from && number <= to).ToArray();

    protected async Task Drain(IEventCursor cursor)
    {
        using (cursor)
        {
            while (await cursor.MoveNext())
            {
                _batchSizes.Add(cursor.Current.Count());
                _read.AddRange(cursor.Current.Select(@event => @event.Context.SequenceNumber.Value));
            }
        }
    }

    async Task Establish()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _recorder = new();
        _host = EventSequenceStorageHost.ForSqlite(identities, Substitute.For<IEventTypesStorage>(), _recorder);
        _storage = _host.Open();
        var events = Enumerable.Range(0, MatchingCount * 2)
            .OrderBy(number => Math.Abs(number - 200))
            .Select(number => new EventToAppendToStorage((ulong)number, EventSourceType.Default, number % 2 == 0 ? _matching : "other", EventStreamType.All, EventStreamId.Default, new("batched", 1), CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new(), EventHash.NotSet));
        (await _storage.AppendMany(events)).IsSuccess.ShouldBeTrue();
        _recorder.Commands.Clear();
    }

    void Destroy() => _host.Dispose();
}
