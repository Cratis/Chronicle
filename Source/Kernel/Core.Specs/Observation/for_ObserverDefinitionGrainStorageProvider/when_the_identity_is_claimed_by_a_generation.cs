// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_the_identity_is_claimed_by_a_generation : given.a_provider_with_identity_claims
{
    Exception _error;

    void Establish() => claims.Claim(observerKey.EventStore, registration).Returns(generation);

    async Task Because() => _error = await Catch.Exception(async () => await provider.WriteStateAsync("name", grainId, state));

    [Fact] void should_refuse_the_registration() => _error.ShouldBeOfExactType<ObserverIdentityClaimedByGeneration>();
    [Fact] void should_not_save_the_definition() => observers.DidNotReceive().Save(Arg.Any<ObserverDefinition>());
}
