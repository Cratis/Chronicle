// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_observer_is_lagging : given.an_observer_publishing_a_lag_sample
{
    static readonly string[] _observerTagKeys =
    [
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.event_sequence.id",
        "cratis.observer.id"
    ];

    IReadOnlyList<RecordedMeasurement> _distance;
    IReadOnlyList<RecordedMeasurement> _stalled;

    void Establish() => Publish(Sample(ObserverLagStatus.Lagging, 17));

    void Because()
    {
        _distance = Collect(DistanceInstrument);
        _stalled = Collect(StalledInstrument);
    }

    [Fact] void should_report_the_distance() => _distance.Single().Value.ShouldEqual(17);
    [Fact] void should_report_the_distance_as_a_long() => _distance.Single().ValueType.ShouldEqual(typeof(long));
    [Fact] void should_report_the_distance_in_events() => _distance.Single().Unit.ShouldEqual("{event}");
    [Fact] void should_report_not_stalled() => _stalled.Single().Value.ShouldEqual(0);
    [Fact] void should_report_stalled_as_an_int() => _stalled.Single().ValueType.ShouldEqual(typeof(int));
    [Fact] void should_report_stalled_as_dimensionless() => _stalled.Single().Unit.ShouldEqual("1");
    [Fact] void should_tag_the_distance_with_exactly_the_observer_dimensions() => _distance.Single().Tags.Select(_ => _.Key).ShouldContainOnly(_observerTagKeys);
    [Fact] void should_tag_stalled_with_exactly_the_observer_dimensions() => _stalled.Single().Tags.Select(_ => _.Key).ShouldContainOnly(_observerTagKeys);
    [Fact] void should_tag_the_observer() => _distance.Single().TagMap["cratis.observer.id"].ShouldEqual("observer");
    [Fact] void should_tag_the_event_sequence() => _distance.Single().TagMap["cratis.event_sequence.id"].ShouldEqual("event-log");
}
