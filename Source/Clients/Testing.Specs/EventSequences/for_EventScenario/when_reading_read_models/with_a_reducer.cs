// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_a_reducer : given.a_scenario
{
    ProjectRenameCount _instance;

    async Task Establish()
    {
        await _scenario.Given.ForEventSource(_parentId).Events(new ProjectOpened("Original"), new ProjectRenamed("First"));
        await _scenario.When.ForEventSource(_parentId).Events(new ProjectRenamed("Second"));
    }

    async Task Because() => _instance = await _scenario.ReadModels.GetInstanceById<ProjectRenameCount>(_parentId.Value);

    [Fact] void should_reduce_the_scenario_events() => _instance.Count.ShouldEqual(2);
    [Fact] void should_read_the_parent_key() => _instance.Id.ShouldEqual(Guid.Parse(_parentId.Value));
}
