// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Json;
using Cratis.Serialization;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Runs the graph projections of an application against the in-memory graph sink, so a specification can append events
/// and assert the nodes and edges they leave behind.
/// </summary>
/// <remarks>
/// The scenario discovers the node and edge types and the <see cref="IGraphProjection"/> implementations the same way an
/// application does, builds their definitions, and applies the operations the kernel's graph projection engine produces to
/// the same in-memory graph sink the kernel uses. Events get increasing sequence numbers in the order they are appended.
/// </remarks>
public sealed class GraphScenario : IDisposable
{
    readonly EventStoreForTesting _eventStore;
    readonly JsonSerializerOptions _serializerOptions = new(Globals.JsonSerializerOptions);
    readonly List<RunningGraph> _graphs = [];
    ulong _nextSequenceNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphScenario"/> class using the default artifacts.
    /// </summary>
    public GraphScenario()
        : this(Defaults.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphScenario"/> class.
    /// </summary>
    /// <param name="defaults">The <see cref="Defaults"/> that provide the artifacts to discover graphs from.</param>
    public GraphScenario(Defaults defaults)
    {
        _eventStore = (EventStoreForTesting)defaults.EventStore;
        foreach (var registration in (_eventStore.Projections as Projections.Projections)?.Graphs?.Registrations ?? [])
        {
            _graphs.Add(RunningGraph.Create(registration));
        }
    }

    /// <summary>
    /// Append events for an event source. The events are projected onto the graphs immediately.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the events belong to.</param>
    /// <param name="events">The events to append, in order.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Append(EventSourceId eventSourceId, params object[] events)
    {
        foreach (var @event in events)
        {
            var (prepared, _) = await ReadModelEvents.Prepare(_eventStore, [(eventSourceId, @event)], persist: false);
            var appended = prepared[0] with
            {
                Context = prepared[0].Context with
                {
                    SequenceNumber = _nextSequenceNumber,
                    Occurred = KernelEvents::EventContext.Empty.Occurred.AddTicks((long)_nextSequenceNumber)
                }
            };

            foreach (var graph in _graphs)
            {
                await graph.Project(appended);
            }

            _nextSequenceNumber++;
        }
    }

    /// <summary>
    /// Get a node.
    /// </summary>
    /// <typeparam name="TNode">The type of the node. Its name is the node label.</typeparam>
    /// <param name="key">The key of the node.</param>
    /// <returns>The node, or null if there is no node with the key.</returns>
    public async Task<TNode?> Node<TNode>(object key)
        where TNode : class
    {
        var graph = GraphWithNode(typeof(TNode).Name);
        var node = await graph.Sink.FindNode(graph.Owner, new NodeReference(typeof(TNode).Name, key));
        return node is null ? null : Deserialize<TNode>(node.Properties, []);
    }

    /// <summary>
    /// Get all nodes of a type.
    /// </summary>
    /// <typeparam name="TNode">The type of the node. Its name is the node label.</typeparam>
    /// <returns>The nodes.</returns>
    public async Task<IReadOnlyList<TNode>> Nodes<TNode>()
        where TNode : class
    {
        var graph = GraphWithNode(typeof(TNode).Name);
        var nodes = await graph.Sink.GetNodes(typeof(TNode).Name, 0, int.MaxValue);
        return [.. nodes.Elements.Select(node => Deserialize<TNode>(node.Properties, []))];
    }

    /// <summary>
    /// Get an edge.
    /// </summary>
    /// <typeparam name="TEdge">The type of the edge.</typeparam>
    /// <param name="sourceKey">The key of the node the edge starts at.</param>
    /// <param name="targetKey">The key of the node the edge ends at.</param>
    /// <param name="identity">The value of the identifying property, for an edge type that has one.</param>
    /// <returns>The edge, or null if there is no such edge.</returns>
    public async Task<TEdge?> Edge<TEdge>(object sourceKey, object targetKey, object? identity = null)
        where TEdge : class
    {
        var type = EdgeTypeOf(typeof(TEdge));
        var graph = GraphWithEdge(type);
        var definition = graph.Definition.GetEdge(type);
        var edge = await graph.Sink.FindEdge(graph.Owner, new EdgeReference(type, new(definition.SourceLabel, sourceKey), new(definition.TargetLabel, targetKey), identity));
        return edge is null ? null : Deserialize<TEdge>(edge.Properties, Endpoints(typeof(TEdge), edge.Reference));
    }

    /// <summary>
    /// Get all edges of a type.
    /// </summary>
    /// <typeparam name="TEdge">The type of the edge.</typeparam>
    /// <returns>The edges.</returns>
    public async Task<IReadOnlyList<TEdge>> Edges<TEdge>()
        where TEdge : class
    {
        var type = EdgeTypeOf(typeof(TEdge));
        var graph = GraphWithEdge(type);
        var edges = await graph.Sink.GetEdges(type, 0, int.MaxValue);
        return [.. edges.Elements.Select(edge => Deserialize<TEdge>(edge.Properties, Endpoints(typeof(TEdge), edge.Reference)))];
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    static string EdgeTypeOf(Type type) => ((EdgeAttribute?)type.GetCustomAttributes(typeof(EdgeAttribute), false).FirstOrDefault())?.Type ?? type.Name;

    IEnumerable<(string Name, object Value)> Endpoints(Type edgeType, EdgeReference edge)
    {
        var namingPolicy = new CamelCaseNamingPolicy();
        foreach (var member in GraphMember.Of(edgeType))
        {
            if (member.Has<IGraphSourceAttribute>()) yield return (namingPolicy.GetPropertyName(member.Name), edge.Source.Key);
            if (member.Has<IGraphTargetAttribute>()) yield return (namingPolicy.GetPropertyName(member.Name), edge.Target.Key);
        }
    }

    TElement Deserialize<TElement>(IReadOnlyDictionary<string, object?> properties, IEnumerable<(string Name, object Value)> extra)
    {
        var json = new JsonObject();
        foreach (var (name, value) in properties) json[name] = JsonSerializer.SerializeToNode(value, _serializerOptions);
        foreach (var (name, value) in extra) json[name] = JsonSerializer.SerializeToNode(value, _serializerOptions);
        return json.Deserialize<TElement>(_serializerOptions)!;
    }

    RunningGraph GraphWithNode(string label) =>
        _graphs.FirstOrDefault(_ => _.Definition.Nodes.Any(node => node.Label == label)) ?? throw new NoGraphDeclaresElement(label);

    RunningGraph GraphWithEdge(string type) =>
        _graphs.FirstOrDefault(_ => _.Definition.Edges.Any(edge => edge.Type == type)) ?? throw new NoGraphDeclaresElement(type);
}
