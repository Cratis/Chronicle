// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the acknowledgment of one chunk added to an import.
/// </summary>
/// <param name="ChunkIndex">The index of the chunk.</param>
/// <param name="AlreadyStaged">True when the chunk had been staged before; the earlier result is returned.</param>
/// <param name="EventsReceived">How many events the chunk carried.</param>
/// <param name="EventsStaged">How many of them were staged.</param>
/// <param name="Rejections">The events that were rejected while staging, by their index in the import.</param>
/// <param name="Error">Why the chunk was refused as a whole, when it was; nothing of it was staged then.</param>
public record ImportChunkResult(
    int ChunkIndex,
    bool AlreadyStaged,
    int EventsReceived,
    int EventsStaged,
    IEnumerable<ImportEventRejection> Rejections,
    string? Error = default);
