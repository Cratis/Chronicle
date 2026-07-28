// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_large_number_of_events;

public class and_they_span_many_event_sources : Specification
{
    const int NumberOfModules = 500;

    ReadModelScenario<SimpleModule> _scenario;
    Guid[] _moduleIds;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>();
        _moduleIds = [.. Enumerable.Range(0, NumberOfModules).Select(_ => Guid.NewGuid())];
    }

    async Task Because()
    {
        for (var index = 0; index < NumberOfModules; index++)
        {
            await _scenario.Given
                .ForEventSource(_moduleIds[index])
                .Events(new ModuleCreated($"Module {index}"));
        }
    }

    [Fact] void should_materialize_an_instance_per_event_source() => _scenario.Instances.Count.ShouldEqual(NumberOfModules);
    [Fact] void should_key_every_instance_by_its_own_event_source() => _scenario.Instances.Keys.OrderBy(_ => _.Value).ShouldEqual(_moduleIds.Select(_ => new EventSourceId(_)).OrderBy(_ => _.Value));
    [Fact] void should_project_every_instance_with_its_own_name() => Enumerable.Range(0, NumberOfModules).All(index => _scenario.InstanceForEventSourceId(_moduleIds[index])!.Name == $"Module {index}").ShouldBeTrue();
}
