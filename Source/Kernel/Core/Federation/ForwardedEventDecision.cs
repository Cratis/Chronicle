// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents the outcome of evaluating an event arriving over a federation link.
/// </summary>
public enum ForwardedEventDecision
{
    /// <summary>
    /// The event is new and should be appended to the inbox.
    /// </summary>
    Accept = 0,

    /// <summary>
    /// The event originated at the receiving store, so accepting it would loop.
    /// </summary>
    RejectOwnOrigin = 1,

    /// <summary>
    /// The event has already been accepted, which happens on redelivery after a reconnect.
    /// </summary>
    Duplicate = 2
}
