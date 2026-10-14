// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Distinguishes acquisitions from individually resolved routing entries.
/// </summary>
internal enum ReadProtectionContributionKind
{
    /// <summary>
    /// A protected read with a global boundary.
    /// </summary>
    ProtectedRead = 0,

    /// <summary>
    /// An exact caller-supplied condition.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// An aggregate rehydration condition.
    /// </summary>
    Aggregate = 2,

    /// <summary>
    /// Routing resolved without declared dimensions.
    /// </summary>
    DefaultRouting = 3,

    /// <summary>
    /// Routing resolved with declared dimensions.
    /// </summary>
    DeclaredRouting = 4,

    /// <summary>
    /// Routing resolved by a custom strategy.
    /// </summary>
    CustomRouting = 5
}
