// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_it_is_stopped_while_validating : given.an_import_being_committed
{
    readonly CancellationTokenSource _stop = new();
    Exception? _error;

    void Establish() => _sequence.ValidateImportChunk(_key.ImportId, Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>()).Returns(_ =>
    {
        _stop.Cancel();
        return new ImportChunkValidationResult([]);
    });

    async Task Because() => _error = await Catch.Exception(() => _work.Run(_stop.Token));

    void Destroy() => _stop.Dispose();

    [Fact] void should_stop_with_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_leave_the_import_validating_to_be_resumed() => _record.State.ShouldEqual(ImportState.Validating);
    [Fact] async Task should_release_the_validation_claims() => await _sequence.Received(1).EndImportValidation(_key.ImportId);
}
