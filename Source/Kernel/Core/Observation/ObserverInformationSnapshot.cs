// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a snapshot of an observer's definition, state and subscription, all read in the same grain turn.
/// </summary>
/// <param name="Definition">The <see cref="ObserverDefinition"/>.</param>
/// <param name="State">The <see cref="ObserverState"/>.</param>
/// <param name="IsSubscribed">Whether a subscription is installed, including while its setup is in progress.</param>
/// <param name="RunningStateChangedAt">When the running state last changed, or null if unknown.</param>
/// <remarks>
/// <see cref="LagSample"/> is appended after the positional parameters so earlier payloads, which lack it, still read.
/// </remarks>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.ObserverInformationSnapshot")]
public record ObserverInformationSnapshot(
    [Id(0)] ObserverDefinition Definition,
    [Id(1)] ObserverState State,
    [Id(2)] bool IsSubscribed,
    [Id(3)] DateTimeOffset? RunningStateChangedAt)
{
    /// <summary>
    /// Gets the latest measurement of the observer's progress, or null when the activation that answered predates measuring.
    /// </summary>
    [Id(4)]
    public ObserverLagSample? LagSample { get; init; }
}
