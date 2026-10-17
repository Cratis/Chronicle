// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_an_unfinished_owner : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    bool _completed;
    bool _clearedAmbient;
    bool _discardedEvents;

    async Task Because()
    {
        using (var owner = Refuse())
        {
            await _sequence.Transactional.Append("source", "discarded");
            ClearCalls();
        }
        _completed = _unit.IsCompleted;
        _clearedAmbient = !_manager.HasCurrent;
        _discardedEvents = !_unit.GetEvents().Any();
        _error = await Catch.Exception(() => _sequence.Append("source", "allowed"));
    }

    [Fact] void should_roll_back_on_owner_disposal() => _completed.ShouldBeTrue();
    [Fact] void should_clear_the_ambient_unit() => _clearedAmbient.ShouldBeTrue();
    [Fact] void should_discard_uncommitted_events() => _discardedEvents.ShouldBeTrue();
    [Fact] void should_restore_admission() => _error.ShouldBeNull();
    [Fact] void should_only_send_the_post_disposal_append() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
}
