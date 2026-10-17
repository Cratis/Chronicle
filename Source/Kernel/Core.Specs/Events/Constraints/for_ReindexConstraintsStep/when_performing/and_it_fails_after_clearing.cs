// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_it_fails_after_clearing : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish() => _uniqueStorage.Save(Arg.Any<EventSourceId>(), _unique, new EventSequenceNumber(1), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>()).Returns(Task.FromException(new IOException("Index unavailable")));

    async Task Because() => await Perform();

    [Fact] void should_fail() => _result.TryGetException(out _).ShouldBeTrue();
    [Fact] async Task should_report_the_last_completed_event() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<ConstraintName>>(), new ConstraintIndexRebuildOutcome(true, 0));
}
