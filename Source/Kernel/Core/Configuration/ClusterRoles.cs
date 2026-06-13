// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the roles configuration for cluster nodes.
/// </summary>
public class ClusterRoles
{
    /// <summary>
    /// Gets whether Event Sequences can be placed on this node.
    /// When true, event sequence grains can be activated on this silo.
    /// When false, event sequence grains will not be placed on this silo.
    /// </summary>
    public bool EventSequences { get; set; } = true;

    /// <summary>
    /// Gets whether Observers can be placed on this node.
    /// When true, observer grains (reactors, reducers, projections) can be activated on this silo.
    /// When false, observer grains will not be placed on this silo.
    /// </summary>
    public bool Observers { get; set; } = true;
}
