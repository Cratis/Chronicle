// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_every_constructor : given.a_scenario
{
    EventScenario[] _scenarios;
    List<ProjectWithTasks> _instances;
    Defaults _defaults;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ProjectOpened), typeof(TaskAdded)]);
        artifacts.ModelBoundProjections.Returns([typeof(ProjectWithTasks), typeof(ProjectTask)]);
        _defaults = new(artifacts);
        EventSequenceId sequence = "selected-sequence";
        _scenarios =
        [
            new(),
            new(constraintProvider: null),
            new(_defaults),
            new(sequence, "named-store", "named-namespace"),
            new(_defaults, sequence, "run-store", "run-namespace"),
            new(sequence, "explicit-store", "explicit-namespace", null),
            new(sequence, "primary-store", "primary-namespace", null, _defaults)
        ];
        _instances = [];
    }

    async Task Because()
    {
        foreach (var scenario in _scenarios)
        {
            await scenario.Given.ForEventSource(_parentId).Events(new ProjectOpened("Original"));
            await scenario.When.ForEventSource(new EventSourceId(_firstChildId)).Events(new TaskAdded(Guid.Parse(_parentId.Value), _firstChildId, "First"));
            _instances.Add(await scenario.ReadModels.GetInstanceById<ProjectWithTasks>(_parentId.Value));
        }
    }

    void Destroy()
    {
        foreach (var scenario in _scenarios) scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Fact] void should_read_from_every_selected_sequence() => _instances.Select(instance => instance.Name).ShouldEachConformTo(name => name == "Original");
    [Fact] void should_resolve_children_in_every_selected_store() => _instances.Select(instance => instance.Tasks.Single().Id).ShouldEachConformTo(id => id == _firstChildId);
}
