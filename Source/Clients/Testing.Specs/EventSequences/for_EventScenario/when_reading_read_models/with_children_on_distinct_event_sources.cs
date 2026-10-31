// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_children_on_distinct_event_sources : given.a_scenario
{
    ProjectWithTasks _instance;

    async Task Establish() => await SeedChildren();
    async Task Because() => _instance = await _scenario.ReadModels.GetInstanceById<ProjectWithTasks>(_parentId.Value);

    [Fact] void should_read_the_parent() => _instance.Name.ShouldEqual("Original");
    [Fact] void should_read_both_children() => _instance.Tasks.Select(task => task.Title).ShouldContainOnly("First", "Second");
    [Fact] void should_keep_the_child_keys_distinct() => _instance.Tasks.Select(task => task.Id).ShouldContainOnly(_firstChildId, _secondChildId);
}
