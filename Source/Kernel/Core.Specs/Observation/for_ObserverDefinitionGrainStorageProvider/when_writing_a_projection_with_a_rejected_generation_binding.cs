// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_writing_a_projection_with_a_rejected_generation_binding : given.a_provider_with_identity_claims
{
    Exception _error;

    void Establish()
    {
        builds.ResolveObserver(observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId, observerKey.ObserverId).Returns(binding);
        claims.Get(observerKey.EventStore, observerKey.ObserverId).Returns(registration);
        claims.Claim(observerKey.EventStore, registration).Returns(registration);
    }

    async Task Because() => _error = await Catch.Exception(() => provider.WriteStateAsync("name", grainId, state));

    [Fact] void should_not_conflict() => _error.ShouldBeNull();
    [Fact] void should_claim_the_identity_for_the_registration() => claims.Received(1).Claim(observerKey.EventStore, registration);
    [Fact] void should_not_claim_the_identity_for_the_generation() => claims.DidNotReceive().Claim(Arg.Any<EventStoreName>(), generation);
    [Fact] void should_save_the_definition() => observers.Received(1).Save(definition);
}
