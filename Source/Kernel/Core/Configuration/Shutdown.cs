// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Configuration for draining local observer work at shutdown.
/// </summary>
public class Shutdown
{
    /// <summary>
    /// Gets the shared drain budget, capped at the host shutdown timeout minus the host shutdown reserve.
    /// </summary>
    public TimeSpan DrainTimeout { get; init; } = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Gets the time reserved after the observer drain for Orleans silo stop, reminders, and hosted services.
    /// </summary>
    public TimeSpan HostShutdownReserve { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the total host shutdown timeout, including the drain and the host shutdown reserve.
    /// </summary>
    public TimeSpan HostShutdownTimeout => DrainTimeout + HostShutdownReserve;
}
