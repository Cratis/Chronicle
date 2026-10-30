// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Registrations;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the decision for a deployment registration.
/// </summary>
/// <param name="Accepted">Whether registration is accepted.</param>
/// <param name="Stored">The state before registration.</param>
/// <param name="ToRecord">The changed state to record, or null when nothing changes.</param>
[GenerateSerializer]
public record DeploymentDecision(
    [property: Id(0)] bool Accepted,
    [property: Id(1)] DeploymentState Stored,
    [property: Id(2)] DeploymentState? ToRecord)
{
    /// <summary>
    /// Decides whether a deployment can register and whether its state needs recording.
    /// </summary>
    /// <param name="stored">The stored state.</param>
    /// <param name="generation">The sent generation, or 0 to bypass the check.</param>
    /// <param name="fingerprint">The latest fingerprint.</param>
    /// <returns>The registration decision.</returns>
    public static DeploymentDecision Decide(DeploymentState stored, DeploymentGeneration generation, ArtifactFingerprint fingerprint)
    {
        if (generation.IsSet && generation.Value < stored.Generation.Value) return new(false, stored, null);

        var next = new DeploymentState(generation.IsSet ? generation : stored.Generation, fingerprint);
        return new(true, stored, next == stored ? null : next);
    }
}
