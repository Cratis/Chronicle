// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents what kind of initiator caused an event.
/// </summary>
public enum InitiatorType
{
    /// <summary>
    /// The initiator could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A person caused the event.
    /// </summary>
    User = 1,

    /// <summary>
    /// An autonomous agent caused the event, possibly on behalf of a person.
    /// </summary>
    Agent = 2,

    /// <summary>
    /// Chronicle itself, or the hosting application, caused the event with nobody behind it.
    /// </summary>
    System = 3
}
