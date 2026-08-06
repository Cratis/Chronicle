// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3571 — AutoMap only ever wires properties
/// that exist by name on both the event and the child type, so it can never discover the
/// <c>_derivedTypeId</c> discriminator: it is a serialization-time artifact <see cref="DerivedTypeAttribute"/>
/// adds, not a real member of the interface or its implementation. When the child collection's item type
/// has exactly one <see cref="DerivedTypeAttribute"/> implementation, the builder must stamp a constant
/// discriminator mapping automatically, the same way an explicit <c>[SetValue]</c> would.
/// </summary>
public class with_a_polymorphic_child_having_a_single_derived_type : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(SliceUiActorUpdated)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SliceWithActors));

    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_stamp_the_derived_type_discriminator_as_a_constant_value()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SliceUiActorUpdated)).ToContract();
        var childrenDef = _result.Children[nameof(SliceWithActors.Actors)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties["_derivedTypeId"].ShouldEqual("$value(userExperience)");
    }
}

public interface IActor;

[DerivedType("userExperience", typeof(IActor))]
public sealed record UserExperienceActor(
    [Key] Guid ActorId,
    [SetFrom<SliceUiActorUpdated>(nameof(SliceUiActorUpdated.DisplayName))] string DisplayName) : IActor;

[EventType]
public record SliceUiActorUpdated(Guid SliceId, Guid ActorId, string DisplayName);

[Passive]
[FromEvent<SliceCreated>]
public sealed record SliceWithActors(
    [Key] Guid Id,
    [ChildrenFrom<SliceUiActorUpdated>(key: nameof(SliceUiActorUpdated.ActorId), parentKey: nameof(SliceUiActorUpdated.SliceId))]
    IReadOnlyList<IActor> Actors);

[EventType]
public record SliceCreated(Guid SliceId);
