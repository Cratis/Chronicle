// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents a placement strategy for connected clients tracking to guarantee each silo has its own
/// local grain activation, keyed by the silo address.
/// </summary>
[Serializable, GenerateSerializer, Immutable, SuppressReferenceTracking]
public class ConnectedClientsPlacementStrategy : PlacementStrategy
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="ConnectedClientsPlacementStrategy"/>.
    /// </summary>
    internal static readonly ConnectedClientsPlacementStrategy Singleton = new();
}
