// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Seeds 150 blocks of seven events in shuffled order. The first event of each block matches every criterion;
/// each of the other six differs in exactly one criterion: event source id, event source type, event stream type, event stream id, event type or tag.
/// </summary>
public class a_sequence_with_single_criterion_distractors : Specification
{
    protected const int Blocks = 150;
    protected const ulong BlockSize = 7;
    protected const int StreamIdDistractorSlot = 6;
    protected static readonly EventSourceId _sourceId = "wanted-source";
    protected static readonly EventSourceType _sourceType = "wanted-source-type";
    protected static readonly EventStreamType _streamType = "wanted-stream-type";
    protected static readonly EventStreamId _streamId = "wanted-stream";
    protected static readonly EventType _typeA = new("type-a", 1);
    protected static readonly EventType _typeB = new("type-b", 1);
    protected static readonly EventType _otherType = new("type-other", 1);
    protected static readonly Tag _tagA = "tag-a";
    protected static readonly Tag _tagB = "tag-b";
    protected EventSequenceStorageHost _host;
    protected IEventSequenceStorage _storage;
    protected List<ulong> _read = [];

    protected static ulong[] Matches(ulong from, ulong to, params int[] slots) =>
        Enumerable.Range(0, Blocks)
            .SelectMany(block => slots.Select(slot => ((ulong)block * BlockSize) + (ulong)slot))
            .Where(number => number >= from && number <= to)
            .Order()
            .ToArray();

    protected async Task Drain(IEventCursor cursor)
    {
        using (cursor)
        {
            while (await cursor.MoveNext())
            {
                _read.AddRange(cursor.Current.Select(@event => @event.Context.SequenceNumber.Value));
            }
        }
    }

    static EventToAppendToStorage Create(ulong number)
    {
        var block = (int)(number / BlockSize);
        var slot = (int)(number % BlockSize);
        var type = block % 2 == 0 ? _typeA : _typeB;
        var tag = block % 3 == 0 ? _tagA : _tagB;
        return new(
            number,
            slot == 2 ? "other-source-type" : _sourceType,
            slot == 1 ? "other-source" : _sourceId,
            slot == 3 ? "other-stream-type" : _streamType,
            slot == StreamIdDistractorSlot ? "other-stream" : _streamId,
            slot == 4 ? _otherType : type,
            CorrelationId.NotSet,
            [],
            [],
            [slot == 5 ? "tag-other" : tag],
            DateTimeOffset.UtcNow,
            new(),
            EventHash.NotSet);
    }

    async Task Establish()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _host = EventSequenceStorageHost.ForSqlite(identities, Substitute.For<IEventTypesStorage>(), new RecordingCommands());
        _storage = _host.Open();
        var events = Enumerable.Range(0, Blocks * (int)BlockSize).OrderBy(number => Math.Abs(number - 500)).Select(number => Create((ulong)number));
        (await _storage.AppendMany(events)).IsSuccess.ShouldBeTrue();
    }

    void Destroy() => _host.Dispose();
}
