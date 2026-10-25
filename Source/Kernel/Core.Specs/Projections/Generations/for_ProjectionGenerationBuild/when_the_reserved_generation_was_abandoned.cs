// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A retry reconciles the original reservation with its abandoned catalog record.</summary>
public class when_the_reserved_generation_was_abandoned : given.a_generation_build_owner
{
    /// <summary>The generation originally reserved for this operation.</summary>
    MaterializationGeneration _generation;

    /// <summary>Models abandonment after reservation but before admission.</summary>
    async Task Establish()
    {
        _generation = (await Reserve()).Record!.Identity.Generation;
        await _catalog.TryAbandon(Store, _key, _generation);
    }

    /// <summary>Retries the same operation request.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The owner refuses to revive the abandoned reservation.</summary>
    [Fact] void should_be_abandoned() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Abandoned);

    /// <summary>The response uses the current catalog state, not the reservation's Building snapshot.</summary>
    [Fact] void should_return_the_authoritative_state() => _result.Record!.State.ShouldEqual(MaterializationGenerationState.Abandoned);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();

    /// <summary>The operation does not invent a replacement generation.</summary>
    [Fact] async Task should_keep_the_reserved_number() => (await Binding()).Next.ShouldEqual(_generation);

    /// <summary>No new candidate is created.</summary>
    [Fact] async Task should_not_allocate_again() => (await Binding()).Generations.Count.ShouldEqual(2);
}
