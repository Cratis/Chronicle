// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_it_completes : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    async Task Because() => await Perform();

    [Fact] void should_start_before_clearing() => _calls.ShouldEqual(["starting", "clear"]);
    [Fact] async Task should_report_the_last_processed_event() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Is<IEnumerable<ConstraintName>>(names => names.SequenceEqual(new[] { _unique.Name })), new ConstraintIndexRebuildOutcome(true, 1));
}
