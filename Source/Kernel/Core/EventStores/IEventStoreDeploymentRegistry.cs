// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Registrations;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Defines cluster-serialized deployment registration, keyed by event store name.
/// </summary>
public interface IEventStoreDeploymentRegistry : IGrainWithStringKey
{
    /// <summary>
    /// Checks a deployment and records changed state when accepted.
    /// </summary>
    /// <param name="generation">The generation, or 0 to bypass the check.</param>
    /// <param name="fingerprint">The latest fingerprint.</param>
    /// <returns>The registration decision.</returns>
    Task<DeploymentDecision> Register(DeploymentGeneration generation, ArtifactFingerprint fingerprint);

    /// <summary>
    /// Gets the stored deployment state.
    /// </summary>
    /// <returns>The stored state, or the empty state before registration.</returns>
    Task<DeploymentState> Get();
}
