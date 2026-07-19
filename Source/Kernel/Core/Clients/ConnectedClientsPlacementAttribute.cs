// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Attribute to mark a grain as using the <see cref="ConnectedClientsPlacementStrategy"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConnectedClientsPlacementAttribute : PlacementAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClientsPlacementAttribute"/> class.
    /// </summary>
    public ConnectedClientsPlacementAttribute()
        : base(ConnectedClientsPlacementStrategy.Singleton)
    {
    }
}
