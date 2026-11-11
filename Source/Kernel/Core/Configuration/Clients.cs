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
    /// Gets the interval in seconds between checks of a connected client's subscription that is known to be intact.
    /// </summary>
    /// <remarks>
    /// This is the safety net for a subscription lost without a silo leaving the cluster. A subscription that is not
    /// confirmed intact - a new one, one that was just restored, or one checked while a silo left - is checked every
    /// <see cref="ReviseIntervalSeconds"/> instead. Values below <see cref="ReviseIntervalSeconds"/> are raised to it.
    /// </remarks>
    public int SubscriptionCheckIntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Gets the number of seconds after a silo leaves the cluster during which every connected client's subscription
    /// on the remaining silos is checked every <see cref="ReviseIntervalSeconds"/>.
    /// </summary>
    /// <remarks>
    /// A silo that leaves takes the observer activations it hosted with it, and the surviving silos deactivate the
    /// activations whose directory entries it owned shortly afterwards. The window covers both.
    /// </remarks>
    public int SubscriptionSettleSeconds { get; init; } = 30;

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
