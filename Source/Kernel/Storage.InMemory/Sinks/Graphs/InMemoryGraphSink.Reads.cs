// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

public partial class InMemoryGraphSink
{
    /// <inheritdoc/>
    public Task<GraphNode?> FindNode(ProjectionId owner, NodeReference node)
    {
        lock (_gate)
        {
            var element = LiveNode(InMemoryGraphNodeId.From(node), owner);
            return Task.FromResult(element is null ? null : ToNode(node, element));
        }
    }

    /// <inheritdoc/>
    public Task<GraphEdge?> FindEdge(ProjectionId owner, EdgeReference edge)
    {
        lock (_gate)
        {
            var element = LiveEdge(InMemoryGraphEdgeId.From(edge), owner);
            return Task.FromResult(element is null ? null : ToEdge(edge, element));
        }
    }

    /// <inheritdoc/>
    public Task<GraphElements<GraphNode>> GetNodes(string label, int skip = 0, int take = 50)
    {
        lock (_gate)
        {
            var matching = _nodes.Where(_ => _.Key.Label == label).OrderBy(_ => _.Key.Key, StringComparer.Ordinal).ToArray();
            var page = matching.Skip(skip).Take(take).Select(_ => ToNode(ReferenceFor(_.Key), _.Value)).ToArray();
            return Task.FromResult(new GraphElements<GraphNode>(page, matching.Length));
        }
    }

    /// <inheritdoc/>
    public Task<GraphElements<GraphEdge>> GetEdges(string type, int skip = 0, int take = 50)
    {
        lock (_gate)
        {
            var matching = _edges.Where(_ => _.Key.Type == type)
                .OrderBy(_ => _.Key.Source.Key, StringComparer.Ordinal)
                .ThenBy(_ => _.Key.Target.Key, StringComparer.Ordinal)
                .ThenBy(_ => _.Key.Identity, StringComparer.Ordinal)
                .ToArray();
            var page = matching.Skip(skip).Take(take).Select(_ => ToEdge(ReferenceFor(_.Key), _.Value)).ToArray();
            return Task.FromResult(new GraphElements<GraphEdge>(page, matching.Length));
        }
    }

    static NodeReference ReferenceFor(InMemoryGraphNodeId id) => new(id.Label, id.Key);

    static EdgeReference ReferenceFor(InMemoryGraphEdgeId id) =>
        new(id.Type, ReferenceFor(id.Source), ReferenceFor(id.Target), id.Identity);

    static GraphNode ToNode(NodeReference reference, InMemoryGraphElement element) =>
        new(reference, new Dictionary<string, object?>(element.Properties), ToWatermark(element));

    static GraphEdge ToEdge(EdgeReference reference, InMemoryGraphElement element) =>
        new(reference, new Dictionary<string, object?>(element.Properties), ToWatermark(element));

    static EventSequenceNumber ToWatermark(InMemoryGraphElement element) => element.Watermark is { } watermark ? watermark : EventSequenceNumber.Unavailable;
}
