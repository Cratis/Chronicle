// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Metrics;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer subscribed to one event type that records the measurements and warnings it reports about suspected skipped events.
/// </summary>
public class an_observer_with_skip_detection : an_observer_with_subscription_for_specific_event_type
{
    protected const string EventsSuspectedSkipped = "chronicle-observer-events-suspected-skipped";

    /// <summary>
    /// The recorder of the observer's metrics. It is created as a field so that it is listening before the observer
    /// is activated.
    /// </summary>
    protected readonly ObserverMetricsRecorder _metrics = new();

    protected readonly RecordingObserverLogger _recordedLogs = new();

    protected override ObserverId _observerId => _metrics.ObserverId;

    protected int _suspectedSkips => _metrics.SumOf(EventsSuspectedSkipped);

    protected override IMeter<Observer> CreateMeter() => new Meter<Observer>(ObserverMetricsRecorder.SharedMeter);

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    /// <summary>
    /// Makes partition jobs started by the jobs manager visible to subsequent persisted-job reconciliation.
    /// </summary>
    protected void PersistStartedPartitionCatchUps()
    {
        var jobs = new List<JobState>();
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(call =>
            {
                var job = new JobState { Id = JobId.New(), Status = JobStatus.Running, Request = call.Arg<CatchUpObserverPartitionRequest>() };
                jobs.Add(job);
                HasOutstandingJobs([.. jobs]);
                return Task.FromResult(Result<JobId, StartJobError>.Success(job.Id));
            });
    }

    void Destroy() => _metrics.Dispose();
}
