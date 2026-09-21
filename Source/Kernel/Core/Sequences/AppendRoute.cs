// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Resolves the routing metadata of a new append at the kernel API boundary.
/// </summary>
/// <param name="SourceType">The event source type.</param>
/// <param name="StreamType">The event stream type.</param>
/// <param name="StreamId">The event stream identifier.</param>
/// <remarks>
/// This policy applies only to new append requests. Query filters and forwarding of existing events retain
/// their own semantics; neither should reinterpret recorded metadata as a request for a default.
/// </remarks>
internal record AppendRoute(EventSourceType SourceType, EventStreamType StreamType, EventStreamId StreamId)
{
    /// <summary>
    /// Resolves omitted or empty routing dimensions, preserving every explicit nonempty value.
    /// </summary>
    /// <param name="sourceType">The requested event source type.</param>
    /// <param name="streamType">The requested event stream type.</param>
    /// <param name="streamId">The requested event stream identifier.</param>
    /// <returns>The route to use for validation and persistence.</returns>
    internal static AppendRoute Resolve(string? sourceType, string? streamType, string? streamId) => new(
        string.IsNullOrEmpty(sourceType) ? EventSourceType.Default : (EventSourceType)sourceType,
        string.IsNullOrEmpty(streamType) ? EventStreamType.All : (EventStreamType)streamType,
        string.IsNullOrEmpty(streamId) ? (EventStreamId)EventStreamId.Default : (EventStreamId)streamId);
}
