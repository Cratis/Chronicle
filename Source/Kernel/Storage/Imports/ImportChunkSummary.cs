// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents the counts of a staged chunk, without its events.
/// </summary>
/// <param name="ChunkIndex">The index of the chunk.</param>
/// <param name="FirstEventIndex">The index in the import of the first event in the chunk.</param>
/// <param name="EventsReceived">How many events the chunk carried.</param>
/// <param name="EventsStaged">How many of them were staged.</param>
/// <param name="EventsRejected">How many of them were rejected while staging.</param>
/// <param name="EventTypes">The distinct identifiers of the event types staged in the chunk.</param>
public record ImportChunkSummary(int ChunkIndex, long FirstEventIndex, int EventsReceived, int EventsStaged, int EventsRejected, IEnumerable<string> EventTypes)
{
    /// <summary>
    /// Tells whether a chunk sent again under the same index is the one that was staged.
    /// </summary>
    /// <param name="firstEventIndex">The index in the import of the first event of the resent chunk.</param>
    /// <param name="eventsReceived">How many events the resent chunk carries.</param>
    /// <returns>True when the resent chunk covers the same events.</returns>
    public bool Matches(long firstEventIndex, int eventsReceived) => FirstEventIndex == firstEventIndex && EventsReceived == eventsReceived;
}
