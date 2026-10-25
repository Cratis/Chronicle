// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_the_registration_already_holds_the_claim : given.a_provider_with_identity_claims
{
    Exception _error;

    void Establish() => claims.Claim(observerKey.EventStore, registration).Returns(registration);

    async Task Because()
    {
        await provider.WriteStateAsync("name", grainId, state);
        _error = await Catch.Exception(async () => await provider.WriteStateAsync("name", grainId, state));
    }

    [Fact] void should_not_conflict() => _error.ShouldBeNull();
    [Fact] void should_not_rewrite_or_release_the_claim() => claims.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<ObserverIdentityClaim>());
    [Fact] void should_save_the_definition_each_time() => observers.Received(2).Save(definition);
}
