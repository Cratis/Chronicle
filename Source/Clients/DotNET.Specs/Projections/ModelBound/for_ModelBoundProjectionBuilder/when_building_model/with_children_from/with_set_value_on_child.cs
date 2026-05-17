// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class with_set_value_on_child : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(UIRoleAdded), typeof(SystemRoleAdded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(CollectionRoles));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_have_from_definition_for_ui_role_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UIRoleAdded)).ToContract();
        var childrenDef = _result.Children[nameof(CollectionRoles.Roles)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_system_role_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SystemRoleAdded)).ToContract();
        var childrenDef = _result.Children[nameof(CollectionRoles.Roles)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_role_type_to_ui_role_constant_from_ui_role_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UIRoleAdded)).ToContract();
        var childrenDef = _result.Children[nameof(CollectionRoles.Roles)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(CollectionRole.RoleType));
        fromDef.Properties[nameof(CollectionRole.RoleType)].ShouldEqual("$value(0)");
    }

    [Fact]
    void should_map_role_type_to_system_role_constant_from_system_role_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SystemRoleAdded)).ToContract();
        var childrenDef = _result.Children[nameof(CollectionRoles.Roles)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(CollectionRole.RoleType));
        fromDef.Properties[nameof(CollectionRole.RoleType)].ShouldEqual("$value(1)");
    }
}

public enum RoleType
{
    UIRole = 0,
    SystemRole = 1
}

[EventType]
public record UIRoleAdded(Guid CollectionId, Guid RoleId, string Name);

[EventType]
public record SystemRoleAdded(Guid CollectionId, Guid RoleId, string Name);

public sealed record CollectionRole(
    [Key] Guid Id,
    [SetValue<UIRoleAdded>(RoleType.UIRole)]
    [SetValue<SystemRoleAdded>(RoleType.SystemRole)]
    RoleType RoleType);

[Passive]
[FromEvent<CollectionAdded>]
public sealed record CollectionRoles(
    [Key] Guid Id,
    [ChildrenFrom<UIRoleAdded>(key: nameof(UIRoleAdded.RoleId), parentKey: nameof(UIRoleAdded.CollectionId), identifiedBy: nameof(CollectionRole.Id))]
    [ChildrenFrom<SystemRoleAdded>(key: nameof(SystemRoleAdded.RoleId), parentKey: nameof(SystemRoleAdded.CollectionId), identifiedBy: nameof(CollectionRole.Id))]
    IReadOnlyList<CollectionRole> Roles);

[EventType]
public record CollectionAdded(Guid CollectionId);
