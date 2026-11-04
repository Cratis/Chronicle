// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
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
        _entries.TryAdd((entry.Projection, entry.Digest), Snapshot(entry));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinitionHistoryEntry>> GetFor(ProjectionId projectionId) =>
        Task.FromResult<IEnumerable<ProjectionDefinitionHistoryEntry>>(_entries.Values
            .Where(_ => _.Projection == projectionId)
            .OrderBy(_ => _.RecordedAt)
            .ThenBy(_ => _.Digest.Value, StringComparer.Ordinal)
            .Select(Snapshot)
            .ToArray());

    static ProjectionDefinitionHistoryEntry Snapshot(ProjectionDefinitionHistoryEntry entry) =>
        entry with { Definition = Snapshot(entry.Definition) };

    static ProjectionDefinition Snapshot(ProjectionDefinition definition)
    {
        // Children also expose their every-event rules as All. Detach both surfaces.
        var snapshot = definition is ChildrenDefinition child
            ? child with { All = Snapshot(child.All) }
            : definition;
        return snapshot with
        {
            InitialModelState = (System.Text.Json.Nodes.JsonObject)definition.InitialModelState.DeepClone(),
            From = definition.From.ToDictionary(pair => pair.Key, pair => Snapshot(pair.Value)),
            Join = definition.Join.ToDictionary(pair => pair.Key, pair => pair.Value with { Properties = pair.Value.Properties.ToDictionary() }),
            Children = definition.Children.ToDictionary(pair => pair.Key, pair => (ChildrenDefinition)Snapshot(pair.Value)),
            Nested = definition.Nested?.ToDictionary(pair => pair.Key, pair => (ChildrenDefinition)Snapshot(pair.Value)),
            FromDerivatives = definition.FromDerivatives.Select(_ => _ with { EventTypes = _.EventTypes.ToArray(), From = Snapshot(_.From) }).ToArray(),
            FromEvery = Snapshot(definition.FromEvery),
            RemovedWith = definition.RemovedWith.ToDictionary(),
            RemovedWithJoin = definition.RemovedWithJoin.ToDictionary(),
            Tags = definition.Tags?.ToArray(),
            NoAutoMapProperties = definition.NoAutoMapProperties?.ToArray()
        };
    }

    static FromDefinition Snapshot(FromDefinition definition) =>
        definition with { Properties = definition.Properties.ToDictionary() };

    static FromEveryDefinition Snapshot(FromEveryDefinition definition) =>
        definition with { Properties = definition.Properties.ToDictionary() };
}
