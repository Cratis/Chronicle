// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Serialization;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Reads the views declared over property graphs and checks them against the rules every Chronicle client applies.
/// </summary>
/// <remarks>
/// A view that breaks a rule is reported with the <see cref="InvalidGraphView"/> for that rule and left out; the other
/// views are read as declared. A view that leads to a view that was left out is left out too.
/// </remarks>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming members and properties.</param>
internal sealed class GraphViewReader(INamingPolicy namingPolicy)
{
    /// <summary>
    /// Read views.
    /// </summary>
    /// <param name="viewTypes">The types marked as views.</param>
    /// <param name="models">The <see cref="GraphModel">graphs</see> the views read.</param>
    /// <returns>The <see cref="GraphViews"/>.</returns>
    public GraphViews Read(IEnumerable<Type> viewTypes, IEnumerable<GraphModel> models)
    {
        var graphs = models.Select(_ => new GraphShape(_, namingPolicy)).ToArray();
        var failures = new Dictionary<Type, Exception>();
        var declared = new List<DeclaredView>();
        foreach (var type in viewTypes.Distinct())
        {
            var attribute = type.GetCustomAttributes(false).OfType<IGraphViewAttribute>().FirstOrDefault();
            if (attribute is null) continue;
            try
            {
                declared.Add(Declare(type, attribute, graphs));
            }
#pragma warning disable CA1031 // One invalid view must not take the other views with it.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                failures[type] = exception;
            }
        }

        foreach (var duplicate in declared.GroupBy(_ => (_.Graph.Name, _.Name)).Where(_ => _.Count() > 1).SelectMany(_ => _))
        {
            failures[duplicate.Type] = new DuplicateViewName(duplicate.Type, duplicate.Name, duplicate.Graph.Name);
        }

