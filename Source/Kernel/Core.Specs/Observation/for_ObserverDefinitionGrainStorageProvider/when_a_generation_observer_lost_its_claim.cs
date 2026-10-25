// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider;

public class when_a_generation_observer_lost_its_claim : given.the_provider
{
    Exception _error;

    void Establish()
    {
        builds.ResolveObserver(observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId, observerKey.ObserverId).Returns(binding);
        claims.Claim(observerKey.EventStore, generation).Returns(registration);
    }

    async Task Because() => _error = await Catch.Exception(async () => await provider.WriteStateAsync("name", grainId, state));

    [Fact] void should_refuse_the_save() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
    [Fact] void should_not_save_the_definition() => observers.DidNotReceive().Save(Arg.Any<ObserverDefinition>());
}
