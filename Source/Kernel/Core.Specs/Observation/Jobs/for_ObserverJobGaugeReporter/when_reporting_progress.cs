// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobGaugeReporter;

public class when_reporting_progress : Specification
{
    IObserverJobGauges _gauges;
    ObserverJobGaugeEntry _entry;
    ObserverJobGaugeReporter _reporter;
    JobId _jobId;
    ObserverKey _observerKey;

    void Establish()
    {
        _jobId = JobId.New();
        _observerKey = new("observer", "store", "namespace", "event-log");
        _gauges = Substitute.For<IObserverJobGauges>();
        _entry = new(_jobId, _observerKey, ObserverJobGaugeType.Replay, _ => { });
        _gauges.Register(_jobId, _observerKey, ObserverJobGaugeType.Replay).Returns(_entry);
        _reporter = new(_gauges, ObserverJobGaugeType.Replay);
    }

    void Because()
    {
        _reporter.Report(_jobId, _observerKey, new JobProgress { TotalSteps = 10 });
        _reporter.Update(new JobProgress { TotalSteps = 10, SuccessfulSteps = 4, FailedSteps = 1, StoppedSteps = 2 });
    }

    [Fact] void should_register_the_job_once() => _gauges.Received(1).Register(_jobId, _observerKey, ObserverJobGaugeType.Replay);
    [Fact] void should_count_successful_and_failed_steps_as_handled() => _entry.Progress.HandledSteps.ShouldEqual(5);
    [Fact] void should_report_the_total_steps() => _entry.Progress.TotalSteps.ShouldEqual(10);
}
