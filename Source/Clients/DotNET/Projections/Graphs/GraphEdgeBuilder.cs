// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphEdgeBuilder{TEdge, TSource, TTarget}"/>.
/// </summary>
/// <typeparam name="TEdge">The type of edge.</typeparam>
/// <typeparam name="TSource">The type of the node the edge starts at.</typeparam>
/// <typeparam name="TTarget">The type of the node the edge ends at.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
internal sealed class GraphEdgeBuilder<TEdge, TSource, TTarget>(INamingPolicy namingPolicy) : IGraphEdgeBuilder<TEdge, TSource, TTarget>
{
    readonly List<GraphEdgeFromModel> _from = [];
    readonly List<GraphEdgeRemovedWithModel> _removedWith = [];
    string? _type;
    string? _identifyingProperty;

    /// <inheritdoc/>
    public IGraphEdgeBuilder<TEdge, TSource, TTarget> Named(string type)
    {
        _type = type;
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeBuilder<TEdge, TSource, TTarget> IdentifiedBy<TIdentity>(Expression<Func<TEdge, TIdentity>> property)
    {
        _identifyingProperty = (string)ProjectionPropertyPaths.FromExpression(property, namingPolicy, ProjectionPathRole.Target);
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeBuilder<TEdge, TSource, TTarget> From<TEvent>(Action<IGraphEdgeFromBuilder<TEvent, TEdge>> configure)
    {
        var builder = new GraphEdgeFromBuilder<TEvent, TEdge>(namingPolicy);
        configure(builder);
        _from.Add(new(typeof(TEvent), builder.SourceKey, builder.TargetKey, builder.Identity, builder.Properties));
        return this;
    }

    /// <inheritdoc/>
    public IGraphEdgeBuilder<TEdge, TSource, TTarget> RemovedWith<TEvent>(Action<IGraphEdgeRemovedWithBuilder<TEvent>> configure)
    {
        var builder = new GraphEdgeRemovedWithBuilder<TEvent>(namingPolicy);
        configure(builder);
        _removedWith.Add(new(typeof(TEvent), builder.SourceKey, builder.TargetKey, builder.Identity));
        return this;
    }

    /// <summary>
    /// Build the edge model.
    /// </summary>
    /// <returns>The <see cref="GraphEdgeModel"/>.</returns>
    /// <exception cref="EdgeMustHaveOneSourceAndOneTarget">Thrown when the edge type does not say which members hold the keys of the end nodes.</exception>
    /// <exception cref="EdgeCanHaveOnlyOneIdentifyingKey">Thrown when the edge type has more than one key.</exception>
    public GraphEdgeModel Build()
    {
        var members = GraphMember.Of(typeof(TEdge));
        var type = _type ?? ((EdgeAttribute?)typeof(TEdge).GetCustomAttributes(typeof(EdgeAttribute), false).FirstOrDefault())?.Type ?? typeof(TEdge).Name;
        var source = members.FirstOrDefault(_ => _.Has<IGraphSourceAttribute>());
        var target = members.FirstOrDefault(_ => _.Has<IGraphTargetAttribute>());
        if (source is null || target is null) throw new EdgeMustHaveOneSourceAndOneTarget(typeof(TEdge));

        var keys = members.Where(_ => _.Has<KeyAttribute>()).ToArray();
        if (keys.Length > 1) throw new EdgeCanHaveOnlyOneIdentifyingKey(typeof(TEdge));

        return new(
            typeof(TEdge),
            type,
            typeof(TSource),
            typeof(TTarget),
            _identifyingProperty ?? (keys.Length == 1 ? ProjectionPropertyPaths.NameOf(keys[0].Name, namingPolicy) : null),
            [ProjectionPropertyPaths.NameOf(source.Name, namingPolicy), ProjectionPropertyPaths.NameOf(target.Name, namingPolicy)],
            _from,
            _removedWith);
    }
}
