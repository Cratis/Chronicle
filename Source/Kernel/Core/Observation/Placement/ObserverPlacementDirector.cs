// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Observation.Placement;

/// <summary>
/// Represents a placement director for observers that respects the Clustering configuration.
/// </summary>
/// <param name="options">Chronicle options containing clustering configuration.</param>
public class ObserverPlacementDirector(IOptions<ChronicleOptions> options) : IPlacementDirector
{
    /// <inheritdoc/>
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        // Check if Observers are allowed on this silo
        if (!options.Value.Clustering.Observers)
        {
            throw new InvalidOperationException(
                "Observers placement is disabled in clustering configuration for this silo.");
        }

        // Use random placement among available silos
        // Orleans will filter to only silos where this director doesn't throw
        var selectedSilo = context.GetCompatibleSilos(target).OrderBy(_ => Random.Shared.Next()).First();
        return Task.FromResult(selectedSilo);
    }
}
