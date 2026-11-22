// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the limits that decide when a silo reports itself as overloaded.
/// </summary>
/// <remarks>
/// The limits are evaluated per grain type (and per event store where the grain's key carries one) from the grain calls
/// this silo has in flight. A silo that crosses one reports <c language="csharp">degraded</c> on its readiness endpoint
/// and on the keep-alive of every client connection, and clears it once it has stayed under all of them for
/// <see cref="RecoverySeconds"/>. A degraded silo stays in service: the report is for people and clients, not a
/// reason to take the silo out of rotation.
/// </remarks>
public class Overload
{
    /// <summary>
    /// Gets whether overload is evaluated at all. When false the silo always reports itself as accepting; the metrics
    /// are still produced.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets how long, in seconds, a grain call may have been waiting for its answer before the silo reports itself as
    /// overloaded. Defaults to 15, half of the default grain response timeout.
    /// </summary>
    public int MaxOldestPendingCallAgeSeconds { get; init; } = 15;

    /// <summary>
    /// Gets how many calls to one grain type may be waiting for their answer, from this silo, before the silo reports
    /// itself as overloaded. Defaults to 2000.
    /// </summary>
    public int MaxPendingCalls { get; init; } = 2000;

    /// <summary>
    /// Gets how many calls to grains of one type may be running on this silo at once, interleaved or not, before the
    /// silo reports itself as overloaded. Defaults to 2000.
    /// </summary>
    public int MaxRunningCalls { get; init; } = 2000;

    /// <summary>
    /// Gets how long, in seconds, the silo has to stay under every limit before it stops reporting itself as
    /// recovering. Defaults to 30.
    /// </summary>
    public int RecoverySeconds { get; init; } = 30;
}
