// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// A single connect attempt that every concurrent caller of <see cref="ChronicleConnection"/> joins instead of starting their own.
/// </summary>
internal sealed class ConnectAttempt
{
    /// <summary>
    /// Gets the signal that completes when the transport is established, immediately before the connected handlers run.
    /// A joiner measures its first deadline against this, so the time the handlers take is not charged to the transport.
    /// </summary>
    public TaskCompletionSource TransportEstablished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the completion of the attempt, created before the attempt is published so a joiner can never observe it unset.
    /// It is completed, faulted or cancelled only after the attempt has stopped being the one in flight.
    /// </summary>
    public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
