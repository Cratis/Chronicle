// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// A [ClearWith] on the member holding a nested object clears the object, exactly as the class-level form on the
/// nested type does - it registers the same nested removal rather than a scalar clear of the member. The two
/// spellings exist so the owner can declare the clear without the shared nested type having to know the event.
/// </summary>
public class with_a_member_level_clear_on_a_nested_object : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(BadgeIssued), typeof(BadgeRevoked)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(VisitorPass));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_clear_the_whole_nested_object()
    {
        var eventType = event_types.GetEventTypeFor(typeof(BadgeRevoked)).ToContract();
        var nestedDef = _result.Nested[nameof(VisitorPass.Badge)];
        nestedDef.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_not_map_the_member_to_a_scalar_clear()
    {
        var eventType = event_types.GetEventTypeFor(typeof(BadgeRevoked)).ToContract();
        var fromDefinition = _result.From.SingleOrDefault(kvp => kvp.Key.IsEqual(eventType)).Value;
        (fromDefinition?.Properties.ContainsKey(nameof(VisitorPass.Badge)) ?? false).ShouldBeFalse();
    }
}

[EventType]
public record BadgeIssued(string Number);

[EventType]
public record BadgeRevoked;

[FromEvent<BadgeIssued>]
public record VisitorBadge(string Number);

public record VisitorPass(
    string VisitorName,
    [Nested]
    [ClearWith<BadgeRevoked>]
    VisitorBadge? Badge);
