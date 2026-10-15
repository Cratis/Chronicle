// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_update_fails;

public class and_the_store_hangs_for_a_batch_of_1000_events : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendManyResult _batch;
    AppendResult _beforeConfirmation;
    int _attempts;
    bool _hang = true;

    void Establish()
    {
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call => _log.AppendMany(call.Arg<IEnumerable<EventToAppendToStorage>>()));
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _attempts++;
                return _hang ? _write.Task : _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<CancellationToken>(5));
            });
    }

    async Task Because()
    {
        var events = Enumerable.Range(0, 1000).Select(_ => ValidatedEvent()).Select(e =>
        {
            var content = Content();
            var @event = e.Event with { EventType = _eventType, EventSourceId = _eventSourceId };
            return (@event, content, e.CompliantContent, _currentValidation.Establish(_eventSourceId, _eventType.Id, content));
        }).ToArray();
        var append = _eventSequence.AppendManyToStorage(events, CorrelationId.New(), [], []);
        _clock.Advance(TimeSpan.FromSeconds(2));
        _batch = await append.WaitAsync(TimeSpan.FromSeconds(2), TimeProvider.System);
        _hang = false;
        _write.TrySetResult();
        await given.ConstraintRepairTimer.Tick(_silo);
        _eventSourceId = EventSourceId.New();
        _beforeConfirmation = await AppendAnEvent();
        await given.ConstraintRepairTimer.Tick(_silo);
        _result = await AppendAnEvent();
    }

    [Fact] void should_succeed_for_all_durable_events_within_the_turn_budget() => _batch.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_only_the_final_claim_during_repair() => _attempts.ShouldEqual(2);
    [Fact] void should_refuse_until_the_timer_confirms_the_abandoned_write() => _beforeConfirmation.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_release_the_constraint_after_confirmation() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_validate_against_the_repaired_claim() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] async Task should_persist_the_repaired_batch_watermark() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1000);
}
