// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_live_boundary_raise_cannot_persist : given.a_catching_up_sequence
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendManyResult _batch;
    bool _snapshotBeforeLiveUpdates;
    int _liveWrites;

    async Task Establish()
    {
        await Seed(0, "first", email: "a");
        await Seed(1, "second", email: "b");
        await Seed(2, "third", email: "c");
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                if (call.ArgAt<EventSequenceNumber>(2) >= (EventSequenceNumber)3) _liveWrites++;
                await _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4));
                if (call.ArgAt<EventSequenceNumber>(2) < (EventSequenceNumber)3) _clock.Advance(TimeSpan.FromSeconds(2));
            });
        await Tick();
        _eventSequence.ConstraintIndexCatchUpKeyLimit = 1;
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call => _log.AppendMany(call.Arg<IEnumerable<EventToAppendToStorage>>()));
        _snapshots.BeforeWrite = () =>
        {
            if (_liveWrites < 3)
            {
                // Deliberately slow boundary storage: the old live-path deadline would abandon it.
                _snapshotBeforeLiveUpdates = true;
                _clock.Advance(TimeSpan.FromMilliseconds(250));
                return _release.Task;
            }
            return Task.FromException(new given.SimulatedStateWriteError());
        };
    }

    async Task Because()
    {
        var events = Enumerable.Range(0, 3).Select(_ =>
        {
            var e = ValidatedEvent();
            var @event = e.Event with { EventType = _eventType };
            var content = Content(@event.EventSourceId.Value);
            return (@event, content, e.CompliantContent, _currentValidation.Establish(@event.EventSourceId, _eventType.Id, content));
        }).ToList();
        _batch = await _eventSequence.AppendManyToStorage(events, CorrelationId.New(), [], []);
    }

    void Destroy() => _release.TrySetResult();

    [Fact] void should_report_the_durable_batch_as_successful() => _batch.IsSuccess.ShouldBeTrue();
    [Fact] void should_apply_every_live_update_in_the_batch() => _liveWrites.ShouldEqual(3);
    [Fact] void should_not_attempt_boundary_persistence_before_the_live_updates() => _snapshotBeforeLiveUpdates.ShouldBeFalse();
}
