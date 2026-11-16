// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Validates graph view declarations against the graph they read, by the rules every implementation of graph views keeps.
/// </summary>
public static class GraphViewValidator
{
    /// <summary>
    /// Validate views against a graph.
    /// </summary>
    /// <param name="graph">The <see cref="GraphDefinition"/> the views read.</param>
    /// <param name="views">The <see cref="GraphView">views</see> to validate.</param>
    /// <returns>The <see cref="GraphViewIssue">issues</see> found, empty when the views are valid.</returns>
    public static IReadOnlyList<GraphViewIssue> Validate(GraphDefinition graph, IReadOnlyList<GraphView> views)
    {
        var issues = new List<GraphViewIssue>();
        var byName = new Dictionary<string, GraphView>(StringComparer.Ordinal);
        foreach (var view in views)
        {
            if (!byName.TryAdd(view.Name, view))
            {
                issues.Add(new(GraphViewReason.DuplicateViewName, view.Name, null, $"A view named '{view.Name}' is declared more than once."));
            }
        }

        var edgesToView = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var view in byName.Values)
        {
            ValidateMembers(graph, view, byName, edgesToView, issues);
        }

        foreach (var view in byName.Values.Where(_ => _.Members.Any(member => member.Kind == GraphViewMemberKind.EdgeProperty)))
        {
            ValidateEdgeProperties(graph, view, edgesToView, issues);
        }

        foreach (var view in byName.Values.Where(_ => Reaches(_, _, byName, [])))
        {
            issues.Add(new(GraphViewReason.RecursiveView, view.Name, null, $"The view '{view.Name}' contains itself, directly or through other views."));
        }

        return issues;
    }

    static bool Reaches(GraphView from, GraphView target, Dictionary<string, GraphView> byName, HashSet<string> visited)
    {
        foreach (var next in from.Members.Where(_ => _.Kind == GraphViewMemberKind.Relation && _.View is not null && byName.ContainsKey(_.View)).Select(_ => byName[_.View!]))
        {
            if (next == target) return true;
            if (visited.Add(next.Name) && Reaches(next, target, byName, visited)) return true;
        }

        return false;
    }

    static void ValidateMembers(
        GraphDefinition graph,
        GraphView view,
        Dictionary<string, GraphView> byName,
        Dictionary<string, HashSet<string>> edgesToView,
        List<GraphViewIssue> issues)
    {
        var root = graph.Nodes.FirstOrDefault(_ => _.Label == view.Root);
        if (root is null)
        {
            issues.Add(new(GraphViewReason.ViewRootNotANode, view.Name, null, $"The root '{view.Root}' is not a node declared in the graph '{graph.Name}'."));
            return;
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in view.Members)
        {
            if (string.IsNullOrWhiteSpace(member.Name) || !names.Add(member.Name))
            {
                issues.Add(new(GraphViewReason.InvalidMember, view.Name, member.Name, "A member needs a name that is unique in its view."));
                continue;
            }

            switch (member.Kind)
            {
                case GraphViewMemberKind.Property:
                    if (string.IsNullOrWhiteSpace(member.Source))
                    {
                        issues.Add(new(GraphViewReason.InvalidMember, view.Name, member.Name, "A property member needs the property it reads."));
                    }
                    else if (GraphViewResolution.FindProperty(root.Schema, member.Source) is null)
                    {
                        issues.Add(new(GraphViewReason.UnknownNodeProperty, view.Name, member.Name, $"The node '{root.Label}' has no property '{member.Source}'."));
                    }

                    break;

                case GraphViewMemberKind.EdgeProperty:
                    if (string.IsNullOrWhiteSpace(member.Source))
                    {
                        issues.Add(new(GraphViewReason.InvalidMember, view.Name, member.Name, "An edge property member needs the property it reads."));
                    }

                    break;

                default:
                    ValidateRelation(graph, view, member, byName, edgesToView, issues);
                    break;
            }
        }
    }

    static void ValidateRelation(
        GraphDefinition graph,
        GraphView view,
        GraphViewMember member,
        Dictionary<string, GraphView> byName,
        Dictionary<string, HashSet<string>> edgesToView,
        List<GraphViewIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(member.Edge) || string.IsNullOrWhiteSpace(member.View))
        {
            issues.Add(new(GraphViewReason.InvalidMember, view.Name, member.Name, "A relation member needs the edge it follows and the view it leads to."));
            return;
        }

        var edge = graph.Edges.FirstOrDefault(_ => _.Type == member.Edge);
        if (edge is null)
        {
            issues.Add(new(GraphViewReason.UnknownEdge, view.Name, member.Name, $"The graph '{graph.Name}' has no edge '{member.Edge}'."));
            return;
        }

        var direction = GraphViewResolution.ResolveDirection(edge, view.Root, member.Direction, out var reason);
        if (direction is null)
        {
            var message = reason == GraphViewReason.AmbiguousDirection
                ? $"The edge '{edge.Type}' connects '{edge.SourceLabel}' to itself, so the relation must give a direction."
                : $"The edge '{edge.Type}' does not lead {(member.Direction is null ? "from" : "that way from")} '{view.Root}'.";
            issues.Add(new(reason!, view.Name, member.Name, message));
            return;
        }

        if (!byName.TryGetValue(member.View, out var target))
        {
            issues.Add(new(GraphViewReason.RelationTargetNotAView, view.Name, member.Name, $"The view '{member.View}' is not declared."));
            return;
        }

        var far = GraphViewResolution.FarLabel(edge, direction.Value);
        if (target.Root != far)
        {
            issues.Add(new(GraphViewReason.TargetLabelMismatch, view.Name, member.Name, $"The view '{target.Name}' is anchored at '{target.Root}', but the edge '{edge.Type}' leads to '{far}'."));
            return;
        }

        if (!edgesToView.TryGetValue(target.Name, out var edges)) edgesToView[target.Name] = edges = new(StringComparer.Ordinal);
        edges.Add(edge.Type);
    }

    static void ValidateEdgeProperties(GraphDefinition graph, GraphView view, Dictionary<string, HashSet<string>> edgesToView, List<GraphViewIssue> issues)
    {
        if (!edgesToView.TryGetValue(view.Name, out var edges) || edges.Count == 0)
        {
            issues.Add(new(GraphViewReason.EdgePropertyViewNotUsedAsTarget, view.Name, null, $"The view '{view.Name}' reads edge properties, but no relation leads to it."));
            return;
        }

        if (edges.Count > 1)
        {
            issues.Add(new(GraphViewReason.EdgePropertyViewSharedAcrossEdges, view.Name, null, $"The view '{view.Name}' reads edge properties, but relations of more than one edge type lead to it."));
            return;
        }

        var edge = graph.Edges.First(_ => _.Type == edges.Single());
        foreach (var member in view.Members.Where(_ => _.Kind == GraphViewMemberKind.EdgeProperty && !string.IsNullOrWhiteSpace(_.Source)))
        {
            if (GraphViewResolution.FindProperty(edge.Schema, member.Source!) is null)
            {
                issues.Add(new(GraphViewReason.UnknownEdgeProperty, view.Name, member.Name, $"The edge '{edge.Type}' has no property '{member.Source}'."));
            }
        }
    }
}
