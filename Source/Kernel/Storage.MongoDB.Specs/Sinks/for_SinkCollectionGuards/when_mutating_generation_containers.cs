// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_mutating_generation_containers : Specification
{
    [Theory]
    [InlineData(MaterializationGenerationState.Building)]
    [InlineData(MaterializationGenerationState.Following)]
    [InlineData(MaterializationGenerationState.Active)]
    [InlineData(MaterializationGenerationState.Retired)]
    void should_refuse_drop_and_rename(MaterializationGenerationState state)
    {
        GenerationContainerClaim[] claims = [new(new("models-g2"), new(2), state)];
        foreach (var operation in new[] { SinkCollectionOperation.Drop, SinkCollectionOperation.Rename })
            Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(operation, "models-g2", "models-g2", claims)).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    }
    [Theory]
    [InlineData(MaterializationGenerationState.Abandoned)]
    [InlineData(MaterializationGenerationState.Archived)]
    [InlineData(MaterializationGenerationState.Removed)]
    void should_leave_unprotected_states_to_the_existing_rules(MaterializationGenerationState state)
    {
        foreach (var operation in Enum.GetValues<SinkCollectionOperation>())
            Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(operation, "models-g2", "other", [new(new("models-g2"), new(2), state)])).ShouldBeNull();
    }
    [Fact] void should_refuse_clearing_another_builders_container() =>
        Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(SinkCollectionOperation.Clear, "models-g2", "other", [new(new("models-g2"), new(2), MaterializationGenerationState.Building)])).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Theory]
    [InlineData(MaterializationGenerationState.Following)]
    [InlineData(MaterializationGenerationState.Active)]
    [InlineData(MaterializationGenerationState.Retired)]
    void should_allow_index_ensures_on_served_containers(MaterializationGenerationState state) =>
        Catch.Exception(() => SinkCollectionGuards.ThrowIfGenerationProtected(SinkCollectionOperation.CreateIndexes, "models-g2", "other", [new(new("models-g2"), new(2), state)])).ShouldBeNull();
}
