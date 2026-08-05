// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_nested_property_is_excluded_from_auto_map;

/// <summary>
/// Exclusions are scoped to the model that declares them, and the nested object's own are what apply to it.
/// </summary>
/// <remarks>
/// The nested object used to be given the root's settings instead of its own, and the set is matched by bare
/// property name - so the two halves failed in opposite directions at once. A root exclusion silently blanked an
/// unrelated nested property that happened to share the name, and the nested type's own exclusion did nothing.
/// Both are asserted here on the same projection, because it is the pair that pins the scoping rather than one
/// direction of it.
/// </remarks>
public class and_the_root_excludes_a_property_of_the_same_name : Specification
{
    ReadModelScenario<NestedFencedProfile> _scenario;
    EventSourceId _profileId;

    void Establish()
    {
        _scenario = new ReadModelScenario<NestedFencedProfile>();
        _profileId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(_profileId)
        .Events(
            new NestedProfileOpened("the-profile"),
            new NestedProfileDetailed("the-marker", "the-city"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_keep_the_root_property_fenced() => _scenario.Instance!.Marker.ShouldBeNull();
    [Fact] void should_not_leak_the_root_exclusion_into_the_nested_object() => _scenario.Instance!.Home!.Marker.ShouldEqual("the-marker");
    [Fact] void should_still_auto_map_the_other_nested_property() => _scenario.Instance!.Home!.City.ShouldEqual("the-city");
}

[EventType]
public record NestedProfileOpened(string Name);

[EventType]
public record NestedProfileDetailed(string Marker, string City);

public record NestedHome(
    [SetFrom<NestedProfileDetailed>(nameof(NestedProfileDetailed.City))]
    string City,

    string Marker);

[Passive]
[FromEvent<NestedProfileOpened>]
public record NestedFencedProfile(
    Guid Id,
    string Name,

    [property: NoAutoMap]
    string? Marker,

    [Nested] NestedHome? Home);
