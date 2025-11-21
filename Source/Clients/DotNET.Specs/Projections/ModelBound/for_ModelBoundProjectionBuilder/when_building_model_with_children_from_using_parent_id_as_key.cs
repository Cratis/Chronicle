// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder;

public class when_building_model_with_children_from_using_parent_id_as_key : given.a_model_bound_projection_builder
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

    [Fact] void should_use_parent_id_property_as_parent_key()
    {
        // This test verifies the issue where the builder defaults to EventSourceId
        // instead of using the parent's Id property (WarehousesForSimulation.Id)
        // when no explicit parentKey is specified in the ChildrenFrom attribute
        var eventType = event_types.GetEventTypeFor(typeof(WarehouseAddedToSimulation)).ToContract();
        var childrenDef = _result.Children[nameof(WarehousesForSimulation.Warehouses)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.ParentKey.ShouldEqual(nameof(WarehousesForSimulation.Id));
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
