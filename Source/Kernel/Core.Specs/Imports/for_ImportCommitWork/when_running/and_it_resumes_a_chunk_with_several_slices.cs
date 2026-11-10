// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

/// <summary>
/// The job stopped while committing the second chunk, after an earlier attempt had stored some of its events. On the
/// resumed attempt the first slice is given a sequence number far above the checkpoint, because ordinary appends went
/// on in between. The second slice may still have events the earlier attempt stored just after the checkpoint, so its
/// search must stay at the recorded range rather than move past what the first slice was given.
/// </summary>
public class and_it_resumes_a_chunk_with_several_slices : given.an_import_being_committed
{
    readonly List<(long Index, EventSequenceNumber SearchFrom, bool MayBeCommitted)> _calls = [];

    void Establish()
    {
        _record = _record with
        {
            State = ImportState.Committing,
            CommitStartedAt = 10,
            LastCommittedChunk = 0,
            CommittedRanges = [new ImportCommittedRange(0, 10, 11)]
        };
        _nextSequenceNumber = 50;
        _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
            .Returns(call =>
            {
                var events = call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4);
                _calls.Add((events[0].Index, call.ArgAt<EventSequenceNumber>(2), call.ArgAt<bool>(3)));
                return Appended(call.ArgAt<EventSequenceNumber>(2), events);
            });
        _work = new(_key, _import, _storage, _ => _sequence, 1, 3, retryDelay: _ => TimeSpan.Zero);
    }

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_commit_the_resumed_chunk_one_slice_at_a_time() => _calls.Select(_ => _.Index).ShouldEqual([2L, 3L]);
    [Fact] void should_search_the_first_slice_from_the_checkpoint() => _calls[0].SearchFrom.ShouldEqual(new EventSequenceNumber(12));
    [Fact] void should_keep_searching_the_second_slice_from_the_checkpoint() => _calls[1].SearchFrom.ShouldEqual(new EventSequenceNumber(12));
    [Fact] void should_look_for_stored_events_in_every_slice_of_the_resumed_chunk() => _calls.ShouldEachConformTo(_ => _.MayBeCommitted);
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
