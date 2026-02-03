// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences;

/// <summary>
/// Well-known event sequence IDs.
/// </summary>
public static class WellKnownEventSequences
{
    /// <summary>
    /// The event sequence id for the event log.
    /// </summary>
    public const string EventLog = "event-log";

    /// <summary>
    /// The event sequence id for the system.
    /// </summary>
    public const string System = "system";

    /// <summary>
    /// The event sequence id for the outbox.
    /// </summary>
    public const string Outbox = "outbox";
}
