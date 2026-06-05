// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for clustering.
/// </summary>
public class Clustering
{
    /// <summary>
    /// Gets the cluster roles configuration.
    /// </summary>
    public ClusterRoles Roles { get; set; } = new();
}
