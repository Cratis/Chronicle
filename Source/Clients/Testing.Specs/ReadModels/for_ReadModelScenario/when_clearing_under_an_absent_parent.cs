// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_clearing_under_an_absent_parent : Specification
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
        .Events(new ProbeProjectCreatedWithoutOuter(_projectId, "First"), new ProbeProjectRenamed(_projectId));

    [Fact] void should_not_create_the_outer_object() => _scenario.Instance!.Outer.ShouldBeNull();
    [Fact] void should_preserve_the_root() => _scenario.Instance!.Name.ShouldEqual("First");
}
