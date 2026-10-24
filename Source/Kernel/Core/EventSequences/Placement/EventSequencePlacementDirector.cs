// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.EventSequences.Placement;

/// <summary>
/// Represents a placement director for event sequences that respects the Clustering configuration.
/// </summary>
/// <param name="options">Chronicle options containing clustering configuration.</param>
/// <param name="localSiloDetails">Details about the local silo used to exclude it from candidates when it does not support event sequences.</param>
public class EventSequencePlacementDirector(IOptions<ChronicleOptions> options, ILocalSiloDetails localSiloDetails) : IPlacementDirector
{
    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        IEnumerable<SiloAddress> candidates = context.GetCompatibleSilos(target);

        if (!options.Value.Clustering.Roles.EventSequences)
        {
            // This silo is not configured to host event sequence grains. Exclude it from the
            // candidate list so the grain is placed on a silo that is configured as an event
            // sequence host. In a single-silo deployment every silo is also an event sequence
            // host, so this path is never reached and no candidates are lost.
            candidates = candidates.Where(s => s != localSiloDetails.SiloAddress);
        }

        var selected = candidates.OrderBy(_ => Random.Shared.Next()).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No event-sequence-capable silos are available for event sequence grain placement. " +
                "Ensure at least one silo has EventSequences enabled in its Clustering configuration.");

        return Task.FromResult(selected);
    }
}
