// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class without_a_matching_key : given.a_scenario
{
    ProjectSummary _instance;

    async Task Establish() => await _scenario.Given.ForEventSource(_parentId).Events(new ProjectOpened("Original"));
    async Task Because() => _instance = await _scenario.ReadModels.GetInstanceById<ProjectSummary>(EventSourceId.New().Value);

    [Fact] void should_return_no_instance() => _instance.ShouldBeNull();
}
