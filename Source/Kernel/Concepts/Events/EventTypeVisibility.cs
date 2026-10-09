// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents whether an event type is part of the contract the owning event store exposes to others.
/// </summary>
public enum EventTypeVisibility
{
    /// <summary>
    /// The visibility is not known, which is what a client that predates visibility sends.
    /// </summary>
    /// <remarks>
    /// Treated as private, but the kernel does not enforce it for the outbox or the event log so existing systems keep working.
    /// </remarks>
    Unspecified = 0,

    /// <summary>
    /// The event type is local to the event store and must never leave it through the outbox.
    /// </summary>
    Private = 1,

    /// <summary>
    /// The event type is part of the public contract and is the only kind that can be appended to the outbox.
    /// </summary>
    Public = 2
}
