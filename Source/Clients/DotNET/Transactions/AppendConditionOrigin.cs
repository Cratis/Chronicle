// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Identifies the dependency that supplied an append condition.
/// </summary>
public enum AppendConditionOrigin
{
    /// <summary>
    /// A freshly folded protected read.
    /// </summary>
    ProtectedRead = 0,

    /// <summary>
    /// A caller-supplied condition.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// An aggregate rehydration condition.
    /// </summary>
    Aggregate = 2,

    /// <summary>
    /// A declared routing condition.
    /// </summary>
    DeclaredRouting = 3,

    /// <summary>
    /// A default routing condition.
    /// </summary>
    DefaultRouting = 4
}
