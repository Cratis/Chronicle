// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Chronicle.EventSequences.Placement;

/// <summary>
/// Attribute to mark a grain as using the <see cref="EventSequencePlacementStrategy"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventSequencePlacementAttribute : PlacementAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequencePlacementAttribute"/> class.
    /// </summary>
    public EventSequencePlacementAttribute()
        : base(EventSequencePlacementStrategy.Singleton)
    {
    }
}
