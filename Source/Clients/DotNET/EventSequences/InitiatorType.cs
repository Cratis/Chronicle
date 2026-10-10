// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes the kind of initiator at the head of a caused-by chain.
/// </summary>
public enum InitiatorType
{
    /// <summary>
    /// The initiator could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// An identity without an on-behalf-of chain caused the event.
    /// </summary>
    User = 1,

    /// <summary>
    /// An identity acting on behalf of another identity caused the event.
    /// </summary>
    Agent = 2,

    /// <summary>
    /// Chronicle caused the event.
    /// </summary>
    System = 3
}
