// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Imports;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_resuming_an_interrupted_import_commit;

/// <summary>
/// The first attempt stores the first slice of a chunk and stops before the chunk's checkpoint. An ordinary writer and
/// another import then append, the other import's event carrying the same event index as one this import has yet to
/// commit. The resumed attempt commits the whole chunk again, slice by slice, as the commit job does after a restart.
/// </summary>
public class and_others_appended_in_between : given.an_event_sequence_with_imports
{
    readonly List<EventToAppendToStorage> _stored = [];
    ImportChunkAppendResult _firstSlice;
    ImportChunkAppendResult _secondSlice;
    IReadOnlyList<ImportEventToAppend> _chunk;

    async Task Establish()
    {
        _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(async call =>
        {
            var events = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
            _stored.AddRange(events);
            return await _log.AppendManyWithIdentities(events);
        });
        _chunk = ImportEvents();

        // The interrupted attempt: the first slice is durable, the checkpoint was never recorded.
        await _eventSequence.AppendImportChunk(_importId, 0, EventSequenceNumber.First, true, [_chunk[0], _chunk[1]]);

        await AppendOutsideTheImport(2, []);
        await AppendOutsideTheImport(3, [AnotherImportsCausation(eventIndex: 2)]);
        _stateStorage.State.SequenceNumber = 4;
    }

    async Task Because()
    {
        _firstSlice = await _eventSequence.AppendImportChunk(_importId, 0, EventSequenceNumber.First, true, [_chunk[0], _chunk[1]]);
        _secondSlice = await _eventSequence.AppendImportChunk(_importId, 0, EventSequenceNumber.First, true, [_chunk[2]]);
    }

    IEnumerable<EventToAppendToStorage> StoredByThisImport => _stored.Where(_ => _.Causation.Any(c =>
        c.Type == CausationType.Import && c.Properties["importId"] == _importId.Value.ToString("D", CultureInfo.InvariantCulture)));

    [Fact] void should_store_every_event_of_the_import_once() => StoredByThisImport.Select(_ => _.EventSourceId).ShouldContainOnly(_events[0].EventSourceId, _events[1].EventSourceId, _events[2].EventSourceId);
    [Fact] void should_report_the_events_stored_before_at_their_places() => _firstSlice.Outcomes.Select(_ => _.SequenceNumber.Value).ShouldEqual([0UL, 1UL]);
    [Fact] void should_report_them_as_appended() => _firstSlice.Outcomes.ShouldEachConformTo(_ => _.Kind == ImportEventOutcomeKind.Appended);
    [Fact] void should_not_mistake_the_other_imports_event_for_its_own() => _secondSlice.Outcomes.Single().SequenceNumber.Value.ShouldEqual(4UL);
    [Fact] void should_commit_the_rest_after_the_events_appended_in_between() => StoredByThisImport.Last().SequenceNumber.Value.ShouldEqual(4UL);
    [Fact] async Task should_never_deliver_the_import_live() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());

    Causation AnotherImportsCausation(long eventIndex) => new(
        DateTimeOffset.UtcNow,
        CausationType.Import,
        new Dictionary<string, string>
        {
            ["importId"] = ImportId.New().Value.ToString("D", CultureInfo.InvariantCulture),
            ["chunkIndex"] = "0",
            ["eventIndex"] = eventIndex.ToString(CultureInfo.InvariantCulture)
        });

    Task AppendOutsideTheImport(ulong sequenceNumber, IEnumerable<Causation> causation) => _log.Append(
        sequenceNumber,
        EventSourceType.Default,
        "someone-else",
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        CorrelationId.New(),
        causation,
        [],
        [],
        DateTimeOffset.UtcNow,
        new Dictionary<EventTypeGeneration, ExpandoObject> { [_eventType.Generation] = new() },
        new Dictionary<EventTypeGeneration, EventHash>());
}
