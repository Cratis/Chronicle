// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_resolving_cataloged_generations : given.a_generation_projection_resolver
{
    IReadOnlyCollection<GenerationExecution> _result;

    void Establish()
    {
        MaterializationGenerationState[] states =
        [
            MaterializationGenerationState.Active,
            MaterializationGenerationState.Following,
            MaterializationGenerationState.Active,
            MaterializationGenerationState.Retired,
            MaterializationGenerationState.Building,
            MaterializationGenerationState.Archived
        ];
        var records = states.Select((state, index) => Record((uint)index + 1, state)).ToArray();
        foreach (var record in records.Where(_ => !_.Identity.Generation.IsFirst))
        {
            Bind(GenerationExecutions.Binding(_execution, record.Identity.Generation.Value));
        }

        var binding = new MaterializationBinding(GenerationExecutions.Key, "models", 1, "models", ServingEpoch.Unset, 7, null, null, records);
        _bindings.Get(Store, GenerationExecutions.Key).Returns(binding);
    }

    async Task Because() => _result = await _resolver.ResolveCataloged(Store, GenerationExecutions.Namespace, GenerationExecutions.Projection);

    [Fact] void should_resolve_only_followed_later_generations() => _result.Select(_ => _.Binding.Identity.Generation.Value).ShouldContainOnly([2u, 3u, 4u]);

    MaterializationGenerationRecord Record(uint generation, MaterializationGenerationState state) =>
        new(GenerationExecutions.Identity(generation), state, _execution.Revision, DateTimeOffset.UnixEpoch);
}
