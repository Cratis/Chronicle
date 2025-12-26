// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class id_by_convention_should_default_to_event_source_id : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(VehicleRegistered)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(VehicleFleet));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact] void should_have_children_for_vehicles()
    {
        _result.Children.Keys.ShouldContain(nameof(VehicleFleet.Vehicles));
    }

    [Fact] void should_have_from_definition_for_vehicle_registered()
    {
        var eventType = event_types.GetEventTypeFor(typeof(VehicleRegistered)).ToContract();
        var childrenDef = _result.Children[nameof(VehicleFleet.Vehicles)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_map_id_property_to_event_source_id_by_default()
    {
        var eventType = event_types.GetEventTypeFor(typeof(VehicleRegistered)).ToContract();
        var childrenDef = _result.Children[nameof(VehicleFleet.Vehicles)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Vehicle.Id));
        fromDef.Properties[nameof(Vehicle.Id)].ShouldEqual("$eventContext(EventSourceId)");
    }

    [Fact] void should_auto_map_license_plate_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(VehicleRegistered)).ToContract();
        var childrenDef = _result.Children[nameof(VehicleFleet.Vehicles)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Vehicle.LicensePlate));
    }

    [Fact] void should_apply_naming_policy_to_identified_by()
    {
        var childrenDef = _result.Children[nameof(VehicleFleet.Vehicles)];
        childrenDef.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(Vehicle.Id))));
    }
}

[EventType]
public record VehicleRegistered(string LicensePlate, string Model);

public record VehicleId(Guid Value);
public record FleetId(Guid Value);

public record Vehicle(
    VehicleId Id,
    string LicensePlate,
    string Model);

[Passive]
[FromEvent<VehicleRegistered>]
public record VehicleFleet(
    FleetId Id,

    [ChildrenFrom<VehicleRegistered>(identifiedBy: nameof(Vehicle.Id))]
    IEnumerable<Vehicle> Vehicles);
