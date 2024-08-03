// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a constraint type.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Represents no constraint.
    /// </summary>
    None = 0,

    /// <summary>
    /// Represents a unique constraint.
    /// </summary>
    Unique = 1,

    /// <summary>
    /// Represents a unique event type constraint.
    /// </summary>
    UniqueEventType = 2
}
