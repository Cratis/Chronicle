// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

public partial class InMemoryGraphSink
{
    /// <inheritdoc/>
    public Task BeginReplay(ProjectionId owner, ReplayContext context)
    {
        lock (_gate)
        {
            _replayGenerations[owner] = Math.Max(context.Started.UtcTicks, _currentGenerations.GetValueOrDefault(owner) + 1);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ProjectionId owner, ReplayContext context)
    {
        lock (_gate)
        {
            if (!_replayGenerations.ContainsKey(owner)) return BeginReplay(owner, context);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ProjectionId owner, ReplayContext context)
    {
        lock (_gate)
        {
            if (_replayGenerations.TryGetValue(owner, out var generation))
            {
                Sweep(owner, generation);
                _currentGenerations[owner] = generation;
                _replayGenerations.Remove(owner);
            }
        }

        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task LeaveReplay(ProjectionId owner)
    {
        lock (_gate)
        {
            _replayGenerations.Remove(owner);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun(ProjectionId owner)
    {
        lock (_gate)
        {
            foreach (var id in _edges.Where(_ => _.Value.Owner == owner).Select(_ => _.Key).ToArray()) _edges.Remove(id);
            foreach (var id in _nodes.Where(_ => _.Value.Owner == owner).Select(_ => _.Key).ToArray())
            {
                foreach (var edge in _edges.Keys.Where(edge => edge.Source == id || edge.Target == id).ToArray()) _edges.Remove(edge);
                _nodes.Remove(id);
            }
        }

        return Task.CompletedTask;
    }

    void Sweep(ProjectionId owner, long generation)
    {
        foreach (var id in _edges.Where(_ => _.Value.Owner == owner && _.Value.Generation < generation).Select(_ => _.Key).ToArray())
        {
            _edges.Remove(id);
        }

        foreach (var id in _nodes.Where(_ => _.Value.Owner == owner && _.Value.Generation < generation).Select(_ => _.Key).ToArray())
        {
            if (_edges.Keys.Any(edge => edge.Source == id || edge.Target == id))
            {
                // Edges of other projections still point here, so the node stays as the bare node an edge would have created.
                _nodes[id] = new InMemoryGraphElement();
            }
            else
            {
                _nodes.Remove(id);
            }
        }
    }
}
