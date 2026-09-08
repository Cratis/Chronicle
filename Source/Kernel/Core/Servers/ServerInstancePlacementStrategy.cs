// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Represents a placement strategy for server instance diagnostics to guarantee each silo has its own
/// local grain activation, keyed by the silo address.
/// </summary>
[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class ServerInstancePlacementStrategy : PlacementStrategy
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="ServerInstancePlacementStrategy"/>.
    /// </summary>
    internal static readonly ServerInstancePlacementStrategy Singleton = new();
}
