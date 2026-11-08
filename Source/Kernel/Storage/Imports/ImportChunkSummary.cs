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
public record ImportChunkSummary(int ChunkIndex, long FirstEventIndex, int EventsReceived, int EventsStaged, int EventsRejected);
