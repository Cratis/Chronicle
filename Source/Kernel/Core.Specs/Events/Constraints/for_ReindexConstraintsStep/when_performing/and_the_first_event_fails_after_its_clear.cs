// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_the_first_event_fails_after_its_clear : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish() => _uniqueStorage.Save(Arg.Any<EventSourceId>(), _unique, EventSequenceNumber.First, Arg.Any<UniqueConstraintValue>(), Arg.Any<string>()).Returns(Task.FromException(new IOException("Index unavailable")));

    async Task Because() => await Perform();

    [Fact] async Task should_report_unknown_progress() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<ConstraintName>>(), ConstraintIndexRebuildOutcome.ProgressUnknown);
}
