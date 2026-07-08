// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_a_stream_that_carries_an_unsubscribed_event;

/// <summary>
/// Seeds a flat projection's event source with a subscribed event and a coexisting audit/marker event the
/// projection does not subscribe to. The production projection engine filters the stream to subscribed event
/// types, so the harness must ignore the unsubscribed event rather than throwing MissingKeyResolverForEventType.
/// </summary>
public class and_projecting_a_flat_read_model : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    EventSourceId _moduleId;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>();
        _moduleId = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleCreated("My Module"), new ModuleAudited());

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_name_from_the_subscribed_event() => _scenario.Instance!.Name.ShouldEqual("My Module");
    [Fact] void should_resolve_the_instance_for_the_event_source() => _scenario.InstanceForEventSourceId(_moduleId).ShouldNotBeNull();
    [Fact] void should_materialize_exactly_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
