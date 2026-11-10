// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_an_observer_published_a_snapshot : given.the_observer_gauges
{
    static readonly string[] _observerTagKeys =
    [
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.event_sequence.id",
        "cratis.observer.id"
    ];

    IReadOnlyList<RecordedMeasurement> _failed;
    IReadOnlyList<RecordedMeasurement> _quarantined;
    IReadOnlyList<RecordedMeasurement> _runningState;

    void Establish() => _gauges.Register(_observerKey, ObserverType.Reactor).Publish(new(ObserverRunningState.Quarantined, 3, 2));

    void Because()
    {
        _failed = Collect("cratis.chronicle.observer.failed_partitions");
        _quarantined = Collect("cratis.chronicle.observer.quarantined_partitions");
        _runningState = Collect("cratis.chronicle.observer.running_state");
    }

    [Fact] void should_report_the_failed_partitions() => _failed.Single().Value.ShouldEqual(3);
    [Fact] void should_report_the_quarantined_partitions() => _quarantined.Single().Value.ShouldEqual(2);
    [Fact] void should_report_the_running_state_code() => _runningState.Single().Value.ShouldEqual((long)ObserverRunningState.Quarantined);
    [Fact] void should_tag_failed_partitions_with_exactly_the_observer_dimensions() => _failed.Single().Tags.Select(_ => _.Key).ShouldContainOnly(_observerTagKeys);
    [Fact] void should_tag_quarantined_partitions_with_exactly_the_observer_dimensions() => _quarantined.Single().Tags.Select(_ => _.Key).ShouldContainOnly(_observerTagKeys);
    [Fact] void should_tag_the_running_state_with_exactly_the_observer_dimensions() => _runningState.Single().Tags.Select(_ => _.Key).ShouldContainOnly(_observerTagKeys);
    [Fact] void should_tag_the_event_store() => _failed.Single().TagMap["cratis.event_store.name"].ShouldEqual("store");
    [Fact] void should_tag_the_namespace() => _failed.Single().TagMap["cratis.event_store.namespace"].ShouldEqual("namespace");
    [Fact] void should_tag_the_event_sequence() => _failed.Single().TagMap["cratis.event_sequence.id"].ShouldEqual("event-log");
    [Fact] void should_tag_the_observer() => _failed.Single().TagMap["cratis.observer.id"].ShouldEqual("observer");
}
