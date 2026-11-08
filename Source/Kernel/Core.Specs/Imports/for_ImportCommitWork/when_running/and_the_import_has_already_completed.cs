// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_the_import_has_already_completed : given.an_import_being_committed
{
    void Establish() => _record = _record with { State = ImportState.Completed };

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_commit_nothing() => _committedChunks.ShouldBeEmpty();
    [Fact] async Task should_make_sure_no_observer_is_left_held_back() => await _sequence.Received(1).CompleteImportCommit(_key.ImportId);
}
