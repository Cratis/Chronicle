// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_the_projection_uses_an_event_property_key : appended_contexts
{
    ReadModelScenario<ScenarioContextProjection> _scenario;
    ReadModelScenario<ScenarioContextProjection> _bareScenario;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<ScenarioContextProjection>()
            .WithProjection(builder => builder.From<ScenarioContextRecorded>(from => from.UsingKey(@event => @event.Value)));
        _bareScenario = new ReadModelScenario<ScenarioContextProjection>()
            .WithProjection(builder => builder.From<ScenarioContextRecorded>(from => from.UsingKey(@event => @event.Value)));
        await _bareScenario.ProcessEventsFor(_firstId, [new ScenarioContextRecorded("property-key")]);
    }

    Task Because() => _scenario.ProcessAppendedEvents([new AppendedEvent(_context, new ScenarioContextRecorded("property-key"))]);

    void Destroy()
    {
        _scenario.Dispose();
        _bareScenario.Dispose();
    }

    [Fact] void should_resolve_the_same_key_as_bare_input() => _scenario.Instances.Keys.ShouldEqual(_bareScenario.Instances.Keys);
    [Fact] void should_materialize_the_property_key() => _scenario.InstanceForEventSourceId("property-key")!.Id.ShouldEqual("property-key");
    [Fact] void should_not_materialize_the_event_source_key() => _scenario.InstanceForEventSourceId(_firstId).ShouldBeNull();
}
