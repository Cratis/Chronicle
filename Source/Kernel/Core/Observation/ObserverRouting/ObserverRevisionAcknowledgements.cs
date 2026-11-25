// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents how the active silos of the cluster answered the publication of a revision of an observer's definition.
/// </summary>
/// <param name="Acknowledged">The number of silos whose routing table holds the revision.</param>
/// <param name="Lagging">The silos that answered with an older revision, or did not answer.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.ObserverRouting.ObserverRevisionAcknowledgements")]
public record ObserverRevisionAcknowledgements([Id(0)] int Acknowledged, [Id(1)] IReadOnlyCollection<SiloAddress> Lagging)
{
    /// <summary>
    /// Gets whether every active silo acknowledged the revision.
    /// </summary>
    public bool IsComplete => Lagging.Count == 0;
}
