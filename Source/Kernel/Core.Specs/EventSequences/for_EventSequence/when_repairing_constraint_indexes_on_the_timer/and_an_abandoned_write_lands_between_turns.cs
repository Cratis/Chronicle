// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_an_abandoned_write_lands_between_turns : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly TaskCompletionSource _land = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _landed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EventSourceId _originalSource;
    bool _hang = true;

    async Task Establish()
    {
        _originalSource = _eventSourceId;
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (!_hang) return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<CancellationToken>(5));
                _clock.Advance(TimeSpan.FromSeconds(2));
                return LandLater(call.ArgAt<UniqueConstraintValue>(3));
            });
        await AppendAnEvent();
        _hang = false;
        var claimType = _eventType;
        _eventType = new("Removed", EventTypeGeneration.First);
        await AppendAnEvent();
        _eventType = claimType;
        await given.ConstraintRepairTimer.Tick(_silo);
        _land.TrySetResult();
        await _landed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task LandLater(UniqueConstraintValue value)
    {
        // This store deliberately ignores cancellation; completion is controlled by a signal, not a delay.
        await _land.Task;
        await _claims.Save(_originalSource, _uniqueDefinition, 0, value);
        _landed.TrySetResult();
    }

    async Task Because()
    {
        await given.ConstraintRepairTimer.Tick(_silo);
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] async Task should_fold_the_late_claim_back_to_the_stored_removal() => (await _claims.GetFor(_originalSource, _uniqueDefinition)).ShouldBeEmpty();
    [Fact] void should_admit_only_after_confirmation() => _result.IsSuccess.ShouldBeTrue();
}
