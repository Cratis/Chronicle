// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for connected clients.
/// </summary>
public class Clients
{
    /// <summary>
    /// Gets the interval in seconds between revisions of the connected clients state.
    /// </summary>
    /// <remarks>
    /// The revision loop prunes stale clients and removes expired connection reservations.
    /// </remarks>
    public int ReviseIntervalSeconds { get; init; } = 2;

    /// <summary>
    /// Gets the interval in seconds between checks of whether a reactor or reducer registration is owned on another event sequence.
    /// </summary>
    /// <remarks>
    /// The keep-alive of a client reactor or reducer runs every <see cref="ReviseIntervalSeconds"/>. Reading the stored
    /// observer definition on every keep-alive costs a storage read per registration and client, even when idle, so the
    /// check runs at most once per this interval: on the first keep-alive after activation, then once the interval has
    /// elapsed. A registration waiting in standby retries taking over at the same pace. A newer owner on another sequence,
    /// or its departure, is therefore noticed within this interval plus <see cref="ReviseIntervalSeconds"/>.
    /// Zero or less checks on every keep-alive.
    /// </remarks>
    public int OwnerCheckIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the number of seconds a connection reservation is kept before it expires.
    /// </summary>
    /// <remarks>
    /// A reservation holds a slot for a client that is in the process of connecting, so the
    /// connection count reflects the client before its real connection has been registered.
    /// </remarks>
    public int ReservationTtlSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the number of seconds a client can go unseen before it is considered disconnected.
    /// </summary>
    /// <remarks>
    /// The revision loop disconnects any client whose last-seen time is older than this threshold.
    /// </remarks>
    public int StaleThresholdSeconds { get; init; } = 5;

    /// <summary>
    /// Gets the interval in seconds between observations of the connected clients for observers.
    /// </summary>
    public int ObserveIntervalSeconds { get; init; } = 1;

    /// <summary>
    /// Gets the interval in seconds between keep-alive messages sent to a connected client.
    /// </summary>
    public int KeepAliveIntervalSeconds { get; init; } = 1;
}
