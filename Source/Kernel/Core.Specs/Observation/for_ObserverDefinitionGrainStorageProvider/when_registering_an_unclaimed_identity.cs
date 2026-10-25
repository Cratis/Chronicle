// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_registering_an_unclaimed_identity : given.a_provider_with_identity_claims
{
    void Establish() => claims.Claim(observerKey.EventStore, registration).Returns(registration);

    Task Because() => provider.WriteStateAsync("name", grainId, state);

    [Fact]
    void should_claim_the_identity_immediately_before_saving() => Received.InOrder(() =>
    {
        claims.Claim(observerKey.EventStore, registration);
        observers.Save(definition);
    });
}
