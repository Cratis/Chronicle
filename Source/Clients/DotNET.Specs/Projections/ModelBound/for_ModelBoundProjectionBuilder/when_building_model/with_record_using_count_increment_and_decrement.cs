// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model;

public class with_record_using_count_increment_and_decrement : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(TaskCreated),
            typeof(TaskStarted),
            typeof(TaskCompleted)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(ProjectStatistics));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_from_definition_for_task_created()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskCreated)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_task_started()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskStarted)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_task_completed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskCompleted)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_count_total_tasks()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskCreated)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(ProjectStatistics.TotalTasks)];
        expression.ShouldEqual($"{WellKnownExpressions.Count}()");
    }

    [Fact]
    void should_increment_active_tasks_on_start()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskStarted)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(ProjectStatistics.ActiveTasks)];
        expression.ShouldEqual($"{WellKnownExpressions.Increment}()");
    }

    [Fact]
    void should_decrement_active_tasks_on_completion()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskCompleted)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(ProjectStatistics.ActiveTasks)];
        expression.ShouldEqual($"{WellKnownExpressions.Decrement}()");
    }

    [Fact]
    void should_count_completed_tasks()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskCompleted)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(ProjectStatistics.CompletedTasks)];
        expression.ShouldEqual($"{WellKnownExpressions.Count}()");
    }
}

[EventType]
public record TaskCreated(string TaskName);

[EventType]
public record TaskStarted();

[EventType]
public record TaskCompleted();

public record ProjectStatistics(
    [Key]
    Guid ProjectId,

    [Count<TaskCreated>]
    int TotalTasks,

    [Increment<TaskStarted>]
    [Decrement<TaskCompleted>]
    int ActiveTasks,

    [Count<TaskCompleted>]
    int CompletedTasks);
