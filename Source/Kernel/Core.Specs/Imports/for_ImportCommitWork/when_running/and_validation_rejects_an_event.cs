// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_validation_rejects_an_event : given.an_import_being_committed
{
    void Establish() => _sequence.ValidateImportChunk(_key.ImportId, Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
        .Returns(call => call.ArgAt<bool>(1)
            ? new ImportChunkValidationResult([new ImportRejection(1, "event-type+1", ["unique: taken"])])
            : new ImportChunkValidationResult([]));

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] async Task should_reject_the_import_with_the_event() => await _import.Received(1).ValidationRejected(Arg.Is<IEnumerable<ImportRejection>>(_ => _.Single().Index == 1));
    [Fact] async Task should_commit_nothing() => await _sequence.DidNotReceiveWithAnyArgs().AppendImportChunk(default!, default, default!, default, default!);
    [Fact] void should_leave_the_import_failed() => _record.State.ShouldEqual(ImportState.Failed);
}
