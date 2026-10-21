// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_an_explicit_default_source_filter : Specification
{
    ReadModelScenario<DefaultSourceEventRoute> _scenario;
    EventSourceId _source;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Order")
            .Events(new RoutedEventRecorded("ignored"));
        await _scenario.Given.ForEventSource(_source).Events(new RoutedEventRecorded("default"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_treat_default_as_a_literal_source_filter() => _scenario.Instance!.Count.ShouldEqual(1);
}
