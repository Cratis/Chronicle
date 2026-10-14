// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Identifies the contribution that supplied a concurrency scope.
/// </summary>
public enum ConcurrencyScopeOrigin
{
    /// <summary>
    /// Caller-selected scope.
    /// </summary>
    Explicit = 0,

    /// <summary>
    /// Scope captured while rehydrating an aggregate.
    /// </summary>
    Aggregate = 1,

    /// <summary>
    /// Scope supplied by event routing.
    /// </summary>
    Routing = 2,

    /// <summary>
    /// Scope enrolled through a decision read.
    /// </summary>
    DecisionRead = 3
}
