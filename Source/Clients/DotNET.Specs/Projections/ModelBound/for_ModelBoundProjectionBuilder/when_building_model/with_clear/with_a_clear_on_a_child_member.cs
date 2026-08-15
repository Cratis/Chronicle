// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// A child item's members are built by a different code path than the root's, so a clear on one is pinned in its
/// own right - the issue asked for root, child and nested members alike.
/// </summary>
public class with_a_clear_on_a_child_member : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(TaskListStarted), typeof(TaskListItemAdded), typeof(TaskListItemDeferred)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(TaskList));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_a_clear_expression_for_the_child_due_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TaskListItemDeferred)).ToContract();
        var childrenDef = _result.Children[nameof(TaskList.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties[nameof(TaskListItem.Due)].ShouldEqual(WellKnownExpressions.Null);
    }
}

[EventType]
public record TaskListStarted(string Name);

[EventType]
public record TaskListItemAdded(Guid ListId, Guid ItemId, string Title, string Due);

[EventType]
public record TaskListItemDeferred(Guid ListId, Guid ItemId);

public sealed record TaskListItem(
    [Key] Guid Id,
    [SetFrom<TaskListItemAdded>(nameof(TaskListItemAdded.Title))] string Title,
    [SetFrom<TaskListItemAdded>(nameof(TaskListItemAdded.Due))]
    [ClearWith<TaskListItemDeferred>]
    string? Due);

[Passive]
[FromEvent<TaskListStarted>]
public sealed record TaskList(
    [Key] Guid Id,
    [ChildrenFrom<TaskListItemAdded>(key: nameof(TaskListItemAdded.ItemId), parentKey: nameof(TaskListItemAdded.ListId), identifiedBy: nameof(TaskListItem.Id))]
    [ChildrenFrom<TaskListItemDeferred>(key: nameof(TaskListItemDeferred.ItemId), parentKey: nameof(TaskListItemDeferred.ListId), identifiedBy: nameof(TaskListItem.Id))]
    IReadOnlyList<TaskListItem> Items);
