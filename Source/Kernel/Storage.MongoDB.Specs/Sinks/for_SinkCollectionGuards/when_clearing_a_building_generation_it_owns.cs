// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_clearing_a_building_generation_it_owns : Specification
{
    [Fact] void should_allow_the_operation() =>
        Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(SinkCollectionOperation.Clear, "models-g2", "models-g2", [new GenerationContainerClaim(new("models-g2"), new(2), MaterializationGenerationState.Building)])).ShouldBeNull();
}
