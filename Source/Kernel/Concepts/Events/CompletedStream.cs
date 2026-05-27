// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the identity of a completed event stream — the combination of a <see cref="EventStreamType"/> and an <see cref="EventStreamId"/>.
/// </summary>
/// <param name="EventStreamType">The <see cref="EventStreamType"/> the stream belongs to.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/> identifying the stream within the type.</param>
/// <remarks>
/// Once a stream has been completed no further events may be appended to it. Completion targets the unique
/// pair of stream type and stream identifier. The default stream type (<see cref="EventStreamType.All"/>) combined
/// with the default stream identifier (<see cref="Cratis.Chronicle.Concepts.Events.EventStreamId.Default"/>) cannot be completed — see <see cref="IsDefault"/>.
/// </remarks>
public record CompletedStream(EventStreamType EventStreamType, EventStreamId EventStreamId)
{
    /// <summary>
    /// Gets a value indicating whether this represents the default stream that must never be completed.
    /// </summary>
    /// <remarks>
    /// The default stream is the pair of <see cref="EventStreamType.All"/> and the default <see cref="Cratis.Chronicle.Concepts.Events.EventStreamId"/>
    /// value (<see cref="Cratis.Chronicle.Concepts.Events.EventStreamId.Default"/>). Appends without an explicit stream go here.
    /// </remarks>
    public bool IsDefault =>
        EventStreamType.Value == EventStreamType.All.Value &&
        EventStreamId.Value == Cratis.Chronicle.Concepts.Events.EventStreamId.Default;
}
