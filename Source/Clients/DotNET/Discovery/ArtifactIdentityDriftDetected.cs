// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// The exception that is thrown when Warning-level reactor or event type identity changes prevent registration under the Fail policy.
/// </summary>
/// <param name="drift">The Warning-level identity changes detected before registration.</param>
public class ArtifactIdentityDriftDetected(IImmutableList<ArtifactIdentityDrift> drift) : Exception(
    $"Likely artifact identity changes: {string.Join("; ", drift.Select(_ => $"{_.Kind}: {_.PreviousId} -> {_.CurrentId}"))}. " +
    "Preserve reactor positions with [Reactor(\"<previous id>\")], or remove orphaned observers with Observers.Remove(\"<previous id>\"). " +
    "Event type history remains under the previous id. Review the changes or use the Warn policy to proceed.");
