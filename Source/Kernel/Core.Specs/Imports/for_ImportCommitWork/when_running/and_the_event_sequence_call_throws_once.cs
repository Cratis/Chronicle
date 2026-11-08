// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_the_event_sequence_call_throws_once : given.an_import_being_committed
{
    int _attempts;

    void Establish() => _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
        .Returns(call => ++_attempts == 1
            ? Task.FromException<ImportChunkAppendResult>(new TimeoutException("the call timed out"))
            : Task.FromResult(Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4))));

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_try_the_chunk_again_rather_than_fail() => _record.State.ShouldEqual(ImportState.Completed);
    [Fact] async Task should_look_for_what_the_failed_call_may_have_stored() => await _sequence.Received(2).AppendImportChunk(_key.ImportId, 0, Arg.Any<EventSequenceNumber>(), true, Arg.Any<IReadOnlyList<ImportEventToAppend>>());
}
