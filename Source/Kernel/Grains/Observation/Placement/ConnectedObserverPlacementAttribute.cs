// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Kernel.Grains.Observation.Placement;

/// <summary>
/// Attribute to mark a grain as using the <see cref="ConnectedObserverPlacementStrategy"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConnectedObserverPlacementAttribute : PlacementAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedObserverPlacementAttribute"/> class.
    /// </summary>
    public ConnectedObserverPlacementAttribute()
        : base(ConnectedObserverPlacementStrategy.Singleton)
    {
    }
}
