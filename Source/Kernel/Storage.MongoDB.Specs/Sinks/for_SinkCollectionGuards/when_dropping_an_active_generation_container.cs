// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_dropping_an_active_generation_container : Specification
{
    [Fact] void should_refuse_the_operation() =>
        Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(SinkCollectionOperation.Drop, "models-g2", "models-g2", [new GenerationContainerClaim(new("models-g2"), new(2), MaterializationGenerationState.Active)])).ShouldBeOfExactType<SinkCollectionOperationRefused>();
}
