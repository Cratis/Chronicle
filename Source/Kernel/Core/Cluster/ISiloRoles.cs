// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Monads;

namespace Cratis.Chronicle.Cluster;

/// <summary>
/// Provides the advertised roles of a silo without exposing Orleans metadata details.
/// </summary>
public interface ISiloRoles
{
    /// <summary>
    /// Gets the advertised roles, or none when the metadata is unavailable or unrecognized.
    /// </summary>
    /// <param name="siloAddress">The silo to look up.</param>
    /// <returns>The known roles, or none for an unknown silo.</returns>
    Option<ClusterRoles> GetRoles(SiloAddress siloAddress);
}
