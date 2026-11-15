// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Defines the stateless worker that stages chunks of imports, outside the event sequence grain, in parallel.
/// </summary>
public interface IImportStager : IGrainWithIntegerKey
{
    /// <summary>
    /// Stages one chunk: every event is checked for what does not depend on the order of the log, and the chunk is
    /// stored once under its import and index. Staging a chunk again returns the first result.
    /// </summary>
    /// <param name="key">The <see cref="ImportKey"/>.</param>
    /// <param name="chunkIndex">The index of the chunk.</param>
    /// <param name="firstEventIndex">The index in the import of the first event in the chunk.</param>
    /// <param name="events">The events of the chunk.</param>
    /// <param name="causedBy">The identity recorded as having caused each event, aligned with the events.</param>
    /// <returns>The <see cref="ImportChunkStagingResult"/>.</returns>
    Task<ImportChunkStagingResult> Stage(ImportKey key, int chunkIndex, long firstEventIndex, IReadOnlyList<ImportEvent> events, IReadOnlyList<Identity> causedBy);
}
