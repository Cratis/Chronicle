// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.Generations;

/// <summary>Shared read-only admission and provider-independent floor comparison.</summary>
public static class GenerationFloorExtensions
{
    /// <summary>Refuses a store below its floor without writing anything.</summary>
    /// <param name="storage">The store.</param>
    /// <param name="support">The executing binary's support.</param>
    /// <returns>The admission task.</returns>
    /// <exception cref="KernelBelowGenerationFloor">The executing binary does not satisfy the store's floor.</exception>
    public static async Task EnsureAdmitted(this IEventStoreStorage storage, GenerationBinarySupport support)
    {
        var floor = await storage.GenerationFloor.Get();
        if (floor is not null && !support.Satisfies(floor))
        {
            throw new KernelBelowGenerationFloor(storage.EventStore, floor, support);
        }
    }

    /// <summary>Determines whether a write is unnecessary or forbidden before attempting CAS.</summary>
    /// <param name="current">The current floor.</param>
    /// <param name="advance">The request.</param>
    /// <returns>A terminal outcome, or null when CAS should be attempted.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The incarnation is unspecified or the expected revision cannot be advanced.</exception>
    public static GenerationFloorAdvanceResult? Compare(GenerationFloor? current, GenerationFloorAdvance advance)
    {
        var target = advance.Target;
        if (target.Incarnation == StoreIncarnation.Unspecified || advance.ExpectedRevision >= long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(advance));
        }
        if (current is not null)
        {
            if (current.Incarnation != target.Incarnation)
            {
                return new GenerationFloorWouldLower();
            }
            if (current.MinimumStorageProtocol.IsAtLeast(target.MinimumStorageProtocol) && current.MinimumWriterProtocol.IsAtLeast(target.MinimumWriterProtocol) && target.RequiredCapabilities.All(current.RequiredCapabilities.Contains))
            {
                return new GenerationFloorAlreadyAtOrAbove(current);
            }
            if (!target.MinimumStorageProtocol.IsAtLeast(current.MinimumStorageProtocol) || !target.MinimumWriterProtocol.IsAtLeast(current.MinimumWriterProtocol) || !current.RequiredCapabilities.All(target.RequiredCapabilities.Contains))
            {
                return new GenerationFloorWouldLower();
            }
        }
        if ((current?.Revision ?? 0) != advance.ExpectedRevision)
        {
            return new GenerationFloorRevisionConflict(current);
        }
        return null;
    }

    /// <summary>Builds the next auditable revision after comparison.</summary>
    /// <param name="advance">The validated request.</param>
    /// <returns>The next floor.</returns>
    public static GenerationFloor Next(this GenerationFloorAdvance advance) => advance.Target with { Revision = advance.ExpectedRevision + 1, AdvancedAt = DateTimeOffset.UtcNow, AdvancedBy = advance.RequestedBy, Reason = advance.Reason };
}
