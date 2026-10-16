// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_resuming;

public class and_each_attempt_has_its_own_identity : given.a_handoff_reindex_step
{
    readonly List<ConstraintIndexRebuildRunId> _starts = [];
    readonly List<ConstraintIndexRebuildRunId> _ends = [];

    void Establish()
    {
        _sequence.ConstraintIndexRebuildStarting(Arg.Any<ConstraintIndexRebuildRunId>(), Arg.Any<IEnumerable<ConstraintName>>()).Returns(call =>
        {
            _starts.Add(call.Arg<ConstraintIndexRebuildRunId>());
            return Task.CompletedTask;
        });
        _sequence.ConstraintIndexRebuilt(Arg.Any<ConstraintIndexRebuildRunId>(), Arg.Any<IEnumerable<ConstraintName>>(), Arg.Any<ConstraintIndexRebuildOutcome>()).Returns(call =>
        {
            _ends.Add(call.Arg<ConstraintIndexRebuildRunId>());
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await Perform();
        await Perform();
    }

    [Fact] void should_generate_a_nonce_for_each_attempt() => _starts.TrueForAll(run => run.Attempt != Guid.Empty).ShouldBeTrue();
    [Fact] void should_keep_the_step_identity() => _starts.Select(run => run.Value).Distinct().ShouldContainOnly(_runId.Value);
    [Fact] void should_not_reuse_a_finished_attempt_identity() => _starts.Distinct().Count().ShouldEqual(2);
    [Fact] void should_report_the_same_identity_at_each_terminal() => _ends.ShouldEqual(_starts);
}
