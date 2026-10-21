// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_projection_catch_up_round_completes;

/// <summary>
/// Reproduces https://github.com/Cratis/Chronicle/issues/4600: A@10 arrives after the projection cursor drains,
/// then live B@12 advances the observer. Correct behavior must recover the dropped A@10 at handover.
/// </summary>
public class and_live_delivery_for_another_partition_arrived : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _cursorDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _completeRound = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["A"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(12UL));
        _subscriber.ClearReceivedCalls();
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        var round = CompleteRound();
        await _cursorDrained.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        try
        {
            await _observer.Handle("A", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
            await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);
        }
        finally
        {
            _completeRound.TrySetResult();
            await round.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    async Task CompleteRound()
    {
        _cursorDrained.TrySetResult();
        await _completeRound.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.CaughtUp(JobId.NotSet, 9UL, 9UL);
    }

    [Fact] void should_currently_skip_the_catching_up_partition_event() => _subscriber.DidNotReceive().OnNext((Key)"A", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_handle_the_other_partition_live() => _subscriber.Received(1).OnNext((Key)"B", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_currently_keep_the_live_position_past_the_dropped_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_report_one_suspected_skip() => _suspectedSkips.ShouldEqual(1);
    [Fact] void should_currently_probe_only_above_the_skipped_event() => _eventSequence.DidNotReceive().GetNextSequenceNumberGreaterOrEqualTo(Arg.Is<EventSequenceNumber>(number => number <= (EventSequenceNumber)10UL), Arg.Any<IEnumerable<EventType>>());
}
