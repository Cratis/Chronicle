// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class without_matching_sources : Specification
{
    ReadModelScenario<SourceFilteredEventRoute> _scenario;

    void Establish() => _scenario = new();

    async Task Because() =>
        await _scenario.Given.ForEventSource(EventSourceId.New())
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new RoutedEventRecorded("ignored"));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_not_materialize_an_instance() => _scenario.Instance.ShouldBeNull();
    [Fact] void should_not_materialize_any_keyed_instances() => _scenario.Instances.ShouldBeEmpty();
}
