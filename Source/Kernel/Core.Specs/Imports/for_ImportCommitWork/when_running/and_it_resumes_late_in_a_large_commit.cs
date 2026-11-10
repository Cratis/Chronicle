// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

/// <summary>
/// The commit started at 10 and the first chunk was recorded as 300 000 to 300 001 before the restart. The resumed
/// chunk may be partly in the log, but never below what was recorded, so the search starts after the checkpoint.
/// </summary>
public class and_it_resumes_late_in_a_large_commit : given.an_import_being_committed
{
    readonly List<(int Chunk, EventSequenceNumber SearchFrom, bool MayBeCommitted)> _calls = [];

    void Establish()
    {
        _record = _record with
        {
            State = ImportState.Committing,
            CommitStartedAt = 10,
            LastCommittedChunk = 0,
            CommittedRanges = [new ImportCommittedRange(0, 300_000, 300_001)]
        };
        _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
            .Returns(call =>
            {
                _calls.Add((call.ArgAt<int>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<bool>(3)));
                return Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4));
            });
    }

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_only_commit_the_chunk_after_the_checkpoint() => _calls.Select(_ => _.Chunk).ShouldContainOnly(1);
    [Fact] void should_look_for_what_the_interrupted_attempt_stored() => _calls.Single().MayBeCommitted.ShouldBeTrue();
    [Fact] void should_search_only_after_the_checkpoint() => _calls.Single().SearchFrom.ShouldEqual(new EventSequenceNumber(300_002));
}
