// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Reads original build intents for the current binding without changing generation state or acquiring authority.
/// </summary>
/// <param name="enablement">The operational generation flag.</param>
/// <param name="keys">The current materialization identities.</param>
/// <param name="bindings">The authoritative generation catalog.</param>
/// <param name="builds">The persisted build intent reader.</param>
/// <remarks>
/// Registered only with the generation catalog; providers without a catalog must not discover it by convention.
/// </remarks>
[IgnoreConvention]
public sealed class GenerationBuildRecovery(
    IGenerationEnablement enablement,
    IMaterializationKeys keys,
    IMaterializationBindingStorage bindings,
    IGenerationBuildStorage builds) : IGenerationBuildRecovery
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<RecoverableGenerationBuild>> Resolve(EventStoreName store, EventStoreNamespaceName ns, ProjectionId projection)
    {
        if (!enablement.Enabled) return [];
        MaterializationKey key;
        try
        {
            key = await keys.For(store, ns, projection);
        }
        catch (GenerationFloorNotEstablished)
        {
            return [];
        }

        var binding = await bindings.Get(store, key);
        if (binding is null) return [];
        var outcomes = new List<RecoverableGenerationBuild>();
        foreach (var record in binding.Generations.Where(_ => _.Identity.Generation.Value > MaterializationGeneration.First.Value && _.State == MaterializationGenerationState.Building))
        {
            outcomes.Add(await ReadIntent(store, key, record));
        }

        return outcomes;
    }

    /// <summary>
    /// Reads one original request, translating only inconsistent persisted intent into a refusal.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="key">The current materialization key.</param>
    /// <param name="record">The Building catalog record.</param>
    /// <returns>The original request and record, or the record and refusal.</returns>
    async Task<RecoverableGenerationBuild> ReadIntent(EventStoreName store, MaterializationKey key, MaterializationGenerationRecord record)
    {
        GenerationBuildRequest? request;
        try
        {
            request = await builds.GetBuildRequest(store, key, record.Identity.Generation);
        }
        catch (GenerationBuildConflict)
        {
            return new(record, Refusal: GenerationBuildRecoveryRefusal.IntentInconsistent);
        }

        if (request is null) return new(record, Refusal: GenerationBuildRecoveryRefusal.IntentMissing);
        if (request.Key != key) return new(record, Refusal: GenerationBuildRecoveryRefusal.KeySuperseded);
        return new(record, request);
    }
}
