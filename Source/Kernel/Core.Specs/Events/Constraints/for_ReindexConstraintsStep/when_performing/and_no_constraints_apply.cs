// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_no_constraints_apply : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish() => _state.Changes = [];

    async Task Because() => await Perform();

    [Fact] async Task should_not_start_a_handoff() => await _sequence.DidNotReceive().ConstraintIndexRebuildStarting(Arg.Any<ConstraintIndexRebuildRunId>(), Arg.Any<IEnumerable<ConstraintName>>());
    [Fact] async Task should_not_report_a_handoff() => await _sequence.DidNotReceive().ConstraintIndexRebuilt(Arg.Any<ConstraintIndexRebuildRunId>(), Arg.Any<IEnumerable<ConstraintName>>(), Arg.Any<ConstraintIndexRebuildOutcome>());
}
