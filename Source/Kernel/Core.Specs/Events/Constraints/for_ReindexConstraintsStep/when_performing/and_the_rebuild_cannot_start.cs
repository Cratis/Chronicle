// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_the_rebuild_cannot_start : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish() => _sequence.ConstraintIndexRebuildStarting(Arg.Any<ConstraintIndexRebuildRunId>(), Arg.Any<IEnumerable<ConstraintName>>()).Returns(call =>
    {
        _runId = call.Arg<ConstraintIndexRebuildRunId>();
        return Task.FromException(new IOException("Sequence unavailable"));
    });

    async Task Because() => await Perform();

    [Fact] void should_fail() => _result.TryGetException(out _).ShouldBeTrue();
    [Fact] async Task should_not_clear() => await _uniqueStorage.DidNotReceive().ClearValues(_unique, Arg.Any<string>());
    [Fact] async Task should_report_nothing_written() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<ConstraintName>>(), ConstraintIndexRebuildOutcome.NothingWritten);
}
