// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Graphs.Views;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Converts the view declarations and clauses of a preview request to the views and query the kernel evaluates.
/// </summary>
internal static class GraphViewDeclarationConverters
{
    /// <summary>
    /// Convert view declarations to views.
    /// </summary>
    /// <param name="declarations">The <see cref="GraphViewDeclaration">declarations</see>.</param>
    /// <param name="errors">The <see cref="GraphViewError">errors</see> for the members that could not be converted.</param>
    /// <returns>The <see cref="GraphView">views</see>.</returns>
    internal static IReadOnlyList<GraphView> ToViews(this IEnumerable<GraphViewDeclaration>? declarations, List<GraphViewError> errors) =>
        [.. (declarations ?? []).Select(declaration => new GraphView(declaration.Name, declaration.Root, [.. declaration.Members.Select(member => ToMember(declaration.Name, member, errors))]))];

    /// <summary>
    /// Convert the clauses of a request to the clauses of a query.
    /// </summary>
    /// <param name="declarations">The <see cref="GraphViewClauseDeclaration">declarations</see>.</param>
    /// <param name="view">The name of the view the clauses are on.</param>
    /// <param name="errors">The <see cref="GraphViewError">errors</see> for the clauses that could not be converted.</param>
    /// <returns>The <see cref="GraphViewClause">clauses</see>.</returns>
    internal static IReadOnlyList<GraphViewClause> ToClauses(this IEnumerable<GraphViewClauseDeclaration>? declarations, string view, List<GraphViewError> errors)
    {
        var clauses = new List<GraphViewClause>();
        foreach (var declaration in declarations ?? [])
        {
            if (!string.IsNullOrEmpty(declaration.Relation))
            {
                IReadOnlyList<GraphViewPropertyClause> nested = string.IsNullOrEmpty(declaration.Member) ? [] : [new(declaration.Member, declaration.Value)];
                clauses.Add(new GraphViewRelatedClause(declaration.Relation, nested));
            }
            else if (!string.IsNullOrEmpty(declaration.Member))
            {
                clauses.Add(new GraphViewPropertyClause(declaration.Member, declaration.Value));
            }
            else
            {
                errors.Add(new(GraphViewReason.InvalidQuery, view, null, "A clause names a relation or a property member."));
            }
        }

        return clauses;
    }

    static GraphViewMember ToMember(string view, GraphViewMemberDeclaration member, List<GraphViewError> errors)
    {
        var kind = Parse<GraphViewMemberKind>(member.Kind) ?? GraphViewMemberKind.Property;
        if (Parse<GraphViewMemberKind>(member.Kind) is null)
        {
            errors.Add(new(GraphViewReason.InvalidMember, view, member.Name, $"The kind '{member.Kind}' is not property, relation or edgeProperty."));
        }

        GraphViewDirection? direction = null;
        if (!string.IsNullOrEmpty(member.Direction))
        {
            direction = Parse<GraphViewDirection>(member.Direction);
            if (direction is null)
            {
                errors.Add(new(GraphViewReason.InvalidMember, view, member.Name, $"The direction '{member.Direction}' is not outgoing or incoming."));
            }
        }

        var cardinality = GraphViewCardinality.Many;
        if (!string.IsNullOrEmpty(member.Cardinality))
        {
            if (Parse<GraphViewCardinality>(member.Cardinality) is { } parsed)
            {
                cardinality = parsed;
            }
            else
            {
                errors.Add(new(GraphViewReason.InvalidMember, view, member.Name, $"The cardinality '{member.Cardinality}' is not many or one."));
            }
        }

        return new(member.Name, kind, member.Source, member.Edge, direction, cardinality, member.View);
    }

    static TEnum? Parse<TEnum>(string? text)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(text, true, out var value) && Enum.IsDefined(value) ? value : null;
}
