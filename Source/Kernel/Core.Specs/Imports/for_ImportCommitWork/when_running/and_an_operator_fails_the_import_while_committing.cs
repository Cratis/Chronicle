// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_an_operator_fails_the_import_while_committing : given.an_import_being_committed
{
    void Establish() => _import.When(_ => _.ChunkCommitted(0, Arg.Any<IEnumerable<ImportEventOutcome>>())).Do(_ =>
        _record = _record with { State = ImportState.Failed, FailureReason = "failed by an operator" });

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_commit_no_further_chunk() => _committedChunks.ShouldEqual([0]);
    [Fact] void should_leave_the_import_failed() => _record.State.ShouldEqual(ImportState.Failed);
    [Fact] async Task should_not_complete_it() => await _import.DidNotReceive().CommitCompleted();
}
