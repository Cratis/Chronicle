// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Attribute to mark a grain as using the <see cref="ServerInstancePlacementStrategy"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ServerInstancePlacementAttribute : PlacementAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerInstancePlacementAttribute"/> class.
    /// </summary>
    public ServerInstancePlacementAttribute()
        : base(ServerInstancePlacementStrategy.Singleton)
    {
    }
}
