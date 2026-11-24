// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Storage.InMemory.Projections;

/// <summary>
/// Represents append-only, event-store-scoped projection definition history in memory.
/// </summary>
public class ProjectionDefinitionHistoryStorage : IProjectionDefinitionHistoryStorage
{
    readonly ConcurrentDictionary<(ProjectionId Projection, ProjectionDefinitionDigest Digest), ProjectionDefinitionHistoryEntry> _entries = new();

    /// <inheritdoc/>
    public Task Record(ProjectionDefinitionHistoryEntry entry)
    {
        _entries.TryAdd((entry.Projection, entry.Digest), entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinitionHistoryEntry>> GetFor(ProjectionId projectionId) =>
        Task.FromResult<IEnumerable<ProjectionDefinitionHistoryEntry>>(_entries.Values
            .Where(_ => _.Projection == projectionId)
            .OrderBy(_ => _.RecordedAt)
            .ThenBy(_ => _.Digest.Value, StringComparer.Ordinal)
            .ToArray());
}
