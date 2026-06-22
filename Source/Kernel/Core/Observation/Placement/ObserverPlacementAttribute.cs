// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Chronicle.Observation.Placement;

/// <summary>
/// Attribute to mark a grain as using the <see cref="ObserverPlacementStrategy"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ObserverPlacementAttribute : PlacementAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverPlacementAttribute"/> class.
    /// </summary>
    public ObserverPlacementAttribute()
        : base(ObserverPlacementStrategy.Singleton)
    {
    }
}
