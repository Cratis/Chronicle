// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

/// <summary>
/// An earlier slice of the import was given sequence number 0 and recorded. The interrupted call stored the first event of
/// this slice at 1; the log goes on with events appended after it. The retry searches from where the commit says the slice
/// can start, and stops reading once every event of the slice is accounted for.
/// </summary>
public class and_it_searches_for_what_an_interrupted_call_stored : given.an_event_sequence_with_imports
{
    readonly List<EventSequenceNumber> _searchedFrom = [];
    int _pagesRead;

    async Task Establish()
    {
        await StoreImported(0, 0);
        await StoreImported(1, 1);
        _stateStorage.State.SequenceNumber = 2;

        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var from = call.ArgAt<EventSequenceNumber>(0);
                _searchedFrom.Add(from);
                using var read = await _log.GetRange(from, call.ArgAt<EventSequenceNumber>(1));
                var events = new List<AppendedEvent>();
                while (await read.MoveNext()) events.AddRange(read.Current);

                // Every event is a page of its own, and the log never ends, so reading past the slice would show.
                var pages = events.ConvertAll(_ => new[] { _ });
                var cursor = Substitute.For<IEventCursor>();
                cursor.MoveNext().Returns(_ => Task.FromResult(_pagesRead < 5));
                cursor.Current.Returns(_ => pages[Math.Min(_pagesRead++, pages.Count - 1)]);
                return cursor;
            });
    }

    async Task Because() => _chunkResult = await _eventSequence.AppendImportChunk(_importId, 0, new EventSequenceNumber(1), true, [ImportEvents()[1]]);

    [Fact] void should_search_from_where_the_slice_can_start() => _searchedFrom.ShouldContainOnly(new EventSequenceNumber(1));
    [Fact] void should_stop_reading_once_the_slice_is_found() => _pagesRead.ShouldEqual(1);
    [Fact] void should_report_the_stored_event_at_its_place() => _chunkResult.Outcomes.Single().SequenceNumber.ShouldEqual(new EventSequenceNumber(1));
    [Fact] void should_not_append_it_again() => _writes.ShouldEqual(0);

    Task StoreImported(ulong sequenceNumber, int index) => _log.Append(
        sequenceNumber,
        EventSourceType.Default,
        _events[index].EventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        CorrelationId.New(),
        [ImportCausation(index)],
        [],
        [],
        DateTimeOffset.UtcNow,
        new Dictionary<EventTypeGeneration, ExpandoObject> { [_eventType.Generation] = new() },
        new Dictionary<EventTypeGeneration, EventHash>());
}