        var views = new Dictionary<Type, ReadView>();
        foreach (var view in declared.Where(_ => !failures.ContainsKey(_.Type)))
        {
            try
            {
                views[view.Type] = ReadMembers(view);
            }
#pragma warning disable CA1031 // One invalid view must not take the other views with it.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                failures[view.Type] = exception;
            }
        }

        foreach (var recursive in Recursive(views))
        {
            failures[recursive] = new RecursiveView(recursive);
            views.Remove(recursive);
        }

        RemoveUntilStable(views, failures);
        return Build(views, failures);
    }

    /// <summary>
    /// Declare a view: find the graph and node it is anchored at.
    /// </summary>
    /// <param name="type">The type of the view.</param>
    /// <param name="attribute">The <see cref="IGraphViewAttribute"/> on it.</param>
    /// <param name="graphs">The graphs.</param>
    /// <returns>The <see cref="DeclaredView"/>.</returns>
    /// <exception cref="ViewIsAlsoSchema">Thrown when the type is also a node or an edge.</exception>
    /// <exception cref="ViewRootNotANode">Thrown when the root is not a node in a graph.</exception>
    static DeclaredView Declare(Type type, IGraphViewAttribute attribute, IEnumerable<GraphShape> graphs)
    {
        if (type.IsDefined(typeof(NodeAttribute), false) || type.IsDefined(typeof(EdgeAttribute), false))
        {
            throw new ViewIsAlsoSchema(type);
        }

        var graph = GraphOf(graphs, attribute.NodeType) ?? throw new ViewRootNotANode(type, attribute.NodeType);
        return new(type, attribute.Name ?? type.Name, graph, graph.NodesByType[attribute.NodeType]);
    }

    static GraphShape? GraphOf(IEnumerable<GraphShape> graphs, Type nodeType)
    {
        var candidates = graphs.Where(_ => _.NodesByType.ContainsKey(nodeType)).ToArray();
        return candidates.FirstOrDefault(_ => _.Name == ModelBoundGraphReader.GraphNameOf(nodeType)) ?? candidates.FirstOrDefault();
    }

    static (Type Element, GraphViewCardinality Cardinality) CardinalityOf(Type type)
    {
        if (type != typeof(string))
        {
            if (type.IsArray) return (type.GetElementType()!, GraphViewCardinality.Many);
            var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? type
                : type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerable is not null && typeof(IEnumerable).IsAssignableFrom(type)) return (enumerable.GetGenericArguments()[0], GraphViewCardinality.Many);
        }

        return (Nullable.GetUnderlyingType(type) ?? type, GraphViewCardinality.One);
    }

    static IEnumerable<Type> Recursive(Dictionary<Type, ReadView> views)
    {
        foreach (var view in views.Keys)
        {
            var visited = new HashSet<Type>();
            var pending = new Stack<Type>(Targets(views, view));
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == view)
                {
                    yield return view;
                    break;
                }

                if (!visited.Add(current)) continue;
                foreach (var next in Targets(views, current)) pending.Push(next);
            }
        }
    }

    static IEnumerable<Type> Targets(Dictionary<Type, ReadView> views, Type view) =>
        views.TryGetValue(view, out var read) ? read.Relations.Select(_ => _.Target) : [];

    static void RemoveUntilStable(Dictionary<Type, ReadView> views, Dictionary<Type, Exception> failures)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var (type, view) in views.ToArray())
            {
                var lost = view.Relations.FirstOrDefault(_ => !views.ContainsKey(_.Target));
                if (lost is not null)
                {
                    failures[type] = new RelationTargetNotAView(type, lost.Member, lost.Target);
                    views.Remove(type);
                    changed = true;
                    continue;
                }

                if (view.EdgeProperties.Count == 0) continue;
                var edges = views.Values.SelectMany(_ => _.Relations).Where(_ => _.Target == type).Select(_ => _.Edge).Distinct().ToArray();
                Exception? failure = edges.Length switch
                {
                    0 => new EdgePropertyViewNotUsedAsTarget(type),
                    > 1 => new EdgePropertyViewSharedAcrossEdges(type, edges.Select(_ => _.EdgeType).Order(StringComparer.Ordinal)),
                    _ => view.EdgeProperties.Where(_ => !edges[0].Properties.Contains(_.Source)).Select(_ => new UnknownEdgeProperty(type, _.Member, _.Source, edges[0].EdgeType)).FirstOrDefault()
                };

                if (failure is not null)
                {
                    failures[type] = failure;
                    views.Remove(type);
                    changed = true;
                }
            }
        }
        while (changed);
    }

    GraphViews Build(Dictionary<Type, ReadView> views, Dictionary<Type, Exception> failures)
    {
        var definitions = views.Values
            .GroupBy(_ => _.Declared.Graph.Name, StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(_ => new GraphViewDefinition(_.Key, [.. _.Select(view => view.View).OrderBy(view => view.Name, StringComparer.Ordinal)], _.First().Declared.Graph.Schema))
            .ToArray();

        var nodes = new Dictionary<Type, GraphQueryNode>();
        foreach (var type in views.Keys) NodeFor(type, views, nodes);
        return new(
            namingPolicy,
            definitions,
            views.ToDictionary(_ => _.Key, _ => new GraphViewTarget(_.Value.Declared.Graph.Name, nodes[_.Key])),
            failures);
    }

    GraphQueryNode NodeFor(Type type, Dictionary<Type, ReadView> views, Dictionary<Type, GraphQueryNode> nodes)
    {
        if (nodes.TryGetValue(type, out var existing)) return existing;
        var view = views[type];
        var members = view.View.Members.Select(member =>
        {
            if (member.Kind != GraphViewMemberKind.Relation) return new GraphQueryMember(member.Name, member.Kind, member.Source);
            var relation = view.Relations.First(_ => _.MemberName == member.Name);
            return new GraphQueryMember(
                member.Name,
                member.Kind,
                null,
                new GraphQueryRelation(
                    relation.Edge.EdgeType,
                    member.Direction!.Value,
                    member.Cardinality!.Value,
                    relation.Edge.IdentifyingProperty,
                    NodeFor(relation.Target, views, nodes)));
        }).ToArray();

        var node = new GraphQueryNode(view.View.Name, view.Declared.Root.Label, view.Declared.Root.KeyProperty, members);
        nodes[type] = node;
        return node;
    }

    ReadView ReadMembers(DeclaredView view)
    {
        var members = new List<GraphViewMember>();
        var relations = new List<RelationRead>();
        var edgeProperties = new List<(string Member, string Source)>();
        var root = view.Root;
        var properties = view.Graph.NodeProperties[root.Label];
        foreach (var member in GraphMember.Of(view.Type))
        {
            var name = ProjectionPropertyPaths.NameOf(member.Name, namingPolicy);
            if (member.Attributes.OfType<IFollowsAttribute>().FirstOrDefault() is { } follows)
            {
                var (relation, definition) = ReadRelation(view, member, name, follows);
                relations.Add(relation);
                members.Add(definition);
            }
            else if (member.Attributes.OfType<FromEdgeAttribute>().FirstOrDefault() is { } fromEdge)
            {
                var source = fromEdge.Property ?? name;
                edgeProperties.Add((name, source));
                members.Add(GraphViewMember.EdgeProperty(name, source));
            }
            else
            {
                members.Add(GraphViewMember.Property(name, NodePropertyOf(view, member, name, properties)));
            }
        }

        return new(view, new(view.Name, root.Label, members), relations, edgeProperties);
    }

    (RelationRead Relation, GraphViewMember Definition) ReadRelation(DeclaredView view, GraphMember member, string name, IFollowsAttribute follows)
    {
        var (target, cardinality) = CardinalityOf(member.Type);
        var targetAttribute = target.GetCustomAttributes(false).OfType<IGraphViewAttribute>().FirstOrDefault()
            ?? throw new RelationTargetNotAView(view.Type, name, target);
        var edgeType = follows.EdgeType.GetCustomAttributes(typeof(EdgeAttribute), false).OfType<EdgeAttribute>().FirstOrDefault()?.Type ?? follows.EdgeType.Name;
        var edge = view.Graph.EdgesByType.GetValueOrDefault(edgeType) ?? throw new UnknownEdge(view.Type, name, edgeType);
        var root = view.Root.Label;
        GraphViewDirection direction;
        if (edge.SourceLabel == edge.TargetLabel)
        {
            if (follows.Direction is null) throw new AmbiguousDirection(view.Type, name, edgeType);
            if (edge.SourceLabel != root) throw new EdgeNotIncidentOnRoot(view.Type, name, edgeType, root);
            direction = follows.Direction.Value;
        }
        else
        {
            direction = follows.Direction ?? (edge.SourceLabel == root ? GraphViewDirection.Outgoing : GraphViewDirection.Incoming);
            var start = direction == GraphViewDirection.Outgoing ? edge.SourceLabel : edge.TargetLabel;
            if (start != root) throw new EdgeNotIncidentOnRoot(view.Type, name, edgeType, root);
        }

        var far = direction == GraphViewDirection.Outgoing ? edge.TargetLabel : edge.SourceLabel;
        var targetLabel = view.Graph.NodesByType.TryGetValue(targetAttribute.NodeType, out var targetNode) ? targetNode.Label : targetAttribute.NodeType.Name;
        if (targetLabel != far) throw new TargetLabelMismatch(view.Type, name, far, targetLabel);

        var targetName = targetAttribute.Name ?? target.Name;
        return (new(member.Name, name, target, edge), GraphViewMember.Relation(name, edgeType, direction, cardinality, targetName));
    }

    string NodePropertyOf(DeclaredView view, GraphMember member, string name, IReadOnlySet<string> properties)
    {
        if (member.Attributes.OfType<FromNodeAttribute>().FirstOrDefault() is { } fromNode)
        {
            return properties.Contains(fromNode.Property)
                ? properties.First(_ => string.Equals(_, fromNode.Property, StringComparison.OrdinalIgnoreCase))
                : throw new UnknownNodeProperty(view.Type, name, fromNode.Property, view.Root.Label);
        }

        if (member.Has<KeyAttribute>()) return view.Root.KeyProperty;
        if (properties.Contains(name)) return properties.First(_ => string.Equals(_, name, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase)) return view.Root.KeyProperty;
        throw new UnknownNodeProperty(view.Type, name, name, view.Root.Label);
    }

    sealed record DeclaredView(Type Type, string Name, GraphShape Graph, NodeShape Root);

    sealed record ReadView(DeclaredView Declared, GraphView View, IReadOnlyList<RelationRead> Relations, IReadOnlyList<(string Member, string Source)> EdgeProperties);

    sealed record RelationRead(string Member, string MemberName, Type Target, EdgeShape Edge);

    sealed record NodeShape(string Label, string KeyProperty);

    sealed record EdgeShape(string EdgeType, string SourceLabel, string TargetLabel, string? IdentifyingProperty, IReadOnlySet<string> Properties);

    sealed class GraphShape
    {
        public GraphShape(GraphModel model, INamingPolicy namingPolicy)
        {
            Name = model.Name;
            NodesByType = model.Nodes.GroupBy(_ => _.Type).ToDictionary(_ => _.Key, _ => new NodeShape(_.First().Label, _.First().KeyProperty));
            var nodes = model.Nodes.GroupBy(_ => _.Label).Select(_ => _.First())
                .Select(node => new GraphViewSchemaNode(node.Label, node.KeyProperty, [.. GraphMember.Of(node.Type).Select(member => ProjectionPropertyPaths.NameOf(member.Name, namingPolicy))]))
                .ToArray();
            var edges = model.Edges.GroupBy(_ => _.EdgeType).Select(_ => _.First()).Select(edge =>
            {
                var excluded = edge.ExcludedProperties.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var properties = GraphMember.Of(edge.Type)
                    .Select(member => ProjectionPropertyPaths.NameOf(member.Name, namingPolicy))
                    .Where(property => !excluded.Contains(property))
                    .ToArray();
                return new GraphViewSchemaEdge(edge.EdgeType, LabelOf(model, edge.SourceNode), LabelOf(model, edge.TargetNode), edge.IdentifyingProperty, properties);
            }).ToArray();

            Schema = new(nodes, edges);
            NodeProperties = nodes.ToDictionary(_ => _.Label, _ => (IReadOnlySet<string>)_.Properties.ToHashSet(StringComparer.OrdinalIgnoreCase));
            EdgesByType = edges.ToDictionary(
                _ => _.Type,
                _ => new EdgeShape(_.Type, _.Source, _.Target, _.IdentifiedBy, _.Properties.ToHashSet(StringComparer.Ordinal)));
        }

        public GraphViewSchema Schema { get; }

        public string Name { get; }

        public Dictionary<Type, NodeShape> NodesByType { get; }

        public Dictionary<string, IReadOnlySet<string>> NodeProperties { get; }

        public Dictionary<string, EdgeShape> EdgesByType { get; }

        static string LabelOf(GraphModel model, Type nodeType) => model.Nodes.FirstOrDefault(_ => _.Type == nodeType)?.Label ?? nodeType.Name;
    }
}
