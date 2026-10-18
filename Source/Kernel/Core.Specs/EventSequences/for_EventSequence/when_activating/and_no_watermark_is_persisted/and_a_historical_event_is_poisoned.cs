// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_historical_event_is_poisoned : given.a_catching_up_sequence
{
    int _schemaFailures;
    int _writesBeforeFifth;

    async Task Establish()
    {
        await Seed(0, "first", email: "a");
        await Seed(1, "poisoned", new(_eventType.Id, new EventTypeGeneration(2)), "b");
        await Seed(2, "last", email: "c");
        _eventTypesStorage.GetFor(_eventType.Id, new EventTypeGeneration(2)).Returns(_ =>
        {
            _schemaFailures++;
            return Task.FromException<EventTypeSchema>(new FormatException("bad schema"));
        });
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Tick();
            _clock.Advance(TimeSpan.FromSeconds(60));
        }
        _writesBeforeFifth = _claimWrites;
    }

    async Task Because()
    {
        await Tick();
        _clock.Advance(TimeSpan.FromSeconds(60));
        await Tick();
    }

    [Fact] void should_require_five_identical_failures_before_settling() => _schemaFailures.ShouldEqual(5);
    [Fact] void should_not_write_an_unfinished_fold() => _writesBeforeFifth.ShouldEqual(0);
    [Fact] async Task should_repair_the_preceding_valid_key() => (await Values("first")).ShouldContainOnly(Hash("a"));
    [Fact] async Task should_repair_the_following_valid_key() => (await Values("last")).ShouldContainOnly(Hash("c"));
    [Fact] async Task should_not_invent_the_poison_events_effect() => (await Values("poisoned")).ShouldBeEmpty();
    [Fact] void should_complete_the_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
