// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class class_level_from_event_on_child_with_parent_key : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(ClassLevelDashboardAdded),
            typeof(ClassLevelConfigurationAddedToDashboard),
            typeof(ClassLevelConfigurationRenamed)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(DashboardWithConfigurations));

    [Fact]
    void should_set_parent_key_for_child_class_level_from_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ClassLevelConfigurationRenamed)).ToContract();
        var childrenDef = _result.Children[nameof(DashboardWithConfigurations.Configurations)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.ParentKey.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(ClassLevelConfigurationRenamed.DashboardId))));
    }

    [Fact]
    void should_keep_child_event_source_as_the_default_key_for_class_level_from_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ClassLevelConfigurationRenamed)).ToContract();
        var childrenDef = _result.Children[nameof(DashboardWithConfigurations.Configurations)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Key.ShouldEqual(WellKnownExpressions.EventSourceId);
    }
}

[EventType]
public record ClassLevelDashboardAdded(ClassLevelDashboardId Id, string Name);

[EventType]
public record ClassLevelConfigurationAddedToDashboard(ClassLevelDashboardId DashboardId, ClassLevelConfigurationId Id, string Name);

[EventType]
public record ClassLevelConfigurationRenamed(ClassLevelDashboardId DashboardId, string Name);

public record ClassLevelDashboardId(Guid Value);

public record ClassLevelConfigurationId(Guid Value);

[FromEvent<ClassLevelConfigurationRenamed>(parentKey: nameof(ClassLevelConfigurationRenamed.DashboardId))]
public record Configuration(
    ClassLevelConfigurationId Id,
    string Name);

[FromEvent<ClassLevelDashboardAdded>]
public record DashboardWithConfigurations(
    ClassLevelDashboardId Id,
    string Name,
    [ChildrenFrom<ClassLevelConfigurationAddedToDashboard>(
        key: nameof(ClassLevelConfigurationAddedToDashboard.Id),
        identifiedBy: nameof(Configuration.Id),
        parentKey: nameof(ClassLevelConfigurationAddedToDashboard.DashboardId))]
    IEnumerable<Configuration> Configurations);

#pragma warning restore SA1402 // File may only contain a single type
