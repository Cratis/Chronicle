// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_validation_calls_keep_failing : given.an_import_being_committed
{
    void Establish() => _sequence.ValidateImportChunk(_key.ImportId, Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>()).Returns(call =>
    {
        _validationFirsts.Add(call.ArgAt<bool>(1));
        return Task.FromException<ImportChunkValidationResult>(new TimeoutException("The grain call timed out."));
    });

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_try_as_many_times_as_it_may() => _validationFirsts.Count.ShouldEqual(3);
    [Fact] void should_fail_the_import() => _record.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_tell_why() => _record.FailureReason!.ShouldContain("The grain call timed out.");
    [Fact] async Task should_release_the_claims() => await _sequence.Received().EndImportValidation(_key.ImportId);
    [Fact] async Task should_commit_nothing() => await _sequence.DidNotReceive().AppendImportChunk(Arg.Any<ImportId>(), Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>());
}
