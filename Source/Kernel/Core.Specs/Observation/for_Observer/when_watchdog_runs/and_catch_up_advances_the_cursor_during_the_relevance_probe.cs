// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_catch_up_advances_the_cursor_during_the_relevance_probe : given.an_observer_behind_on_a_relevant_event
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
    readonly TaskCompletionSource _probeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<EventSequenceNumber, GetSequenceNumberError>> _probeResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverLagSample _sample;
    ulong _advancedCursor;

    void Establish() => _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(_nextSequenceNumber, Arg.Any<IEnumerable<EventType>>())
        .Returns(_ =>
        {
            _probeStarted.TrySetResult();
            return _probeResult.Task;
        });

    async Task Because()
    {
        var watchdog = _observer.RunWatchdogAsync();
        try
        {
            await _probeStarted.Task.WaitAsync(_timeout, _timeProvider);
            await _observer.CaughtUp(JobId.New(), EventSequenceNumber.Unavailable, 11UL).WaitAsync(_timeout, _timeProvider);
            _advancedCursor = (await _observer.GetState()).NextEventSequenceNumber.Value;
        }
        finally
        {
            _probeResult.TrySetResult((EventSequenceNumber)10UL);
            await watchdog.WaitAsync(_timeout, _timeProvider);
        }

        _sample = (await _observer.GetInformation()).LagSample;
    }

    [Fact] void should_sample_the_advanced_cursor() => _sample.NextEventSequenceNumber.ShouldEqual(_advancedCursor);
    [Fact] void should_not_report_a_pending_event_behind_the_cursor() => _sample.RelevantPendingEventSequenceNumber.ShouldBeNull();
}
