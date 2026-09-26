// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_recreating_an_object_inside_another_nested_object : Specification
{
    ReadModelScenario<DeepNestedProbe> _scenario;
    Guid _projectId;

    void Establish()
    {
        _scenario = new ReadModelScenario<DeepNestedProbe>();
        _projectId = Guid.NewGuid();
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(new EventSourceId(_projectId))
        .Events(new ProbeProjectRegistered(_projectId, "First"), new ProbeProjectRenamed(_projectId), new ProbeProjectRegistered(_projectId, "Again"));

    [Fact] void should_keep_the_outer_object() => _scenario.Instance!.Outer!.Name.ShouldEqual("Again");
    [Fact] void should_recreate_the_inner_object() => _scenario.Instance!.Outer!.Info!.Name.ShouldEqual("Again");
}
