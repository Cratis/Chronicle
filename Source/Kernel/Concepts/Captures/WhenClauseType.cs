// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the supported kinds of append conditions.
/// </summary>
public enum WhenClauseType
{
    /// <summary>
    /// A single property changed.
    /// </summary>
    PropertyChange = 0,

    /// <summary>
    /// Any of the listed properties changed.
    /// </summary>
    LogicalOr = 1,

    /// <summary>
    /// All of the listed properties changed.
    /// </summary>
    LogicalAnd = 2,

    /// <summary>
    /// A property changed from one value to another.
    /// </summary>
    ValueTransition = 3,

    /// <summary>
    /// An item was added.
    /// </summary>
    Added = 4,

    /// <summary>
    /// An item was removed.
    /// </summary>
    Removed = 5,

    /// <summary>
    /// A free-form expression evaluated to true.
    /// </summary>
    Expression = 6
}
