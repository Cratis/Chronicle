// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_resuming_after_a_checkpoint : given.an_import_being_committed
{
    void Establish() => _record = _record with
    {
        State = ImportState.Committing,
        CommitStartedAt = 10,
        LastCommittedChunk = 0,
        CommittedRanges = [new ImportCommittedRange(0, 10, 11)]
    };

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_not_validate_again() => _validationFirsts.ShouldBeEmpty();
    [Fact] void should_commit_only_the_chunks_after_the_checkpoint() => _committedChunks.ShouldEqual([1]);
    [Fact] void should_look_for_an_interrupted_commit_after_the_checkpoint() => _committedFrom.Single().ShouldEqual(new EventSequenceNumber(12));
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
