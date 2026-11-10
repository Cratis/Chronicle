// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobGauges.when_reporting_jobs;

public class and_the_job_was_released : given.the_observer_job_gauges
{
    IReadOnlyList<RecordedMeasurement> _running;
    IReadOnlyList<RecordedMeasurement> _total;

    void Establish()
    {
        var entry = _gauges.Register(JobId.New(), _observerKey, ObserverJobGaugeType.Replay);
        entry.Publish(1, 2);
        entry.Release();
    }

    void Because()
    {
        _running = Collect("cratis.chronicle.job.running");
        _total = Collect("cratis.chronicle.observer.job.total_steps");
    }

    [Fact] void should_not_count_the_job_as_running() => _running.ShouldBeEmpty();
    [Fact] void should_not_report_its_progress() => _total.ShouldBeEmpty();
}
