// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Generations.Bindings;

/// <summary>Adapts the initializer boundary to authoritative materialization storage.</summary>
/// <param name="keys">The materialization key resolver.</param>
/// <param name="bindings">The binding storage.</param>
public sealed class GenerationCatalog(IMaterializationKeys keys, IMaterializationBindingStorage bindings) : IGenerationCatalogSeam
{
    /// <inheritdoc/>
    public async Task<AdoptResult> TryAdoptFirst(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationRecord record)
    {
        var key = await keys.For(eventStore, @namespace, record.Identity.ProjectionId);
        return await bindings.TryAdoptFirst(eventStore, key, record) switch
        {
            FirstGenerationAdoption.Adopted => AdoptResult.Adopted,
            FirstGenerationAdoption.AlreadyAdopted => AdoptResult.AlreadyAdopted,
            FirstGenerationAdoption.Conflicting => AdoptResult.Conflicting,
            _ => throw new InvalidOperationException("Unknown first-generation adoption outcome.")
        };
    }

    /// <inheritdoc/>
    public async Task<bool> TrySetBindingEpoch(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projection, uint expected, uint next) =>
        await bindings.TrySetBindingEpoch(eventStore, await keys.For(eventStore, @namespace, projection), new(expected), new(next));
}
