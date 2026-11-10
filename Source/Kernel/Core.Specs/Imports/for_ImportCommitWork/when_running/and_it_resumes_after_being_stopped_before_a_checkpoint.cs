// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_it_resumes_after_being_stopped_before_a_checkpoint : given.an_import_being_committed
{
    readonly List<(int Chunk, bool MayBeCommitted)> _calls = [];
    Exception? _stopped;
    bool _stopOnce = true;

    void Establish()
    {
        _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
            .Returns(call =>
            {
                _calls.Add((call.ArgAt<int>(1), call.ArgAt<bool>(3)));
                return Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4));
            });

        // The silo stops after the first chunk is durable and before its checkpoint is recorded.
        _import.ChunkCommitted(0, Arg.Any<IEnumerable<ImportEventOutcome>>()).Returns(_ =>
        {
            if (!_stopOnce) return Task.CompletedTask;
            _stopOnce = false;
            _committedChunks.Clear();
            _record = _record with { LastCommittedChunk = -1, CommittedRanges = [] };
            return Task.FromException(new OperationCanceledException());
        });
        _work = new(_key, _import, _storage, _ => _sequence, 1, 3, retryDelay: _ => TimeSpan.Zero);
    }

    async Task Because()
    {
        _stopped = await Catch.Exception(() => _work.Run(CancellationToken.None));
        await _work.Run(CancellationToken.None);
    }

    [Fact] void should_stop_the_first_run_as_resumable() => _stopped.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_have_the_resumed_run_look_for_what_the_first_one_stored() => _calls.Where(_ => _.Chunk == 0).Skip(2).ShouldEachConformTo(_ => _.MayBeCommitted);
    [Fact] void should_not_search_for_the_chunk_the_first_run_never_reached() => _calls.Where(_ => _.Chunk == 1).ShouldEachConformTo(_ => !_.MayBeCommitted);
    [Fact] void should_record_each_chunk_once() => _committedChunks.ShouldEqual([0, 1]);
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
