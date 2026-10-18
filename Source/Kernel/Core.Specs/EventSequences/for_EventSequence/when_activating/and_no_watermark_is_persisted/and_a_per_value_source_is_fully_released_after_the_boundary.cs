// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_per_value_source_is_fully_released_after_the_boundary : given.a_catching_up_sequence
{
    protected override UniqueConstraintMode UniqueMode => UniqueConstraintMode.PerValue;
    EventSourceId _source;

    async Task Establish()
    {
        _source = _eventSourceId;
        await Seed(0, email: "first");
        await Seed(1, email: "second");
        await Seed(2, email: "third");
        _eventType = new("Removed", EventTypeGeneration.First);
        await AppendAnEvent();
    }

    Task Because() => Tick();

    [Fact] async Task should_not_resurrect_any_historical_value() => (await Values(_source.Value)).ShouldBeEmpty();
    [Fact] void should_never_claim_any_released_value() => _claimWrites.ShouldEqual(0);
    [Fact] void should_finish_the_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
