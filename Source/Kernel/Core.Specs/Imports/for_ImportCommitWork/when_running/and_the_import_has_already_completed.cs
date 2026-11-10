// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_the_import_has_already_completed : given.an_import_being_committed
{
    void Establish() => _record = _record with { State = ImportState.Completed };

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_commit_nothing() => _committedChunks.ShouldBeEmpty();
    [Fact] async Task should_not_touch_the_event_sequence() => await _sequence.DidNotReceiveWithAnyArgs().AppendImportChunk(default!, default, default!, default, default!);
    [Fact] async Task should_leave_the_import_as_it_is() => await _import.DidNotReceiveWithAnyArgs().Fail(default!);
}
