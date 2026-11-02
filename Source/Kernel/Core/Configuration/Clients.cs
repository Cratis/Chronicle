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

    /// <summary>
    /// Gets a value indicating whether a client that reports running with a debugger is exempt from connection expiry.
    /// </summary>
    /// <remarks>
    /// Set to false to ignore client debugger claims for connection expiry, so such a client is expired like any other.
    /// </remarks>
    public bool AllowDebuggerExemption { get; init; } = true;

    /// <summary>
    /// Gets whether a connection is exempt from connection expiry.
    /// </summary>
    /// <param name="isRunningWithDebugger">Whether the client reported running with a debugger.</param>
    /// <returns>True if the connection is exempt, false if not.</returns>
    public bool ExemptsDebugger(bool isRunningWithDebugger) => AllowDebuggerExemption && isRunningWithDebugger;
}
