// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_generations_are_disabled : given.a_provider_with_identity_claims
{
    void Establish() => enablement.Enabled.Returns(false);

    Task Because() => provider.WriteStateAsync("name", grainId, state);

    [Fact] void should_not_claim_the_identity() => claims.DidNotReceive().Claim(Arg.Any<EventStoreName>(), Arg.Any<ObserverIdentityClaim>());
    [Fact] void should_save_the_definition() => observers.Received(1).Save(definition);
}
