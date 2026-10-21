// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models.given;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_observing_read_models;

public class and_a_child_is_appended : a_scenario
{
    ISubject<IEnumerable<ProjectWithTasks>> _observation;
    IDisposable _subscription;
    SemaphoreSlim _signal;
    List<ProjectWithTasks[]> _emissions;
    Exception _error;

    async Task Establish()
    {
        await SeedChildren();
        _signal = new(0);
        _emissions = [];
    }

    async Task Because()
    {
        _observation = _scenario.ReadModels.Observe<ProjectWithTasks>();
        _subscription = _observation.Subscribe(
            set =>
            {
                _emissions.Add(set.ToArray());
                _signal.Release();
            },
            error =>
            {
                _error = error;
                _signal.Release();
            });
        await WaitForEmission();
        var thirdId = Guid.NewGuid();
        await _scenario.EventLog.Append(new EventSourceId(thirdId), new TaskAdded(Guid.Parse(_parentId.Value), thirdId, "Third"));
        await WaitForEmission();
    }

    async Task WaitForEmission()
    {
        (await _signal.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBeTrue();
        _error.ShouldBeNull();
    }

    void Destroy()
    {
        _subscription?.Dispose();
        (_observation as IDisposable)?.Dispose();
        _signal.Dispose();
    }

    [Fact] void should_emit_the_initial_parent_with_both_children() => _emissions[0].Single().Tasks.Select(task => task.Title).ShouldContainOnly("First", "Second");
    [Fact] void should_emit_again_after_the_append() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_include_the_new_child() => _emissions[^1].Single().Tasks.Select(task => task.Title).ShouldContainOnly("First", "Second", "Third");
}
