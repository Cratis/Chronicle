// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_update_budget_expires_before_an_unrelated_constraint : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    bool _expired;

    async Task Establish()
    {
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _unique.When(storage => storage.Save(Arg.Any<EventSourceId>(), _uniqueDefinition, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (_expired) return;
                _expired = true;
                _clock.Advance(TimeSpan.FromSeconds(2));
            });
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
        _claimWrites = 0;
        _unique.ClearReceivedCalls();
        _eventType = new("OtherEvent", EventTypeGeneration.First);
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_admit_the_other_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_the_other_claim_exactly_once() => _claimWrites.ShouldEqual(1);
    [Fact] async Task should_not_repair_the_unrelated_claim() => await _unique.DidNotReceive().Save(Arg.Any<EventSourceId>(), _uniqueDefinition, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
