// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_a_later_chunk_does_not_settle_at_first : given.an_import_being_committed
{
    readonly List<(int Chunk, bool MayBeCommitted)> _calls = [];

    void Establish() => _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
        .Returns(call =>
        {
            _calls.Add((call.ArgAt<int>(1), call.ArgAt<bool>(3)));
            return call.ArgAt<int>(1) == 1 && _calls.Count(_ => _.Chunk == 1) == 1
                ? ImportChunkAppendResult.NotSettled("AppendOutcomeUnknown")
                : Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4));
        });

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_not_look_for_stored_events_on_the_first_attempt_at_the_later_chunk() => _calls.First(_ => _.Chunk == 1).MayBeCommitted.ShouldBeFalse();
    [Fact] void should_look_for_stored_events_when_the_later_chunk_is_retried() => _calls.Last(_ => _.Chunk == 1).MayBeCommitted.ShouldBeTrue();
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
