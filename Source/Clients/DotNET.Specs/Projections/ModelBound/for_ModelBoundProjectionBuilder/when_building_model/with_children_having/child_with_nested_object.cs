// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

/// <summary>
/// Spec that verifies a [Nested] attribute on a child type's property is correctly promoted into
/// the children definition so the server sees it as a nested sub-projection.
/// </summary>
public class child_with_nested_object : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(SliceParentCreated),
            typeof(SliceAdded),
            typeof(SliceCommandSet),
            typeof(SliceCommandRenamed),
            typeof(SliceCommandCleared)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SliceParent));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_for_slices()
    {
        _result.Children.Keys.ShouldContain(nameof(SliceParent.Slices));
    }

    [Fact]
    void should_have_nested_command_definition_within_slices_children()
    {
        var slicesDef = _result.Children[nameof(SliceParent.Slices)];
        slicesDef.Nested.Keys.ShouldContain(nameof(SliceChild.Command));
    }

    [Fact]
    void should_have_from_definition_for_command_set_in_nested_command()
    {
        var commandSetEventType = event_types.GetEventTypeFor(typeof(SliceCommandSet)).ToContract();
        var slicesDef = _result.Children[nameof(SliceParent.Slices)];
        var commandDef = slicesDef.Nested[nameof(SliceChild.Command)];
        commandDef.From.Keys.ShouldContain(et => et.IsEqual(commandSetEventType));
    }

    [Fact]
    void should_have_from_definition_for_command_renamed_in_nested_command()
    {
        var commandRenamedEventType = event_types.GetEventTypeFor(typeof(SliceCommandRenamed)).ToContract();
        var slicesDef = _result.Children[nameof(SliceParent.Slices)];
        var commandDef = slicesDef.Nested[nameof(SliceChild.Command)];
        commandDef.From.Keys.ShouldContain(et => et.IsEqual(commandRenamedEventType));
    }

    [Fact]
    void should_have_auto_map_enabled_on_nested_command()
    {
        var slicesDef = _result.Children[nameof(SliceParent.Slices)];
        var commandDef = slicesDef.Nested[nameof(SliceChild.Command)];
        commandDef.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Enabled);
    }

    [Fact]
    void should_map_name_from_command_renamed_with_property_rename()
    {
        var commandRenamedEventType = event_types.GetEventTypeFor(typeof(SliceCommandRenamed)).ToContract();
        var slicesDef = _result.Children[nameof(SliceParent.Slices)];
        var commandDef = slicesDef.Nested[nameof(SliceChild.Command)];
        var fromDef = commandDef.From.Single(kvp => kvp.Key.IsEqual(commandRenamedEventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(SliceCommandInfo.Name));
        fromDef.Properties[nameof(SliceCommandInfo.Name)].ShouldEqual(nameof(SliceCommandRenamed.NewName));
    }

    [Fact]
    void should_have_removed_with_definition_for_command_cleared_in_nested_command()
    {
        var commandClearedEventType = event_types.GetEventTypeFor(typeof(SliceCommandCleared)).ToContract();
        var slicesDef = _result.Children[nameof(SliceParent.Slices)];
        var commandDef = slicesDef.Nested[nameof(SliceChild.Command)];
        commandDef.RemovedWith.Keys.ShouldContain(et => et.IsEqual(commandClearedEventType));
    }
}

[EventType]
public record SliceParentCreated(string Name);

[EventType]
public record SliceAdded(Guid ParentId, Guid SliceId, string Name);

[EventType]
public record SliceCommandSet(string Name, string Schema);

[EventType]
public record SliceCommandRenamed(string NewName);

[EventType]
public record SliceCommandCleared;

[FromEvent<SliceCommandSet>]
[FromEvent<SliceCommandRenamed>]
[ClearWith<SliceCommandCleared>]
public record SliceCommandInfo(
    [SetFrom<SliceCommandRenamed>(nameof(SliceCommandRenamed.NewName))]
    string Name,
    string Schema);

[FromEvent<SliceAdded>]
public record SliceChild(
    Guid Id,
    string Name,
    [Nested] SliceCommandInfo? Command);

[FromEvent<SliceParentCreated>]
public record SliceParent(
    Guid Id,
    string Name,
    [ChildrenFrom<SliceAdded>(key: nameof(SliceAdded.SliceId), parentKey: nameof(SliceAdded.ParentId))]
    IEnumerable<SliceChild> Slices);

#pragma warning restore SA1402 // File may only contain a single type
