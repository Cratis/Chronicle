// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_a_flat_projection : given.a_scenario
{
    ProjectSummary _instance;

    async Task Establish()
    {
        await _scenario.Given.ForEventSource(_parentId).Events(new ProjectOpened("Original"));
        await _scenario.When.ForEventSource(_parentId).Events(new ProjectRenamed("Renamed"));
    }

    async Task Because() => _instance = await _scenario.ReadModels.GetInstanceById<ProjectSummary>(_parentId.Value);

    [Fact] void should_read_the_projected_name() => _instance.Name.ShouldEqual("Renamed");
    [Fact] void should_read_the_parent_key() => _instance.Id.ShouldEqual(Guid.Parse(_parentId.Value));
}
