// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_repeated_reads : given.a_scenario
{
    IEnumerable<ProjectWithTasks> _instances;
    EventCount _count;

    async Task Establish() => await SeedChildren();

    async Task Because()
    {
        await _scenario.ReadModels.GetInstanceById<ProjectWithTasks>(_parentId.Value);
        _instances = await _scenario.ReadModels.GetInstances<ProjectWithTasks>();
        _count = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    [Fact] void should_rebuild_the_parent_with_both_children() => _instances.Single().Tasks.Select(task => task.Id).ShouldContainOnly(_firstChildId, _secondChildId);
    [Fact] void should_not_append_events_during_reads() => _count.ShouldEqual((EventCount)3);
}
