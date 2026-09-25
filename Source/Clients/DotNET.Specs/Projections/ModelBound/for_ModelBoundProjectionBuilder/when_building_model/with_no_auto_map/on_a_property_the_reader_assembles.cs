// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

/// <summary>
/// A member the reader assembles must never be written by the projection. A subscribed event carrying the same name
/// would otherwise auto-map straight over it, which is the silent half of the mistake - so declaring it excludes it
/// from auto-mapping as well as from the analyzers.
/// </summary>
public class on_a_property_the_reader_assembles : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ItineraryChanged)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SummaryWithNotProjectedProperty));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact] void should_exclude_the_assembled_property_from_auto_mapping() => _result.NoAutoMapProperties.ShouldContain(nameof(SummaryWithNotProjectedProperty.Destination));

    [Fact] void should_not_exclude_other_properties() => _result.NoAutoMapProperties.ShouldNotContain(nameof(SummaryWithNotProjectedProperty.Name));

    [Fact] void should_keep_auto_map_enabled_for_the_projection() => _result.AutoMap.ShouldEqual(Cratis.Chronicle.Contracts.Projections.AutoMap.Enabled);
}

[EventType]
public record ItineraryChanged(string Destination, string Name);

[FromEvent<ItineraryChanged>]
public record SummaryWithNotProjectedProperty(
    [Key] Guid Id,

    [NotProjected]
    string Destination,

    string Name);
