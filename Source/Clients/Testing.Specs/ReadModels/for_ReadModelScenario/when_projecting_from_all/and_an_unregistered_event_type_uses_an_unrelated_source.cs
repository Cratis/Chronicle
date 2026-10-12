// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_from_all;

public class and_an_unregistered_event_type_uses_an_unrelated_source : Specification
{
    ReadModelScenario<FluentAllEvents> _scenario;
    EventSourceId _openedId;
    EventSourceId _touchedId;
    FluentAllEvents? _touched;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<FluentAllEvents>().WithStrictEventSubscription();
        _openedId = new EventSourceId(Guid.NewGuid());
        _touchedId = new EventSourceId(Guid.NewGuid());
        await _scenario.Given.ForEventSource(_openedId).Events(new FluentThingOpened("Acme"));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_touchedId).Events(new FluentThingTouched());
        _touched = _scenario.InstanceForEventSourceId(_touchedId);
    }

    [Fact] void should_materialize_both_sources() => _scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_opened_name() => _scenario.Instances[_openedId].Name.ShouldEqual("Acme");
    [Fact] void should_count_the_opened_event_once() => _scenario.Instances[_openedId].EventCount.ShouldEqual(1);
    [Fact] void should_return_the_unrelated_source_by_key() => _touched.ShouldNotBeNull();
    [Fact] void should_use_the_unrelated_source_identity() => _touched!.Id.ToString().ShouldEqual(_touchedId.Value);
    [Fact] void should_count_the_unregistered_event_once() => _touched!.EventCount.ShouldEqual(1);
}
