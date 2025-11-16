// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Custom evaluatable expression filter that marks ConceptAs member accesses as evaluatable
/// so EF Core evaluates them to constants before query translation.
/// </summary>
/// <remarks>
/// This filter tells EF Core that expressions accessing ConceptAs members from closures
/// should be evaluated client-side BEFORE the query is translated. This allows ConceptAs
/// types to be automatically unwrapped to their primitive values without manual .Value calls.
/// </remarks>
/// <param name="dependencies">The evaluatable expression filter dependencies.</param>
/// <param name="relationalDependencies">The relational evaluatable expression filter dependencies.</param>
public class ConceptAsEvaluatableExpressionFilter(
    EvaluatableExpressionFilterDependencies dependencies,
    RelationalEvaluatableExpressionFilterDependencies relationalDependencies)
    : RelationalEvaluatableExpressionFilter(dependencies, relationalDependencies)
{
    /// <inheritdoc/>
    public override bool IsEvaluatableExpression(Expression expression, Microsoft.EntityFrameworkCore.Metadata.IModel model)
    {
        // Mark ANY ConceptAs typed expression as evaluatable if it's not an entity property
        // This ensures ConceptAs values from closures are evaluated to their primitive values
        if (expression.Type.IsConcept())
        {
            // Check if this is NOT an entity property access
            // Entity property access looks like: Property(parameterExpression, "PropertyName")
            // or MemberAccess(parameterExpression.Property)
            if (expression is MemberExpression memberExpr)
            {
                // If the member's declaring type is an entity type in the model, don't evaluate it
                // Otherwise (closure variable, local variable, etc.), mark as evaluatable
                var declaringType = memberExpr.Member.DeclaringType;
                if (declaringType != null && model.FindEntityType(declaringType) != null)
                {
                    // This is an entity property, let EF handle it
                    return base.IsEvaluatableExpression(expression, model);
                }

                // This is a ConceptAs from a closure/local - evaluate it!
                return true;
            }

            // For other ConceptAs expressions (constants, etc.), evaluate them
            if (expression is ConstantExpression)
            {
                return true;
            }
        }

        // For Convert expressions involving ConceptAs, check the operand
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            if (unary.Type.IsConcept() || unary.Operand.Type.IsConcept())
            {
                return IsEvaluatableExpression(unary.Operand, model);
            }
        }

        return base.IsEvaluatableExpression(expression, model);
    }
}
