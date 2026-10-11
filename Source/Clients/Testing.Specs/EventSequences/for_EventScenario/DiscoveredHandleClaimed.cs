// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A unique handle was claimed.
/// </summary>
/// <param name="Handle">The claimed handle.</param>
[EventType]
public record DiscoveredHandleClaimed([property: Unique(DiscoveredHandleClaimed.ConstraintName)] string Handle)
{
    /// <summary>
    /// The unique handle constraint.
    /// </summary>
    public const string ConstraintName = "discovered-handle";
}
