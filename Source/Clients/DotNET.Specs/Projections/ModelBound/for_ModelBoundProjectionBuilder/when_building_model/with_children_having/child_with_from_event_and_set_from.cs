// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class child_with_from_event_and_set_from : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(ConfigAdded),
            typeof(WeightsSetForConfig),
            typeof(RunEnded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(Dashboard));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_for_configurations()
    {
        _result.Children.Keys.ShouldContain(nameof(Dashboard.Configurations));
    }

    [Fact]
    void should_have_from_definition_for_simulation_configuration_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ConfigAdded)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_weights_set()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WeightsSetForConfig)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_simulation_run_ended()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RunEnded)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_not_map_name_from_weights_set_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WeightsSetForConfig)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // Auto-mapped properties should not be in client-side Properties
        fromDef.Properties.Keys.ShouldNotContain(nameof(Config.Name));
    }

    [Fact]
    void should_not_map_description_from_weights_set_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WeightsSetForConfig)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // Auto-mapped properties should not be in client-side Properties
        fromDef.Properties.Keys.ShouldNotContain(nameof(Config.Description));
    }

    [Fact]
    void should_have_auto_map_enabled_on_children()
    {
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        childrenDef.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Enabled);
    }

    [Fact]
    void should_map_last_simulation_total_distance_from_simulation_run_ended()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RunEnded)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Config.LastSimulationTotalDistance));
        fromDef.Properties[nameof(Config.LastSimulationTotalDistance)].ShouldEqual(nameof(RunEnded.TotalDistance));
    }

    [Fact]
    void should_map_last_simulation_total_time_from_simulation_run_ended()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RunEnded)).ToContract();
        var childrenDef = _result.Children[nameof(Dashboard.Configurations)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Config.LastSimulationTotalTime));
        fromDef.Properties[nameof(Config.LastSimulationTotalTime)].ShouldEqual(nameof(RunEnded.TotalTime));
    }
}

[EventType]
public record ConfigAdded(
    ConfigId Id,
    ConfigName Name,
    ConfigDescription Description);

[EventType]
public record WeightsSetForConfig(
    ConfigName Name,
    ConfigDescription Description);

[EventType]
public record RunEnded(
    Distance TotalDistance,
    TimeSpan TotalTime,
    Co2FootPrint TotalWaste,
    Cost TotalCost);

public record ConfigId(Guid Value);
public record ConfigName(string Value);
public record ConfigDescription(string Value);
public record DashboardId(Guid Value);
public record Distance(double Value);
public record Cost(decimal Value);

[FromEvent<WeightsSetForConfig>]
public record Config(
    ConfigId Id,
    ConfigName Name,
    ConfigDescription Description,

    [SetFrom<RunEnded>(nameof(RunEnded.TotalDistance))]
    Distance LastSimulationTotalDistance,

    [SetFrom<RunEnded>(nameof(RunEnded.TotalTime))]
    TimeSpan LastSimulationTotalTime,

    [SetFrom<RunEnded>(nameof(RunEnded.TotalWaste))]
    Co2FootPrint LastSimulationTotalWaste,

    [SetFrom<RunEnded>(nameof(RunEnded.TotalCost))]
    Cost LastSimulationTotalCost);

[FromEvent<DashboardAdded>]
public record Dashboard(
    DashboardId Id,
    [ChildrenFrom<ConfigAdded>(identifiedBy: nameof(Config.Id))]
    IEnumerable<Config> Configurations);

[EventType]
public record DashboardAdded(DashboardId Id);
