// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobGauges.when_reporting_jobs;

public class and_catch_up_and_replay_jobs_are_running : given.the_observer_job_gauges
{
    static readonly string[] _runningTagKeys =
    [
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.chronicle.job.type"
    ];

    static readonly string[] _progressTagKeys =
    [
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.event_sequence.id",
        "cratis.observer.id",
        "cratis.chronicle.job.type"
    ];

    IReadOnlyList<RecordedMeasurement> _running;
    IReadOnlyList<RecordedMeasurement> _handled;
    IReadOnlyList<RecordedMeasurement> _total;

    void Establish()
    {
        _gauges.Register(JobId.New(), _observerKey, ObserverJobGaugeType.CatchUp).Publish(2, 10);
        _gauges.Register(JobId.New(), _observerKey, ObserverJobGaugeType.CatchUp).Publish(1, 4);
        _gauges.Register(JobId.New(), _observerKey, ObserverJobGaugeType.Replay).Publish(3, 7);
    }

    void Because()
    {
        _running = Collect("cratis.chronicle.job.running");
        _handled = Collect("cratis.chronicle.observer.job.handled_steps");
        _total = Collect("cratis.chronicle.observer.job.total_steps");
    }

    RecordedMeasurement For(IReadOnlyList<RecordedMeasurement> measurements, string jobType) =>
        measurements.Single(_ => (string)_.TagMap["cratis.chronicle.job.type"]! == jobType);

    [Fact] void should_count_the_running_catch_ups() => For(_running, "catch_up").Value.ShouldEqual(2);
    [Fact] void should_count_the_running_replay() => For(_running, "replay").Value.ShouldEqual(1);
    [Fact] void should_tag_running_jobs_with_exactly_the_bounded_dimensions() => _running.All(_ => _.Tags.Select(tag => tag.Key).Order().SequenceEqual(_runningTagKeys.Order())).ShouldBeTrue();
    [Fact] void should_sum_the_handled_catch_up_steps_of_the_observer() => For(_handled, "catch_up").Value.ShouldEqual(3);
    [Fact] void should_sum_the_catch_up_steps_of_the_observer() => For(_total, "catch_up").Value.ShouldEqual(14);
    [Fact] void should_report_the_handled_replay_steps() => For(_handled, "replay").Value.ShouldEqual(3);
    [Fact] void should_report_the_replay_steps() => For(_total, "replay").Value.ShouldEqual(7);
    [Fact] void should_tag_handled_steps_with_exactly_the_observer_and_job_type() => _handled.All(_ => _.Tags.Select(tag => tag.Key).Order().SequenceEqual(_progressTagKeys.Order())).ShouldBeTrue();
    [Fact] void should_tag_total_steps_with_exactly_the_observer_and_job_type() => _total.All(_ => _.Tags.Select(tag => tag.Key).Order().SequenceEqual(_progressTagKeys.Order())).ShouldBeTrue();
    [Fact] void should_tag_the_observer() => For(_handled, "replay").TagMap["cratis.observer.id"].ShouldEqual("observer");
}
