// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class nested_child_with_from_event_and_set_from : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(WarehouseAdded),
            typeof(RouteAddedToWarehouse),
            typeof(RouteDetailsUpdated)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SupplyChain));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact] void should_have_children_for_warehouses()
    {
        _result.Children.Keys.ShouldContain(nameof(SupplyChain.Warehouses));
    }

    [Fact] void should_have_nested_children_for_routes()
    {
        var warehouseChildren = _result.Children[nameof(SupplyChain.Warehouses)];
        warehouseChildren.Children.Keys.ShouldContain(nameof(Warehouse.Routes));
    }

    [Fact] void should_have_from_definition_for_route_added_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RouteAddedToWarehouse)).ToContract();
        var warehouseChildren = _result.Children[nameof(SupplyChain.Warehouses)];
        var routeChildren = warehouseChildren.Children[nameof(Warehouse.Routes)];
        routeChildren.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_have_from_definition_for_route_details_updated_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RouteDetailsUpdated)).ToContract();
        var warehouseChildren = _result.Children[nameof(SupplyChain.Warehouses)];
        var routeChildren = warehouseChildren.Children[nameof(Warehouse.Routes)];
        routeChildren.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_map_distance_from_route_details_updated()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RouteDetailsUpdated)).ToContract();
        var warehouseChildren = _result.Children[nameof(SupplyChain.Warehouses)];
        var routeChildren = warehouseChildren.Children[nameof(Warehouse.Routes)];
        var fromDef = routeChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Route.Distance));
        fromDef.Properties[nameof(Route.Distance)].ShouldEqual(nameof(RouteDetailsUpdated.TotalDistance));
    }

    [Fact] void should_map_name_from_route_details_updated()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RouteDetailsUpdated)).ToContract();
        var warehouseChildren = _result.Children[nameof(SupplyChain.Warehouses)];
        var routeChildren = warehouseChildren.Children[nameof(Warehouse.Routes)];
        var fromDef = routeChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Route.Name));
    }
}

[EventType]
public record WarehouseAdded(WarehouseId Id, WarehouseName Name);

[EventType]
public record RouteAddedToWarehouse(RouteId RouteId, RouteName Name);

[EventType]
public record RouteDetailsUpdated(RouteName Name, Distance TotalDistance);

public record SupplyChainId(Guid Value);
public record WarehouseId(Guid Value);
public record WarehouseName(string Value);
public record RouteId(Guid Value);
public record RouteName(string Value);

[FromEvent<RouteDetailsUpdated>]
public record Route(
    RouteId RouteId,
    RouteName Name,

    [SetFrom<RouteDetailsUpdated>(nameof(RouteDetailsUpdated.TotalDistance))]
    Distance Distance);

public record Warehouse(
    WarehouseId Id,
    WarehouseName Name,

    [ChildrenFrom<RouteAddedToWarehouse>(key: nameof(RouteAddedToWarehouse.RouteId), identifiedBy: nameof(Route.RouteId))]
    IEnumerable<Route> Routes);

[FromEvent<SupplyChainCreated>]
public record SupplyChain(
    SupplyChainId Id,

    [ChildrenFrom<WarehouseAdded>(identifiedBy: nameof(Warehouse.Id))]
    IEnumerable<Warehouse> Warehouses);

[EventType]
public record SupplyChainCreated(SupplyChainId Id);
