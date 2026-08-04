// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

/// <summary>
/// A nested type's own exclusions, which used to be dropped while the root's leaked in.
/// </summary>
/// <remarks>
/// The two failed in opposite directions and that is the tell for the cause: the nested object was given the
/// root's settings rather than its own, and the exclusion set is matched by bare property name. So a nested
/// type's own per-property exclusion did nothing, and a root exclusion silently blanked an unrelated nested
/// property that happened to share the name.
/// </remarks>
public class on_a_nested_type : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ProfileChanged)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(ProfileWithFencedNestedProperty));

    ChildrenDefinition Nested => _result.Nested[nameof(ProfileWithFencedNestedProperty.Home)];

    [Fact] void should_flag_the_marked_nested_property_as_no_auto_map() => Nested.NoAutoMapProperties.ShouldContain(nameof(Address.Marker));
    [Fact] void should_not_flag_other_nested_properties() => Nested.NoAutoMapProperties.ShouldNotContain(nameof(Address.City));
    [Fact] void should_keep_the_root_exclusion_on_the_root() => _result.NoAutoMapProperties.ShouldContain(nameof(ProfileWithFencedNestedProperty.Marker));
}

[EventType]
public record ProfileChanged(string Marker, string City);

public record Address(
    [property: NoAutoMap]
    string Marker,

    string City);

[FromEvent<ProfileChanged>]
public record ProfileWithFencedNestedProperty(
    [Key] Guid Id,

    [property: NoAutoMap]
    string Marker,

    [Nested] Address? Home);
