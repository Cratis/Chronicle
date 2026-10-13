// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_a_runtime_read_model_type : given.a_scenario
{
    ProjectWithTasks _instance;

    async Task Establish() => await SeedChildren();
    async Task Because() => _instance = (ProjectWithTasks)await _scenario.ReadModels.GetInstanceById(typeof(ProjectWithTasks), _parentId.Value);

    [Fact] void should_read_both_children() => _instance.Tasks.Select(task => task.Id).ShouldContainOnly(_firstChildId, _secondChildId);
}
