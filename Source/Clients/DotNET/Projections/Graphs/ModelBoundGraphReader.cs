// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Reads nodes and edges from the model-bound attributes on their types.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal class ModelBoundGraphReader(INamingPolicy namingPolicy)
{
    readonly GraphMemberMappings _mappings = new(namingPolicy);

    /// <summary>
    /// Gets the name of the graph a node or edge type belongs to.
    /// </summary>
    /// <param name="type">The node or edge type.</param>
    /// <returns>The graph name.</returns>
    public static string GraphNameOf(Type type) =>
        type.GetCustomAttributes(typeof(GraphAttribute), false).OfType<GraphAttribute>().FirstOrDefault()?.Name ??
        type.Assembly.GetCustomAttributes(typeof(GraphAttribute), false).OfType<GraphAttribute>().FirstOrDefault()?.Name ??
        type.Assembly.GetName().Name ??
        type.Name;

    /// <summary>
    /// Read a node type.
    /// </summary>
    /// <param name="type">The type marked as a node.</param>
    /// <returns>The <see cref="GraphNodeModel"/>.</returns>
    /// <exception cref="NodeMustHaveExactlyOneKey">Thrown when the node does not have exactly one key.</exception>
    public GraphNodeModel ReadNode(Type type)
    {
        var members = GraphMember.Of(type);
        var keys = members.Where(_ => _.Has<KeyAttribute>()).ToArray();
        if (keys.Length != 1) throw new NodeMustHaveExactlyOneKey(type, keys.Length);

        var byEvent = _mappings.Read(members);
        var from = type.GetCustomAttributes(false).OfType<IFromEventAttribute>().Select(attribute => new GraphNodeFromModel(
            attribute.EventType,
            KeyOf(attribute),
            byEvent.GetValueOrDefault(attribute.EventType) ?? [])).ToList();

        // An event that only a property refers to still projects the node, keyed by the event source id.
        from.AddRange(byEvent.Where(_ => from.TrueForAll(declared => declared.EventType != _.Key)).Select(_ => new GraphNodeFromModel(_.Key, null, _.Value)));
        var removedWith = type.GetCustomAttributes(false).OfType<IRemovedWithAttribute>().Select(attribute => new GraphNodeRemovedWithModel(
            attribute.EventType,
            KeyOf(attribute),
            attribute is IDetachingRemovedWithAttribute { Detach: true })).ToArray();

        return new(type, type.Name, ProjectionPropertyPaths.NameOf(keys[0].Name, namingPolicy), from, removedWith);
    }

    /// <summary>
    /// Read an edge type.
    /// </summary>
    /// <param name="type">The type marked as an edge.</param>
    /// <returns>The <see cref="GraphEdgeModel"/>.</returns>
    /// <exception cref="EdgeMustHaveOneSourceAndOneTarget">Thrown when the edge does not have exactly one source and one target.</exception>
    /// <exception cref="EdgeCanHaveOnlyOneIdentifyingKey">Thrown when the edge has more than one key.</exception>
    public GraphEdgeModel ReadEdge(Type type)
    {
        var members = GraphMember.Of(type);
        var sources = members.Where(_ => _.Attributes.OfType<IGraphSourceAttribute>().Any()).ToArray();
        var targets = members.Where(_ => _.Attributes.OfType<IGraphTargetAttribute>().Any()).ToArray();
        if (sources.Length != 1 || targets.Length != 1) throw new EdgeMustHaveOneSourceAndOneTarget(type);

        var keys = members.Where(_ => _.Has<KeyAttribute>()).ToArray();
        if (keys.Length > 1) throw new EdgeCanHaveOnlyOneIdentifyingKey(type);

        var byEvent = _mappings.Read(members.Except([sources[0], targets[0]]));
        var attribute = (EdgeAttribute)type.GetCustomAttributes(typeof(EdgeAttribute), false).Single();
        var from = type.GetCustomAttributes(false).OfType<IEdgeFromAttribute>().Select(edge => new GraphEdgeFromModel(
            edge.EventType,
            Source(edge.SourceKey),
            Source(edge.TargetKey),
            edge.Identity is null ? null : Source(edge.Identity),
            byEvent.GetValueOrDefault(edge.EventType) ?? [])).ToArray();
        var removedWith = type.GetCustomAttributes(false).OfType<IEdgeRemovedWithAttribute>().Select(edge => new GraphEdgeRemovedWithModel(
            edge.EventType,
            Source(edge.SourceKey),
            Source(edge.TargetKey),
            edge.Identity is null ? null : Source(edge.Identity))).ToArray();

        return new(
            type,
            attribute.Type,
            sources[0].Attributes.OfType<IGraphSourceAttribute>().First().NodeType,
            targets[0].Attributes.OfType<IGraphTargetAttribute>().First().NodeType,
            keys.Length == 1 ? ProjectionPropertyPaths.NameOf(keys[0].Name, namingPolicy) : null,
            [ProjectionPropertyPaths.NameOf(sources[0].Name, namingPolicy), ProjectionPropertyPaths.NameOf(targets[0].Name, namingPolicy)],
            from,
            removedWith);
    }

    string Source(string name) => name.StartsWith('$') ? name : (string)ProjectionPropertyPaths.Resolve(name, namingPolicy, ProjectionPathRole.Source);

    string? KeyOf(IKeyedAttribute attribute)
    {
        if (attribute is IFromEventAttribute { ConstantKey: { } constant }) return $"{WellKnownExpressions.Value}({constant})";
        return attribute.Key is { } key ? Source(key) : null;
    }
}
