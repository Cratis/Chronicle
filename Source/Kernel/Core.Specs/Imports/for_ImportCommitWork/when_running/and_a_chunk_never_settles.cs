// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_a_chunk_never_settles : given.an_import_being_committed
{
    void Establish() => _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
        .Returns(ImportChunkAppendResult.NotSettled("AppendOutcomeUnknown"));

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_fail_the_import() => _record.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_say_why() => _record.FailureReason.ShouldContain("AppendOutcomeUnknown");
    [Fact] async Task should_look_for_events_an_earlier_attempt_stored_on_every_attempt() => await _sequence.Received(3).AppendImportChunk(_key.ImportId, 0, Arg.Any<EventSequenceNumber>(), true, Arg.Any<IReadOnlyList<ImportEventToAppend>>());
    [Fact] async Task should_try_as_many_times_as_allowed() => await _sequence.ReceivedWithAnyArgs(3).AppendImportChunk(default!, default, default!, default, default!);
}
