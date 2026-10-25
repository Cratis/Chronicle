// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_no_claims_storage_is_registered : given.the_provider
{
    void Establish() => provider = new(storage, enablement, null, builds);

    Task Because() => provider.WriteStateAsync("name", grainId, state);

    [Fact] void should_save_the_definition() => observers.Received(1).Save(definition);
    [Fact] void should_not_claim_the_identity() => claims.DidNotReceive().Claim(Arg.Any<EventStoreName>(), Arg.Any<ObserverIdentityClaim>());
}
