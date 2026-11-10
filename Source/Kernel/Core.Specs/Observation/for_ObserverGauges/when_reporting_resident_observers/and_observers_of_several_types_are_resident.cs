// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_resident_observers;

public class and_observers_of_several_types_are_resident : given.the_observer_gauges
{
    static readonly string[] _residentTagKeys =
    [
        "cratis.event_store.name",
        "cratis.event_store.namespace",
        "cratis.observer.type",
        "cratis.chronicle.observer.running_state"
    ];

    IReadOnlyList<RecordedMeasurement> _resident;

    void Establish()
    {
        _gauges.Register(new("first", "store", "namespace", "event-log"), ObserverType.Reactor).Publish(new(ObserverRunningState.Active, 0, 0));
        _gauges.Register(new("second", "store", "namespace", "event-log"), ObserverType.Reactor).Publish(new(ObserverRunningState.Active, 0, 0));
        _gauges.Register(new("third", "store", "namespace", "event-log"), ObserverType.Projection).Publish(new(ObserverRunningState.Replaying, 0, 0));
        _gauges.Register(new("fourth", "store", "other", "event-log"), ObserverType.Reducer).Publish(new(ObserverRunningState.Disconnected, 0, 0));
    }

    void Because() => _resident = Collect("cratis.chronicle.observer.resident");

    RecordedMeasurement For(string @namespace, string type, string state) =>
        _resident.Single(_ =>
            (string)_.TagMap["cratis.event_store.namespace"]! == @namespace &&
            (string)_.TagMap["cratis.observer.type"]! == type &&
            (string)_.TagMap["cratis.chronicle.observer.running_state"]! == state);

    [Fact] void should_report_one_series_per_namespace_type_and_state() => _resident.Count.ShouldEqual(3);
    [Fact] void should_tag_with_exactly_the_bounded_dimensions() => _resident.All(_ => _.Tags.Select(tag => tag.Key).Order().SequenceEqual(_residentTagKeys.Order())).ShouldBeTrue();
    [Fact] void should_count_the_active_reactors() => For("namespace", "reactor", "active").Value.ShouldEqual(2);
    [Fact] void should_count_the_replaying_projection() => For("namespace", "projection", "replaying").Value.ShouldEqual(1);
    [Fact] void should_count_the_disconnected_reducer_in_its_namespace() => For("other", "reducer", "disconnected").Value.ShouldEqual(1);
    [Fact] void should_never_tag_with_an_observer() => _resident.Any(_ => _.TagMap.ContainsKey("cratis.observer.id")).ShouldBeFalse();
}
