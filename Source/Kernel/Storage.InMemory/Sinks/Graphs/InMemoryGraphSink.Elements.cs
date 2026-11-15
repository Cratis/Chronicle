// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

public partial class InMemoryGraphSink
{
    static bool IsAtOrBelowWatermark(InMemoryGraphElement element, ulong sequence) => element.Watermark is { } watermark && sequence <= watermark;

    bool IsStale(InMemoryGraphElement element, ProjectionId reader) =>
        element.Owner is { } owner && owner == reader && _replayGenerations.TryGetValue(reader, out var replay) && element.Generation < replay;

    long GenerationFor(ProjectionId owner) =>
        _replayGenerations.TryGetValue(owner, out var replay) ? replay : _currentGenerations.GetValueOrDefault(owner);

    InMemoryGraphElement? LiveNode(InMemoryGraphNodeId id, ProjectionId reader) =>
        _nodes.TryGetValue(id, out var element) && !IsStale(element, reader) ? element : null;

    InMemoryGraphElement? LiveEdge(InMemoryGraphEdgeId id, ProjectionId reader) =>
        _edges.TryGetValue(id, out var element) && !IsStale(element, reader) ? element : null;

    InMemoryGraphElement CreateNode(InMemoryGraphNodeId id, List<Action> journal)
    {
        // A stale element of the replaying owner is replaced: the first write in a replay starts the element over.
        var previous = _nodes.GetValueOrDefault(id)?.Snapshot();
        var element = _nodes[id] = new InMemoryGraphElement();
        journal.Add(() =>
        {
            if (previous is null) _nodes.Remove(id);
            else _nodes[id] = previous;
        });
        return element;
    }

    InMemoryGraphElement CreateEdge(InMemoryGraphEdgeId id, List<Action> journal)
    {
        var previous = _edges.GetValueOrDefault(id)?.Snapshot();
        var element = _edges[id] = new InMemoryGraphElement();
        journal.Add(() =>
        {
            if (previous is null) _edges.Remove(id);
            else _edges[id] = previous;
        });
        return element;
    }

    void EnsureEndpoint(NodeReference node, ProjectionId owner, List<Action> journal)
    {
        definition.GetNode(node.Label);
        var id = InMemoryGraphNodeId.From(node);
        if (!_nodes.TryGetValue(id, out var existing))
        {
            CreateNode(id, journal);
        }
        else if (IsStale(existing, owner))
        {
            // The edge needs the node, so it must outlive the sweep. It becomes a bare node that the node mapping can claim again.
            CreateNode(id, journal);
        }
    }

    void Write(InMemoryGraphElement element, IReadOnlyDictionary<string, object?> properties, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        var before = element.Snapshot();
        journal.Add(() => element.CopyFrom(before));
        foreach (var (name, value) in properties)
        {
            var normalized = GraphValues.Normalize(value);
            if (normalized is null) element.Properties.Remove(name);
            else element.Properties[name] = normalized;
        }

        element.Watermark = sequence;
        if (element.Owner is null || element.Owner == owner)
        {
            element.Owner = owner;
            element.Generation = GenerationFor(owner);
        }
    }

    void RemoveNode(InMemoryGraphNodeId id, List<Action> journal)
    {
        var removed = _nodes[id];
        _nodes.Remove(id);
        journal.Add(() => _nodes[id] = removed);
    }

    void RemoveEdge(InMemoryGraphEdgeId id, List<Action> journal)
    {
        var removed = _edges[id];
        _edges.Remove(id);
        journal.Add(() => _edges[id] = removed);
    }
}
