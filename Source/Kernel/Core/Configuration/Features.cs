// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents feature toggles for Chronicle.
/// </summary>
public class Features
{
    /// <summary>
    /// Whether the API is enabled. Default true.
    /// </summary>
    /// <remarks>If the API is disabled, the workbench is also disabled as it depends on the API.</remarks>
    public bool Api { get; init; } = true;

    /// <summary>
    /// Whether the Workbench is enabled. Default true.
    /// </summary>
    public bool Workbench { get; init; } = true;

    /// <summary>
    /// Whether the Changeset Storage is enabled. Default false.
    /// </summary>
    public bool ChangesetStorage { get; init; }

    /// <summary>
    /// Whether the OAuth Authority is enabled. Default true.
    /// </summary>
    /// <remarks>When enabled, provides an internal OpenIdDict-based OAuth authority for authentication.</remarks>
    public bool OAuthAuthority { get; init; } = true;

    /// <summary>
    /// Whether rebuilding a projection builds its next materialization generation alongside the active one. Default false.
    /// </summary>
    /// <remarks>
    /// Experimental and reserved. Rebuilding a namespaced projection is to build its next generation into its own destination
    /// while the active generation keeps handling live events. Only the seams exist so far, so rebuilds use the legacy replay
    /// path whatever its value. When enabled, every write of a MongoDB sink runs in a transaction fenced by the owner epoch of
    /// its generation. Fenced transactions are capped by ReadModels.MaxBatchSize and require a replica set or mongos;
    /// a standalone MongoDB server is refused at startup.
    /// </remarks>
    public bool ProjectionGenerations { get; init; }

    /// <summary>
    /// Whether replaying a single partition of an eligible projection rebuilds it in isolation, out of sight of the live read model. Default false.
    /// </summary>
    /// <remarks>
    /// Reserved. Nothing consults it yet, so partition replays behave as they always have whatever its value.
    /// </remarks>
    public bool IsolatedPartitionReplay { get; init; }
}
