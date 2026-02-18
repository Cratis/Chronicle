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
}
