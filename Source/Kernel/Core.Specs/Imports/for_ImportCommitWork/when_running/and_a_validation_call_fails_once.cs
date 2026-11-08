// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_a_validation_call_fails_once : given.an_import_being_committed
{
    int _calls;

    void Establish() => _sequence.ValidateImportChunk(_key.ImportId, Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>()).Returns(call =>
    {
        _validationFirsts.Add(call.ArgAt<bool>(1));
        return ++_calls == 2
            ? Task.FromException<ImportChunkValidationResult>(new TimeoutException("The grain call timed out."))
            : Task.FromResult(new ImportChunkValidationResult([]));
    });

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_start_validating_over_from_the_first_chunk() => _validationFirsts.ShouldEqual([true, false, true, false]);
    [Fact] void should_complete_the_import() => _record.State.ShouldEqual(ImportState.Completed);
}
