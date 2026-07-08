// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

public class on_a_single_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ArrangementChanged)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SummaryWithPropertyNoAutoMap));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact] void should_flag_the_marked_property_as_no_auto_map() => _result.NoAutoMapProperties.ShouldContain(nameof(SummaryWithPropertyNoAutoMap.Location));

    [Fact] void should_not_flag_other_properties() => _result.NoAutoMapProperties.ShouldNotContain(nameof(SummaryWithPropertyNoAutoMap.Name));

    [Fact] void should_keep_auto_map_enabled_for_the_projection() => _result.AutoMap.ShouldEqual(Cratis.Chronicle.Contracts.Projections.AutoMap.Enabled);
}

[EventType]
public record ArrangementChanged(string Location, string Name);

[FromEvent<ArrangementChanged>]
public record SummaryWithPropertyNoAutoMap(
    [Key] Guid Id,

    [SetFrom<ArrangementChanged>]
    [NoAutoMap]
    string Location,

    string Name);
