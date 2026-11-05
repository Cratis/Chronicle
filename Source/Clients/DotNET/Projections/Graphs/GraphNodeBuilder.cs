// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphNodeBuilder{TNode}"/>.
/// </summary>
/// <typeparam name="TNode">The type of node.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphNodeBuilder<TNode>(INamingPolicy namingPolicy) : IGraphNodeBuilder<TNode>
{
    readonly List<GraphNodeFromModel> _from = [];
    readonly List<GraphNodeRemovedWithModel> _removedWith = [];
    string? _keyProperty;

    /// <inheritdoc/>
    public IGraphNodeBuilder<TNode> IdentifiedBy<TKey>(Expression<Func<TNode, TKey>> property)
    {
        _keyProperty = (string)ProjectionPropertyPaths.FromExpression(property, namingPolicy, ProjectionPathRole.Target);
        return this;
    }

    /// <inheritdoc/>
    public IGraphNodeBuilder<TNode> From<TEvent>(Action<IGraphNodeFromBuilder<TEvent, TNode>>? configure = null)
    {
        var builder = new GraphNodeFromBuilder<TEvent, TNode>(namingPolicy);
        configure?.Invoke(builder);
        _from.Add(new(typeof(TEvent), builder.Key, builder.Properties, builder.UpdateOnly));
        return this;
    }

    /// <inheritdoc/>
    public IGraphNodeBuilder<TNode> RemovedWith<TEvent>(Action<IGraphNodeRemovedWithBuilder<TEvent>>? configure = null)
    {
        var builder = new GraphNodeRemovedWithBuilder<TEvent>(namingPolicy);
        configure?.Invoke(builder);
        _removedWith.Add(new(typeof(TEvent), builder.Key, builder.DetachEdges));
        return this;
    }

    /// <summary>
    /// Build the node model.
    /// </summary>
    /// <returns>The <see cref="GraphNodeModel"/>.</returns>
    /// <exception cref="NodeMustHaveExactlyOneKey">Thrown when the key is neither declared nor marked with the key attribute.</exception>
    public GraphNodeModel Build()
    {
        var key = _keyProperty ?? KeyOf(typeof(TNode), namingPolicy, 1);
        return new(typeof(TNode), typeof(TNode).Name, key, _from, _removedWith);
    }

    /// <summary>
    /// Get the name of the key property of a type, as it is named in the graph.
    /// </summary>
    /// <param name="type">The node type.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
    /// <param name="required">The number of members with the key attribute the type must have.</param>
    /// <returns>The name of the key property.</returns>
    /// <exception cref="NodeMustHaveExactlyOneKey">Thrown when the type does not have the required number of keys.</exception>
    internal static string KeyOf(Type type, INamingPolicy namingPolicy, int required)
    {
        var keys = GraphMember.Of(type).Where(_ => _.Has<KeyAttribute>()).ToArray();
        return keys.Length == required ? namingPolicy.GetPropertyName(keys[0].Name) : throw new NodeMustHaveExactlyOneKey(type, keys.Length);
    }
}
