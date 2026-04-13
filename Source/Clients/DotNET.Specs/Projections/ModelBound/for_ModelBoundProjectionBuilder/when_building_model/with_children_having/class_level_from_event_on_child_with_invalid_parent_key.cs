// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class class_level_from_event_on_child_with_invalid_parent_key : given.a_model_bound_projection_builder
{
    Exception _exception;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(InvalidParentDashboardAdded),
            typeof(InvalidParentConfigurationAddedToDashboard),
            typeof(InvalidParentConfigurationRenamed)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _exception = Catch.Exception(() => builder.Build(typeof(DashboardWithConfigurationUsingInvalidParentKey)));

    [Fact] void should_throw_invalid_property_for_type() => _exception.ShouldBeOfExactType<InvalidPropertyForType>();
    [Fact] void should_indicate_invalid_property_name() => _exception.Message.ShouldContain("DoesNotExist");
    [Fact] void should_indicate_event_type() => _exception.Message.ShouldContain(nameof(InvalidParentConfigurationRenamed));
}

[EventType]
public record InvalidParentDashboardAdded(Guid Id, string Name);

[EventType]
public record InvalidParentConfigurationAddedToDashboard(Guid DashboardId, Guid Id, string Name);

[EventType]
public record InvalidParentConfigurationRenamed(string Name);

[FromEvent<InvalidParentConfigurationRenamed>(parentKey: "DoesNotExist")]
public record ConfigurationUsingInvalidParentKey(
    Guid Id,
    string Name);

[FromEvent<InvalidParentDashboardAdded>]
public record DashboardWithConfigurationUsingInvalidParentKey(
    Guid Id,
    string Name,
    [ChildrenFrom<InvalidParentConfigurationAddedToDashboard>(
        key: nameof(InvalidParentConfigurationAddedToDashboard.Id),
        identifiedBy: nameof(ConfigurationUsingInvalidParentKey.Id),
        parentKey: nameof(InvalidParentConfigurationAddedToDashboard.DashboardId))]
    IEnumerable<ConfigurationUsingInvalidParentKey> Configurations);

#pragma warning restore SA1402 // File may only contain a single type
