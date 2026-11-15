// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Validates a <see cref="GraphViewQuery"/> against the views it reads.
/// </summary>
internal static class GraphViewQueryValidator
{
    /// <summary>
    /// Validate a query.
    /// </summary>
    /// <param name="views">The <see cref="GraphView">views</see> declared over the graph.</param>
    /// <param name="query">The <see cref="GraphViewQuery"/> to validate.</param>
    /// <returns>The <see cref="GraphViewIssue">issues</see> found, empty when the query is valid.</returns>
    internal static IReadOnlyList<GraphViewIssue> Validate(IReadOnlyList<GraphView> views, GraphViewQuery query)
    {
        var issues = new List<GraphViewIssue>();
        var byName = views.GroupBy(_ => _.Name, StringComparer.Ordinal).ToDictionary(_ => _.Key, _ => _.First(), StringComparer.Ordinal);
        if (!byName.TryGetValue(query.View, out var view))
        {
            issues.Add(new(GraphViewReason.InvalidQuery, query.View, null, $"The view '{query.View}' is not declared."));
            return issues;
        }

        foreach (var clause in query.Clauses)
        {
            switch (clause)
            {
                case GraphViewPropertyClause property:
                    CheckProperty(view, property, issues);
                    break;

                case GraphViewRelatedClause related:
                    var relation = view.Members.FirstOrDefault(_ => _.Name == related.Member && _.Kind == GraphViewMemberKind.Relation);
                    if (relation is null)
                    {
                        issues.Add(new(GraphViewReason.InvalidQuery, view.Name, related.Member, $"'{related.Member}' is not a relation of the view '{view.Name}'."));
                    }
                    else if (relation.View is not null && byName.TryGetValue(relation.View, out var target))
                    {
                        foreach (var nested in related.Clauses) CheckProperty(target, nested, issues);
                    }

                    break;

                default:
                    issues.Add(new(GraphViewReason.InvalidQuery, view.Name, null, "A clause is not a property or related clause."));
                    break;
            }
        }

        return issues;
    }

    static void CheckProperty(GraphView view, GraphViewPropertyClause clause, List<GraphViewIssue> issues)
    {
        if (!view.Members.Any(_ => _.Name == clause.Member && _.Kind == GraphViewMemberKind.Property))
        {
            issues.Add(new(GraphViewReason.InvalidQuery, view.Name, clause.Member, $"'{clause.Member}' is not a property of the view '{view.Name}'."));
        }
    }
}
