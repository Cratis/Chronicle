// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_a_secondary_event_creates_a_derived_child : Specification
{
    ReadModelScenario<SliceWithActors> _scenario;
    Guid _sliceId;
    Guid _primaryActorId;

    void Establish()
    {
        _scenario = new();
        _sliceId = Guid.NewGuid();
        _primaryActorId = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(new EventSourceId(_sliceId))
            .Events(new SliceCreated(_sliceId), new SliceUiActorImported(_sliceId, "Imported"));
        await _scenario.Given.ForEventSource(new EventSourceId(_primaryActorId))
            .Events(new SliceUiActorUpdated(_sliceId, _primaryActorId, "Primary"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_both_children() => _scenario.Instance.Actors.Count.ShouldEqual(2);
    [Fact] void should_materialize_the_secondary_child_as_its_derived_type() => Imported().ShouldBeOfExactType<UserExperienceActor>();
    [Fact] void should_preserve_the_secondary_child_value() => Imported().DisplayName.ShouldEqual("Imported");
    [Fact] void should_preserve_the_primary_child_value() => _scenario.Instance.Actors.Cast<UserExperienceActor>().Single(actor => actor.ActorId == _primaryActorId).DisplayName.ShouldEqual("Primary");

    UserExperienceActor Imported() => (UserExperienceActor)_scenario.Instance.Actors.Single(actor => ((UserExperienceActor)actor).ActorId == _sliceId);
}
