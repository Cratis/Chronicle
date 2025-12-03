// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class using_parent_id_as_key : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(WarehouseAddedToSimulation)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(WarehousesForSimulation));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact] void should_use_warehouse_id_from_event_as_key()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WarehouseAddedToSimulation)).ToContract();
        var childrenDef = _result.Children[nameof(WarehousesForSimulation.Warehouses)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Key.ShouldEqual(nameof(WarehouseAddedToSimulation.Id));
    }

    [Fact] void should_map_warehouse_id_property_to_event_id_not_event_source_id()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WarehouseAddedToSimulation)).ToContract();
        var childrenDef = _result.Children[nameof(WarehousesForSimulation.Warehouses)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties[nameof(Warehouse.Id)].ShouldEqual(nameof(WarehouseAddedToSimulation.Id));
    }

    [Fact] void should_use_parent_id_property_as_parent_key()
    {
        // This test verifies the builder discovers the event property that identifies the parent
        // by matching the property type with the parent's Id property type (WarehouseSimulationId)
        // The event has SimulationId property which matches the parent's Id property type
        var eventType = event_types.GetEventTypeFor(typeof(WarehouseAddedToSimulation)).ToContract();
        var childrenDef = _result.Children[nameof(WarehousesForSimulation.Warehouses)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.ParentKey.ShouldEqual(nameof(WarehouseAddedToSimulation.SimulationId));
    }

    [Fact] void should_apply_naming_policy_to_identified_by()
    {
        var childrenDef = _result.Children[nameof(WarehousesForSimulation.Warehouses)];
        childrenDef.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(Warehouse.Id))));
    }
}

[EventType]
public record WarehouseAddedToSimulation(WarehouseSimulationId SimulationId, Guid Id, string Name);

public record WarehouseSimulationId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator WarehouseSimulationId(Guid value) => new(value);
}

public record Warehouse([Key] Guid Id, string Name);

public record WarehousesForSimulation(
    WarehouseSimulationId Id,
    [ChildrenFrom<WarehouseAddedToSimulation>(key: nameof(WarehouseAddedToSimulation.Id), identifiedBy: nameof(Warehouse.Id))]
    IEnumerable<Warehouse> Warehouses);
