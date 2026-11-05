// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Builds the <see cref="GraphChangeset"/> an event causes, from the mappings of a <see cref="GraphProjectionPlan"/>.
/// </summary>
/// <remarks>
/// Counters are resolved here: the current value of the node or edge is read from the sink and the operation carries the
/// absolute result, which is what makes the operations idempotent under the watermark.
/// </remarks>
/// <param name="plan">The <see cref="GraphProjectionPlan"/> to build from.</param>
/// <param name="sink">The <see cref="IGraphSink"/> to read current values from.</param>
/// <param name="owner">The <see cref="ProjectionId"/> of the projection the changesets are built for.</param>
public class GraphChangesetBuilder(GraphProjectionPlan plan, IGraphSink sink, ProjectionId owner)
{
    /// <summary>
    /// Build the changeset for an event.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/>.</param>
    /// <returns>The <see cref="GraphChangeset"/>, empty when the projection does nothing for the event.</returns>
    public async Task<GraphChangeset> Build(AppendedEvent @event)
    {
        var operations = new List<GraphOperation>();
        var mappings = plan.GetMappingsFor(@event.Context.EventType);
        if (mappings is null) return new(owner, operations);

        foreach (var node in mappings.Nodes) await AddNode(node, @event, operations);
        foreach (var edge in mappings.Edges) await AddEdge(edge, @event, operations);
        foreach (var removal in mappings.EdgeRemovals) AddEdgeRemoval(removal, @event, operations);
        foreach (var removal in mappings.NodeRemovals) AddNodeRemoval(removal, @event, operations);
        return new(owner, operations);
    }

    static object? ResolveKey(ValueProvider<AppendedEvent>? provider, AppendedEvent @event)
    {
        var value = GraphValues.Normalize(provider?.Invoke(@event));
        return value is null or string { Length: 0 } ? null : value;
    }

    static Dictionary<string, object?> ResolveProperties(
        IReadOnlyList<string> names,
        IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> mappers,
        IReadOnlyDictionary<string, object?>? current,
        AppendedEvent @event)
    {
        var target = new ExpandoObject();
        var values = (IDictionary<string, object?>)target;
        foreach (var (name, value) in current ?? new Dictionary<string, object?>()) values[name] = value;
        foreach (var mapper in mappers) mapper(@event, target, ArrayIndexers.NoIndexers);

        return names.ToDictionary(name => name, name => GraphValues.Normalize(values.TryGetValue(name, out var value) ? value : null));
    }

    async Task AddNode(GraphNodeMapping mapping, AppendedEvent @event, List<GraphOperation> operations)
    {
        if (ResolveKey(mapping.Key, @event) is not { } key) return;

        var node = new NodeReference(mapping.Label, key);
        var current = mapping.ReadsCurrentState ? (await sink.FindNode(owner, node))?.Properties : null;
        var properties = ResolveProperties(mapping.PropertyNames, mapping.Properties, current, @event);
        if (mapping.UpdateOnly)
        {
            operations.Add(new SetProperties(node, properties));
            return;
        }

        properties[mapping.KeyProperty] = key;
        operations.Add(new UpsertNode(node, properties));
    }

    async Task AddEdge(GraphEdgeMapping mapping, AppendedEvent @event, List<GraphOperation> operations)
    {
        if (Edge(mapping.Type, mapping.SourceLabel, mapping.TargetLabel, mapping.SourceKey, mapping.TargetKey, mapping.Identity, @event) is not { } edge) return;

        var current = mapping.ReadsCurrentState ? (await sink.FindEdge(owner, edge))?.Properties : null;
        var properties = ResolveProperties(mapping.PropertyNames, mapping.Properties, current, @event);
        if (mapping.IdentifyingProperty is not null) properties[mapping.IdentifyingProperty] = edge.Identity;
        operations.Add(new UpsertEdge(edge, properties));
    }

    void AddEdgeRemoval(GraphEdgeRemoval removal, AppendedEvent @event, List<GraphOperation> operations)
    {
        if (Edge(removal.Type, removal.SourceLabel, removal.TargetLabel, removal.SourceKey, removal.TargetKey, removal.Identity, @event) is { } edge)
        {
            operations.Add(new DeleteEdge(edge));
        }
    }

    void AddNodeRemoval(GraphNodeRemoval removal, AppendedEvent @event, List<GraphOperation> operations)
    {
        if (ResolveKey(removal.Key, @event) is { } key)
        {
            operations.Add(new DeleteNode(new(removal.Label, key), removal.Detach));
        }
    }

    EdgeReference? Edge(
        string type,
        string sourceLabel,
        string targetLabel,
        ValueProvider<AppendedEvent> sourceKey,
        ValueProvider<AppendedEvent> targetKey,
        ValueProvider<AppendedEvent>? identity,
        AppendedEvent @event)
    {
        if (ResolveKey(sourceKey, @event) is not { } source || ResolveKey(targetKey, @event) is not { } target) return null;

        var identityValue = identity is null ? null : ResolveKey(identity, @event);
        return identity is not null && identityValue is null ? null : new(type, new(sourceLabel, source), new(targetLabel, target), identityValue);
    }
}
