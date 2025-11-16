// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the clustering configuration for Orleans.
/// </summary>
public class Clustering
{
    /// <summary>
    /// Gets a value indicating whether clustering is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets the cluster identifier.
    /// </summary>
    public string ClusterId { get; init; } = "chronicle-cluster";

    /// <summary>
    /// Gets the service identifier.
    /// </summary>
    public string ServiceId { get; init; } = "chronicle";
}
