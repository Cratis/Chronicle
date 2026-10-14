// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Accumulated event-enrollment contributions on one label.
/// </summary>
[Flags]
internal enum ScopeOrigins
{
    /// <summary>
    /// No concrete contribution.
    /// </summary>
    None = 0,

    /// <summary>
    /// Caller-selected scope.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// Aggregate scope.
    /// </summary>
    Aggregate = 2,

    /// <summary>
    /// Routing scope.
    /// </summary>
    Routing = 4
}
