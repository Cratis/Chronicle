// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// Defines what a silo knows about how busy it is, without asking a grain.
/// </summary>
/// <remarks>
/// Everything here is read from the silo's own memory, so it answers while the grains are saturated; that is the
/// point of it. The grain call filters feed it, and the readiness check, the keep-alive and the diagnostics read it.
/// </remarks>
public interface IKernelLoad
{
    /// <summary>
    /// Gets the status of the silo. It is evaluated at most once a second.
    /// </summary>
    KernelLoadStatus Status { get; }

    /// <summary>
    /// Gets why the silo is degraded: one entry per limit crossed. Empty when it is not.
    /// </summary>
    IReadOnlyList<string> Reasons { get; }

    /// <summary>
    /// Builds a report of what the silo is busy with.
    /// </summary>
    /// <param name="busiest">How many grain types to include, busiest first.</param>
    /// <returns>The <see cref="KernelLoadReport"/>.</returns>
    KernelLoadReport Report(int busiest = 20);

    /// <summary>
    /// Records that this silo made a call to a grain.
    /// </summary>
    /// <param name="grainType">The name of the grain interface.</param>
    /// <param name="eventStore">The event store the grain belongs to, or an empty string.</param>
    /// <returns>The <see cref="PendingCall"/> to pass to <see cref="EndPending"/> when the call is answered.</returns>
    PendingCall BeginPending(string grainType, string eventStore);

    /// <summary>
    /// Records that a call made by <see cref="BeginPending"/> was answered, or failed.
    /// </summary>
    /// <param name="call">The <see cref="PendingCall"/>.</param>
    void EndPending(PendingCall call);

    /// <summary>
    /// Records that a call to a grain on this silo started running.
    /// </summary>
    /// <param name="grainType">The name of the grain interface.</param>
    /// <param name="eventStore">The event store the grain belongs to, or an empty string.</param>
    void BeginRunning(string grainType, string eventStore);

    /// <summary>
    /// Records that a call to a grain on this silo stopped running.
    /// </summary>
    /// <param name="grainType">The name of the grain interface.</param>
    /// <param name="eventStore">The event store the grain belongs to, or an empty string.</param>
    void EndRunning(string grainType, string eventStore);

    /// <summary>
    /// Records that a gRPC streaming call started.
    /// </summary>
    void StreamOpened();

    /// <summary>
    /// Records that a gRPC streaming call ended.
    /// </summary>
    void StreamClosed();
}
