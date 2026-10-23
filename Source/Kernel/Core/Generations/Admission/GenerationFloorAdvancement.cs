// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Admission;

/// <summary>Advances a floor only explicitly, after independently checking membership.</summary>
/// <param name="storage">The cluster storage.</param>
/// <param name="membership">The membership probe, unknown by default.</param>
public sealed class GenerationFloorAdvancement(IStorage storage, IGenerationMembershipProbe membership) : IGenerationFloorAdvancement
{
    /// <inheritdoc/>
    public async Task<GenerationFloorAdvanceResult> Advance(EventStoreName eventStore, GenerationFloorAdvance advance)
    {
        var evidence = await membership.Probe(advance.Target);
        return evidence.IsUniform
            ? await storage.GetEventStore(eventStore).GenerationFloor.Advance(advance with { Evidence = evidence })
            : new GenerationFloorMembershipNotUniform(evidence.Reason);
    }
}
