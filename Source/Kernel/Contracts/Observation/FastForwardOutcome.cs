// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the outcome of fast forwarding an observer.
/// </summary>
public enum FastForwardOutcome
{
    /// <summary>
    /// The observer was moved to the tail of the event sequence.
    /// </summary>
    FastForwarded = 0,

    /// <summary>
    /// The observer was already at the tail of the event sequence, so nothing was changed.
    /// </summary>
    AlreadyAtTail = 1,

    /// <summary>
    /// The observer is replaying, so it cannot be fast forwarded until the replay has completed.
    /// </summary>
    ObserverReplaying = 2,

    /// <summary>
    /// The observer is retired or removed, so nothing was changed.
    /// </summary>
    NotAvailable = 3
}
