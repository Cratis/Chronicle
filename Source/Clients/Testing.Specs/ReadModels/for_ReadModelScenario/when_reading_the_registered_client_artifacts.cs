// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;
using Cratis.Chronicle.Testing.Reactors;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that a scenario hands out the very registry Chronicle discovered its artifacts from, so a spec can
/// ask what was registered instead of re-deriving it by reflecting over its own assemblies. The classification a
/// consumer re-deriving it would most easily get wrong is pinned here as well: a property-level <c>[Unique]</c>
/// makes an event type a unique constraint, while a class-level <c>[Unique]</c> makes it a unique event type
/// constraint, and the two never overlap.
/// </summary>
public class when_reading_the_registered_client_artifacts : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    IClientArtifactsProvider _artifacts;
    IClientArtifactsProvider _artifactsReadAgain;

    void Establish() => _scenario = new ReadModelScenario<SimpleModule>();

    void Because()
    {
        _artifacts = _scenario.ClientArtifactsProvider;
        _artifactsReadAgain = _scenario.ClientArtifactsProvider;
    }

    [Fact] void should_hand_out_the_registry_the_scenario_itself_uses() => ReferenceEquals(_artifacts, Defaults.Instance.ClientArtifactsProvider).ShouldBeTrue();
    [Fact] void should_hand_out_the_same_registry_on_every_read() => ReferenceEquals(_artifacts, _artifactsReadAgain).ShouldBeTrue();
    [Fact] void should_not_materialize_a_read_model_by_being_read() => _scenario.Instance.ShouldBeNull();
    [Fact] void should_know_the_read_model_under_test_is_model_bound() => _artifacts.ModelBoundProjections.ShouldContain(typeof(SimpleModule));
    [Fact] void should_know_the_registered_event_types() => _artifacts.EventTypes.ShouldContain(typeof(ModuleCreated));
    [Fact] void should_know_the_registered_reactors() => _artifacts.Reactors.ShouldContain(typeof(ReservationReactor));
    [Fact] void should_know_the_registered_constraints() => _artifacts.ConstraintTypes.ShouldContain(typeof(UniqueLicenseKey));
    [Fact] void should_classify_a_property_level_unique_as_a_unique_constraint() => _artifacts.UniqueConstraints.ShouldContain(typeof(SubscriberRegistered));
    [Fact] void should_not_classify_a_property_level_unique_as_a_unique_event_type_constraint() => _artifacts.UniqueEventTypeConstraints.ShouldNotContain(typeof(SubscriberRegistered));
    [Fact] void should_classify_a_class_level_unique_as_a_unique_event_type_constraint() => _artifacts.UniqueEventTypeConstraints.ShouldContain(typeof(SubscriptionActivated));
    [Fact] void should_not_classify_a_class_level_unique_as_a_unique_constraint() => _artifacts.UniqueConstraints.ShouldNotContain(typeof(SubscriptionActivated));
}
