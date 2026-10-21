// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_source_moved_after_the_boundary : given.a_catching_up_sequence
{
    async Task Establish()
    {
        await Seed(0, email: "historical");
        await Seed(1, email: "historical");
        await Seed(2, email: "historical");
        await AppendAnEvent();
    }

    async Task Because()
    {
        await Tick();
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_keep_the_live_value_protected() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_not_write_the_historical_value() => _claimWrites.ShouldEqual(1);
    [Fact] void should_complete_the_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
