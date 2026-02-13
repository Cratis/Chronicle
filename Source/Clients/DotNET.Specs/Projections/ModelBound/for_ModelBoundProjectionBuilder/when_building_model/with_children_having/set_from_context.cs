// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class set_from_context : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(TransportTypeAdded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(TransportTypesForSimulation));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_have_children_for_transport_types()
    {
        _result.Children.Keys.ShouldContain(nameof(TransportTypesForSimulation.TransportTypes));
    }

    [Fact]
    void should_have_from_definition_for_transport_type_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TransportTypeAdded)).ToContract();
        var childrenDef = _result.Children[nameof(TransportTypesForSimulation.TransportTypes)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_added_at_property_from_event_context()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TransportTypeAdded)).ToContract();
        var childrenDef = _result.Children[nameof(TransportTypesForSimulation.TransportTypes)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(TransportType.AddedAt));
        fromDef.Properties[nameof(TransportType.AddedAt)].ShouldEqual("$eventContext(Occurred)");
    }

    [Fact]
    void should_not_auto_map_name_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TransportTypeAdded)).ToContract();
        var childrenDef = _result.Children[nameof(TransportTypesForSimulation.TransportTypes)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldNotContain(nameof(TransportType.Name));
    }

    [Fact]
    void should_not_auto_map_co2_per_km_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TransportTypeAdded)).ToContract();
        var childrenDef = _result.Children[nameof(TransportTypesForSimulation.TransportTypes)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldNotContain(nameof(TransportType.Co2PerKm));
    }

    [Fact]
    void should_have_auto_map_enabled_on_children()
    {
        var childrenDef = _result.Children[nameof(TransportTypesForSimulation.TransportTypes)];
        childrenDef.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Enabled);
    }

    [Fact]
    void should_apply_naming_policy_to_identified_by()
    {
        var childrenDef = _result.Children[nameof(TransportTypesForSimulation.TransportTypes)];
        childrenDef.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(TransportType.Id))));
    }
}

[EventType]
public record TransportTypeAdded(TransportTypeName Name, Co2FootPrint Co2PerKm);

public record TransportTypeId(Guid Value);
public record TransportTypeName(string Value);
public record Co2FootPrint(double Value);
public record SimulationId(Guid Value);

public record TransportType(
    TransportTypeId Id,
    TransportTypeName Name,
    Co2FootPrint Co2PerKm,
    [SetFromContext<TransportTypeAdded>(nameof(EventContext.Occurred))]
    DateTimeOffset AddedAt);

[Passive]
[FromEvent<TransportTypeAdded>]
public record TransportTypesForSimulation(
    SimulationId Id,

    [ChildrenFrom<TransportTypeAdded>(identifiedBy: nameof(TransportType.Id))]
    IEnumerable<TransportType> TransportTypes);
