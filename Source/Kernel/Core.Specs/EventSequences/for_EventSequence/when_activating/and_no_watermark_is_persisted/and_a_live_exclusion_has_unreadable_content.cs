// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_live_exclusion_has_unreadable_content : given.a_catching_up_sequence
{
    protected override EventSequenceNumber InitialNext => 4;

    async Task Establish()
    {
        await Seed(0, "unreadable", email: "old");
        await Seed(1, "other", email: "other");
        await Seed(3, "unreadable", new(_eventType.Id, new EventTypeGeneration(2)), "live");
        await Hold("unreadable", "live");
        _stateStorage.State.ConstraintIndexCatchUpBelow = 3;
        await _log.SaveState(_stateStorage.State);
        await Reactivate();
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _eventTypesStorage.GetFor(_eventType.Id, new EventTypeGeneration(2)).Returns(Task.FromException<EventTypeSchema>(new FormatException("unreadable schema")));
    }

    Task Because() => Tick();

    [Fact] void should_finish_instead_of_retrying_the_exclusion_forever() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] async Task should_conservatively_preserve_the_live_sources_claim() => (await Values("unreadable")).ShouldContainOnly(Hash("live"));
    [Fact] async Task should_repair_other_historical_sources() => (await Values("other")).ShouldContainOnly(Hash("other"));
}
