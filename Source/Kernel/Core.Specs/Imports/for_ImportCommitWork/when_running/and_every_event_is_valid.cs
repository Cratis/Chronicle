// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_every_event_is_valid : given.an_import_being_committed
{
    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_validate_every_chunk_in_order_starting_the_claims_once() => _validationFirsts.ShouldEqual([true, false]);
    [Fact] async Task should_forget_the_claims_after_validating() => await _sequence.Received(1).EndImportValidation(_key.ImportId);
    [Fact] async Task should_start_the_commit_after_the_tail() => await _import.Received(1).CommitStarted(new EventSequenceNumber(10));
    [Fact] void should_commit_the_chunks_in_staging_order() => _committedChunks.ShouldEqual([0, 1]);
    [Fact] void should_tell_the_second_chunk_where_the_first_ended() => _committedFrom[1].ShouldEqual(new EventSequenceNumber(12));
    [Fact] async Task should_release_the_held_observers_once() => await _sequence.Received(1).CompleteImportCommit(_key.ImportId);
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
