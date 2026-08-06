// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3571 — a child collection typed as a
/// <see cref="DerivedTypeAttribute"/> base with a single implementation must round-trip through the
/// correct concrete type without an explicit <c>[SetValue]</c> discriminator.
/// </summary>
public class and_child_is_polymorphic_with_a_single_derived_type : Specification
{
    ReadModelScenario<SliceWithActors> _scenario;
    EventSourceId _sliceId;
    Guid _sliceGuid;
    Guid _actorGuid;

    void Establish()
    {
        _scenario = new ReadModelScenario<SliceWithActors>();
        _sliceGuid = Guid.NewGuid();
        _actorGuid = Guid.NewGuid();
        _sliceId = new EventSourceId(_sliceGuid);
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_sliceId)
            .Events(new SliceCreated(_sliceGuid));

        await _scenario.Given
            .ForEventSource(new EventSourceId(_actorGuid))
            .Events(new SliceUiActorUpdated(_sliceGuid, _actorGuid, "Jane"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_one_actor() => _scenario.Instance.Actors.Count.ShouldEqual(1);
    [Fact] void should_resolve_the_actor_to_its_concrete_derived_type() => _scenario.Instance.Actors[0].ShouldBeOfExactType<UserExperienceActor>();
    [Fact] void should_preserve_the_actor_display_name() => ((UserExperienceActor)_scenario.Instance.Actors[0]).DisplayName.ShouldEqual("Jane");
}
