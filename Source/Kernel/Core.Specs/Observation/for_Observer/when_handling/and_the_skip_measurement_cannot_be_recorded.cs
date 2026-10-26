// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Recording the new measurement fails. The observer carries on exactly as it does without it, and the warning is still logged.
/// </summary>
public class and_the_skip_measurement_cannot_be_recorded : given.an_observer_with_skip_detection
{
    Exception _exception;
    MeterListener _failingListener;
    bool _measurementWasAttempted;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();

        // A listener that fails is how a metric exporter failing shows up at the call that records the measurement.
        _failingListener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, given.ObserverMetricsRecorder.SharedMeter) && instrument.Name == WellKnownMetricNames.ObserverSuspectedSkippedEvents)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _failingListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (tags.ToArray().Any(_ => _.Key == WellKnownTelemetryAttributes.ObserverId && _observerId.Equals(_.Value)))
            {
                _measurementWasAttempted = true;
                throw new InvalidOperationException("The measurement listener failed");
            }
        });
        _failingListener.Start();
    }

    async Task Because()
    {
        try
        {
            await _observer.FailedPartitionRecovered("P", 103UL);
            await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_still_skip_the_queued_event() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_still_advance_past_the_queued_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)104UL);
    [Fact] void should_still_clear_the_recovered_failure() => _failedPartitionsStorage.State.IsFailed((Key)"P").ShouldBeFalse();
    [Fact] void should_have_attempted_the_measurement() => _measurementWasAttempted.ShouldBeTrue();
    [Fact] void should_still_log_the_warning() => _recordedLogs.SuspectedSkipWarnings.Count().ShouldEqual(1);

    void Destroy() => _failingListener.Dispose();
}
