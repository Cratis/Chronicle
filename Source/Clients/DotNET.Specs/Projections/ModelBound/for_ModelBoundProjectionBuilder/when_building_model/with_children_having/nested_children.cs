// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class nested_children : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(NestedSimulationCreated),
            typeof(NestedConfigurationAdded),
            typeof(NestedHubAdded)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(NestedSimulationDashboard));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition_for_configurations() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_have_children_for_configurations()
    {
        _result.Children.Keys.ShouldContain(nameof(NestedSimulationDashboard.Configurations));
    }

    [Fact]
    void should_have_nested_children_for_hubs()
    {
        var configChildrenDef = _result.Children[nameof(NestedSimulationDashboard.Configurations)];
        configChildrenDef.Children.Keys.ShouldContain(nameof(NestedSimulationConfiguration.Hubs));
    }

    [Fact]
    void should_have_from_definition_for_hub_added_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(NestedHubAdded)).ToContract();
        var configChildrenDef = _result.Children[nameof(NestedSimulationDashboard.Configurations)];
        var hubsChildrenDef = configChildrenDef.Children[nameof(NestedSimulationConfiguration.Hubs)];
        hubsChildrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_auto_map_name_property_for_nested_child()
    {
        var eventType = event_types.GetEventTypeFor(typeof(NestedHubAdded)).ToContract();
        var configChildrenDef = _result.Children[nameof(NestedSimulationDashboard.Configurations)];
        var hubsChildrenDef = configChildrenDef.Children[nameof(NestedSimulationConfiguration.Hubs)];
        var fromDef = hubsChildrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(NestedHub.Name));
    }
}

[EventType]
public record NestedSimulationCreated(string Name, string Description);

[EventType]
public record NestedConfigurationAdded(NestedConfigurationId Id, string Name, string Description);

[EventType]
public record NestedHubAdded(NestedHubId Id, NestedConfigurationId ConfigurationId, string Name);

public record NestedSimulationId(Guid Value);
public record NestedConfigurationId(Guid Value);
public record NestedHubId(Guid Value);

public record NestedHub(
    [Key] NestedHubId Id,
    string Name);

public record NestedSimulationConfiguration(
    [Key] NestedConfigurationId Id,
    string Name,
    string Description,

    [ChildrenFrom<NestedHubAdded>(key: nameof(NestedHubAdded.Id), identifiedBy: nameof(NestedHub.Id), parentKey: nameof(NestedHubAdded.ConfigurationId))]
    IEnumerable<NestedHub> Hubs);

[Passive]
[FromEvent<NestedSimulationCreated>]
public record NestedSimulationDashboard(
    NestedSimulationId Id,
    string Name,
    string Description,

    [ChildrenFrom<NestedConfigurationAdded>(key: nameof(NestedConfigurationAdded.Id), identifiedBy: nameof(NestedSimulationConfiguration.Id))]
    IEnumerable<NestedSimulationConfiguration> Configurations);

