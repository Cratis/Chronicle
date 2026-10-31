// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_catch_up_is_still_unreachable_after_the_warning_budget : given.an_unreachable_observer
{
    readonly TaskCompletionSource _catchupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly MeterListener _listener = new();
    int _budgetWarnings;
    int _budgetMeasurements;
    int _attempts;
    int _attemptsAtDeadline;
    bool _canStartCatchup;
    TimeSpan _delayAfterDeadline;

    void Establish()
    {
        _recordedLogs.ThrowWhen = message =>
        {
            if (message.StartsWith("Catch-up trigger budget exceeded", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _budgetWarnings);
            }
            return false;
        };
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, QueueMetricsRecorder.SharedMeter) &&
                instrument.Name == "chronicle-appended-events-queue-catchup-trigger-budget-exceeded")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) => Interlocked.Add(ref _budgetMeasurements, measurement));
        _listener.Start();
    }

    async Task Because()
    {
        await ExhaustSuspectedSkipWarnings();
        ConfigureFailingCatchup();
        await DispatchFirstEvent();
        await AdvancePastDeadline();
        await _queue.Enqueue([_laterEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
        await _clock.AdvanceNextDelay();
        _delayAfterDeadline = await _clock.WaitForNextDelay();
        Volatile.Write(ref _canStartCatchup, true);
        await _clock.AdvanceNextDelay();
        await _catchupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task ExhaustSuspectedSkipWarnings()
    {
        // The warning limiter admits ten entries per real-time minute; the retry clock is independent of it.
        for (var failure = 0; failure < 11; failure++)
        {
            await DispatchFirstEvent();
            await _queue.Subscribe(_observerKey, [_eventType]);
        }
        _observer.ClearReceivedCalls();
    }

    void ConfigureFailingCatchup() => _observer.CatchUp().Returns(_ =>
    {
        Interlocked.Increment(ref _attempts);
        if (!Volatile.Read(ref _canStartCatchup))
        {
            return Task.FromException(new InvalidOperationException("catch-up still unreachable"));
        }
        _catchupStarted.TrySetResult();
        return Task.CompletedTask;
    });

    async Task AdvancePastDeadline()
    {
        while (_clock.GetElapsedTime(0) < TimeSpan.FromMinutes(2))
        {
            await _clock.AdvanceNextDelay();
        }
        await _clock.WaitForNextDelay();
        _attemptsAtDeadline = Volatile.Read(ref _attempts);
    }

    void Destroy() => _listener.Dispose();

    [Fact] void should_warn_once_even_when_suspected_skip_warnings_are_throttled() => _budgetWarnings.ShouldEqual(1);
    [Fact] void should_have_exhausted_the_suspected_skip_warning_limit() => _recordedLogs.SuspectedUndeliveredWarnings.Count().ShouldEqual(10);
    [Fact] void should_count_the_exceeded_budget_once() => _budgetMeasurements.ShouldEqual(1);
    [Fact] void should_continue_retrying_past_the_deadline_until_catchup_starts() => (_attempts > _attemptsAtDeadline).ShouldBeTrue();
    [Fact] void should_cap_the_retry_delay() => _delayAfterDeadline.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_not_deliver_live_events_past_the_hole() =>
        _observer.DidNotReceive().Handle((Key)_laterEvent.Context.EventSourceId, Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] async Task should_keep_the_observer_spilled_until_it_resubscribes() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
}
