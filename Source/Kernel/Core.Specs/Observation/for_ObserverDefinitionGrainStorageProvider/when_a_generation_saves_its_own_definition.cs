// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_a_generation_saves_its_own_definition : given.a_provider_with_identity_claims
{
    void Establish()
    {
        builds.ResolveObserver(observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId, observerKey.ObserverId).Returns(binding);
        claims.Claim(observerKey.EventStore, generation).Returns(generation);
    }

    Task Because() => provider.WriteStateAsync("name", grainId, state);

    [Fact] void should_claim_the_identity_for_the_generation() => claims.Received(1).Claim(observerKey.EventStore, generation);
    [Fact] void should_save_the_definition() => observers.Received(1).Save(definition);
}
