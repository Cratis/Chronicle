// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_only_ungoverned_events_remain : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish()
    {
        _events.Clear();
        _events.Add(Event("Unrelated", 8));
    }

    async Task Because() => await Perform();

    [Fact] async Task should_clear_the_unscoped_index() => await _uniqueStorage.Received(1).ClearValues(_unique, "");
    [Fact] async Task should_report_unknown_progress_after_the_clear() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<ConstraintName>>(), ConstraintIndexRebuildOutcome.ProgressUnknown);
}
