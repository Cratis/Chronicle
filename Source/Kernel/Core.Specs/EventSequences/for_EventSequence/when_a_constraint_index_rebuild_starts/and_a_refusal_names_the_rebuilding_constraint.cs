// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_a_refusal_names_the_rebuilding_constraint : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await Seed(2, "other-source", new("OtherEvent", EventTypeGeneration.First));
        _stateStorage.State.SequenceNumber = 3;
        _stateStorage.State.ConstraintIndexCatchUpBelow = 3;
        _stateStorage.State.ConstraintIndexCatchUpRefusing = [_uniqueDefinition.Name];
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
    }

    async Task Because()
    {
        await Tick();
        await Tick();
    }

    [Fact] void should_preserve_the_rebuilding_constraint_refusal() => _stateStorage.State.ConstraintIndexCatchUpRefusing.ShouldContain(_uniqueDefinition.Name);
    [Fact] void should_catch_up_the_other_constraint() => _claimWrites.ShouldBeGreaterThan(0);
}
