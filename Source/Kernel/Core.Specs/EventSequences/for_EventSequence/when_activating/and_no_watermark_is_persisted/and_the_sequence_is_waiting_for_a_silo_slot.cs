// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_sequence_is_waiting_for_a_silo_slot : given.a_catching_up_sequence
{
    readonly object[] _owners = [new(), new(), new(), new()];

    void Establish()
    {
        var slots = ConstraintIndexCatchUpSlots.For(_storage);
        foreach (var owner in _owners) slots.TryEnter(owner).ShouldBeTrue();
    }

    async Task Because()
    {
        await AppendAnEvent();
        await Tick();
    }

    void Destroy()
    {
        var slots = ConstraintIndexCatchUpSlots.For(_storage);
        foreach (var owner in _owners) slots.Complete(owner);
    }

    [Fact] void should_not_accumulate_exclusions_outside_the_silo_memory_limit() => _eventSequence.ConstraintIndexCatchUpExcludedKeyCount.ShouldEqual(0);
    [Fact] void should_not_start_reading_history_without_a_slot() => _rangeReads.ShouldEqual(0);
}
