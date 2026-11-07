// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

/// <summary>
/// Represents an in-memory implementation of <see cref="IGraphSink"/>: the executable reference for the semantics every
/// graph sink must have.
/// </summary>
/// <remarks>
/// Every <see cref="GraphChangeset"/> is applied atomically: when one operation fails, the operations before it are undone.
/// </remarks>
/// <param name="definition">The <see cref="GraphDefinition"/> of the graph.</param>
public partial class InMemoryGraphSink(GraphDefinition definition) : IGraphSink
{
    readonly object _gate = new();
    readonly Dictionary<InMemoryGraphNodeId, InMemoryGraphElement> _nodes = [];
    readonly Dictionary<InMemoryGraphEdgeId, InMemoryGraphElement> _edges = [];
    readonly Dictionary<ProjectionId, long> _currentGenerations = [];
    readonly Dictionary<ProjectionId, long> _replayGenerations = [];

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemoryGraph;

    /// <inheritdoc/>
    public bool SupportsMultipleEdges => true;

    /// <summary>
    /// Gets a value indicating whether the sink is in bulk mode. The in-memory sink never buffers, so this only records the mode.
    /// </summary>
    public bool IsInBulk { get; private set; }

    /// <inheritdoc/>
    public Task EnsureSchema()
    {
        foreach (var edge in definition.Edges)
        {
            definition.GetNode(edge.SourceLabel);
            definition.GetNode(edge.TargetLabel);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyGraphChanges(GraphChangeset changeset, EventSequenceNumber eventSequenceNumber)
    {
        lock (_gate)
        {
            var journal = new List<Action>();
            try
            {
                foreach (var operation in changeset.Operations)
                {
                    Apply(operation, changeset.Owner, eventSequenceNumber.Value, journal);
                }
            }
            catch
            {
                for (var i = journal.Count - 1; i >= 0; i--) journal[i]();
                throw;
            }
        }

        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task BeginBulk()
    {
        IsInBulk = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk()
    {
        IsInBulk = false;
        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }
}
