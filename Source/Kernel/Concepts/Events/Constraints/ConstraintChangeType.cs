// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents the type of change that occurred for a constraint definition.
/// </summary>
public enum ConstraintChangeType
{
    /// <summary>
    /// No change.
    /// </summary>
    None = 0,

    /// <summary>
    /// An event type was added to the definition.
    /// </summary>
    EventAdded = 1,

    /// <summary>
    /// An event type was removed from the definition.
    /// </summary>
    EventRemoved = 2,

    /// <summary>
    /// The indexed properties changed.
    /// </summary>
    IndexedPropertiesChanged = 3
}
