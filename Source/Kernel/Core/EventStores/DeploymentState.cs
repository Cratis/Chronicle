// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Registrations;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the highest accepted generation and latest registered fingerprint for an event store.
/// </summary>
/// <param name="Generation">The highest accepted generation.</param>
/// <param name="Fingerprint">The latest fingerprint.</param>
[GenerateSerializer]
public record DeploymentState(
    [property: Id(0)] DeploymentGeneration Generation,
    [property: Id(1)] ArtifactFingerprint Fingerprint)
{
    /// <summary>
    /// Gets the state before any deployment has been registered.
    /// </summary>
    public static readonly DeploymentState Empty = new(DeploymentGeneration.NotSet, ArtifactFingerprint.NotSet);
}
