// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

/// <summary>
/// Each chunk goes to the event sequence one event at a time. The second slice of the first chunk does not settle at
/// first; its retry must search the log only after what the first slice was given, and the next chunk only after that.
/// </summary>
public class and_a_slice_after_the_first_is_retried : given.an_import_being_committed
{
    readonly List<(int Chunk, long Index, EventSequenceNumber SearchFrom, bool MayBeCommitted)> _calls = [];

    void Establish()
    {
        _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
            .Returns(call =>
            {
                var events = call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4);
                _calls.Add((call.ArgAt<int>(1), events[0].Index, call.ArgAt<EventSequenceNumber>(2), call.ArgAt<bool>(3)));
                return events[0].Index == 1 && _calls.Count(_ => _.Index == 1) == 1
                    ? ImportChunkAppendResult.NotSettled("AppendOutcomeUnknown")
                    : Appended(call.ArgAt<EventSequenceNumber>(2), events);
            });
        _work = new(_key, _import, _storage, _ => _sequence, 1, 3, retryDelay: _ => TimeSpan.Zero);
    }

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_search_the_retry_after_what_the_first_slice_was_given() => _calls.Last(_ => _.Index == 1).SearchFrom.ShouldEqual(new EventSequenceNumber(11));
    [Fact] void should_look_for_stored_events_when_retrying() => _calls.Last(_ => _.Index == 1).MayBeCommitted.ShouldBeTrue();
    [Fact] void should_start_the_next_chunk_after_the_recorded_range() => _calls.First(_ => _.Chunk == 1).SearchFrom.ShouldEqual(new EventSequenceNumber(12));
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
