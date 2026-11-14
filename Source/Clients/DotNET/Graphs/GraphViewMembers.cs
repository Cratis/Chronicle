// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Resolves the member a selector expression names to the member of a <see cref="GraphQueryNode"/>.
/// </summary>
/// <param name="views">The <see cref="IGraphViews"/> the names were given by.</param>
internal sealed class GraphViewMembers(IGraphViews views)
{
    /// <summary>
    /// Resolve a selector.
    /// </summary>
    /// <param name="viewType">The type of the view.</param>
    /// <param name="node">The <see cref="GraphQueryNode"/> of the view.</param>
    /// <param name="selector">The selector expression.</param>
    /// <returns>The <see cref="GraphQueryMember"/>.</returns>
    /// <exception cref="MemberExpressionMustSelectAMember">Thrown when the expression does not select a member.</exception>
    /// <exception cref="GraphViewMemberNotFound">Thrown when the view has no such member.</exception>
    public GraphQueryMember Resolve(Type viewType, GraphQueryNode node, LambdaExpression selector)
    {
        var body = selector.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert ? convert.Operand : selector.Body;
        if (body is not MemberExpression { Expression: ParameterExpression } member) throw new MemberExpressionMustSelectAMember(selector.ToString());
        var name = views.NamingPolicy.GetPropertyName(member.Member.Name);
        return node.Members.FirstOrDefault(_ => _.Name == name)
            ?? node.Members.FirstOrDefault(_ => string.Equals(_.Name, member.Member.Name, StringComparison.OrdinalIgnoreCase))
            ?? throw new GraphViewMemberNotFound(viewType, member.Member.Name);
    }

    /// <summary>
    /// Build a property clause.
    /// </summary>
    /// <param name="viewType">The type of the view.</param>
    /// <param name="node">The <see cref="GraphQueryNode"/> of the view.</param>
    /// <param name="selector">The selector expression.</param>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="GraphPropertyClause"/>.</returns>
    /// <exception cref="GraphViewMemberIsNotAProperty">Thrown when the member is not a property of the node.</exception>
    public GraphPropertyClause Property(Type viewType, GraphQueryNode node, LambdaExpression selector, object? value)
    {
        var member = Resolve(viewType, node, selector);
        if (member.Kind != GraphViewMemberKind.Property) throw new GraphViewMemberIsNotAProperty(viewType, member.Name);
        return new(member.Name, member.Source!, GraphValueConverter.ToGraph(value));
    }
}
