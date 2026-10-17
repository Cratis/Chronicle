// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_cancelled_outside_perform;

public class and_the_step_has_not_started : given.a_handoff_reindex_step
{
    async Task Because() => await (ValueTask<object?>)typeof(ReindexConstraintsStep).GetMethod("CreateCancelledResultFromCurrentState", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_step, [_state])!;

    [Fact] async Task should_report_unknown_progress_with_the_stable_identity() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Is<IEnumerable<ConstraintName>>(names => names.SequenceEqual(new[] { _unique.Name })), ConstraintIndexRebuildOutcome.ProgressUnknown);
}
