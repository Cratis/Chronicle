// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_a_bounded_collection : given.a_scenario
{
    IEnumerable<ProjectSummary> _instances;

    async Task Establish()
    {
        await _scenario.Given.ForEventSource(_parentId).Events(new ProjectOpened("Original"));
        await _scenario.When.ForEventSource(_parentId).Events(new ProjectRenamed("Renamed"));
    }

    async Task Because() => _instances = await _scenario.ReadModels.GetInstances<ProjectSummary>(1);

    [Fact] void should_only_apply_the_requested_number_of_events() => _instances.Single().Name.ShouldEqual("Original");
}
