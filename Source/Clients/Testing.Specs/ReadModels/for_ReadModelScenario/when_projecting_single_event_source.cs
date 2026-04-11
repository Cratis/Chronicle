// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_projecting_single_event_source : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    EventSourceId _moduleId;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>();
        _moduleId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleCreated("My Module"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_name() => _scenario.Instance!.Name.ShouldEqual("My Module");
}
