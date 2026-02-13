// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Defines the types of constraints.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Represents unknown constraint.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Represents a unique constraint.
    /// </summary>
    Unique = 1,

    /// <summary>
    /// Represents a unique event type constraint.
    /// </summary>
    UniqueEventType = 2,

    /// <summary>
    /// Represents a schema validation constraint.
    /// </summary>
    Schema = 3
}
