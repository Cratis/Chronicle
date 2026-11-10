// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_a_chunk_does_not_settle_at_first : given.an_import_being_committed
{
    int _attempts;

    void Establish() => _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
        .Returns(call => ++_attempts == 1
            ? ImportChunkAppendResult.NotSettled("AppendOutcomeUnknown")
            : Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4)));

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_try_the_chunk_again() => _attempts.ShouldEqual(3);
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
