// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Protects and verifies kernel event provenance.
/// </summary>
public interface IKernelEventProof
{
    /// <summary>
    /// Signs an event and its predecessor.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="source">The event source.</param>
    /// <param name="predecessor">The preceding family slot.</param>
    /// <param name="body">The canonical event body.</param>
    /// <returns>The protected proof.</returns>
    string Sign(EventType type, EventSourceId source, EventSequenceNumber predecessor, string body);

    /// <summary>
    /// Verifies an event's kernel proof.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="source">The event source.</param>
    /// <param name="predecessor">The preceding family slot.</param>
    /// <param name="body">The canonical event body.</param>
    /// <param name="proof">The protected proof.</param>
    /// <returns>Whether the proof matches.</returns>
    bool Verify(EventType type, EventSourceId source, EventSequenceNumber predecessor, string body, string? proof);
}
