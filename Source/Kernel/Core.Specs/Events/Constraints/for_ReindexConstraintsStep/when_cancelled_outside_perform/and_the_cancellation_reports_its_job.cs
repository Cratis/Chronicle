// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_cancelled_outside_perform;

public class and_the_cancellation_reports_its_job : given.a_handoff_reindex_step
{
    async Task Because() => await (ValueTask<object?>)typeof(ReindexConstraintsStep).GetMethod("CreateCancelledResultFromCurrentState", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_step, [_state])!;

    [Fact]
    async Task should_report_its_job_without_an_attempt() => await _sequence.Received(1).ConstraintIndexRebuilt(
        Arg.Is<ConstraintIndexRebuildRunId>(run => run.JobId == _jobId && run.Attempt == Guid.Empty),
        Arg.Any<IEnumerable<ConstraintName>>(),
        Arg.Any<ConstraintIndexRebuildOutcome>());
}
