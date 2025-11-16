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
        Console.WriteLine($"[ConceptAsEvaluatableExpressionFilter] Checking: {expression} (Type: {expression.Type.Name}, NodeType: {expression.NodeType})");

        // First, check if the base implementation says this is NOT evaluatable
        // If it's not evaluatable for standard reasons, respect that
        var baseResult = base.IsEvaluatableExpression(expression, model);
        Console.WriteLine($"[ConceptAsEvaluatableExpressionFilter] Base result: {baseResult}");

        // Mark ANY ConceptAs typed expression as evaluatable if it's not an entity property
        // This ensures ConceptAs values from closures are evaluated to their primitive values
        if (expression.Type.IsConcept())
        {
            Console.WriteLine("[ConceptAsEvaluatableExpressionFilter] Found ConceptAs expression!");

            // Check if this is NOT an entity property access
            // Entity property access looks like: Property(parameterExpression, "PropertyName")
            // or MemberAccess(parameterExpression.Property)
            if (expression is MemberExpression memberExpr)
            {
                Console.WriteLine($"[ConceptAsEvaluatableExpressionFilter] MemberExpression, Member: {memberExpr.Member.Name}, Expression: {memberExpr.Expression}");

                // If the member's declaring type is an entity type in the model, don't evaluate it
                // Otherwise (closure variable, local variable, etc.), mark as evaluatable
                var declaringType = memberExpr.Member.DeclaringType;
                if (declaringType != null && model.FindEntityType(declaringType) != null)
                {
                    Console.WriteLine("[ConceptAsEvaluatableExpressionFilter] This is an entity property, DON'T evaluate");

                    // This is an entity property, let EF handle it
                    return baseResult;
                }

                Console.WriteLine("[ConceptAsEvaluatableExpressionFilter] This is a closure/local variable");

                // Store the closure constant so the parameter evaluator can use it
                if (memberExpr.Expression is ConstantExpression closureConstant && closureConstant.Value != null)
                {
                    ClosureConstantCache.Store(closureConstant);
                }

                // Return FALSE to prevent EF Core from creating a parameter!
                // This keeps the closure expression in the tree so our rewriter can evaluate it
                return false;
            }

            // For other ConceptAs expressions (constants, parameters from closures), evaluate them
            if (expression is ConstantExpression)
            {
                Console.WriteLine("[ConceptAsEvaluatableExpressionFilter] ConstantExpression, EVALUATE IT!");
                return true;
            }

            // For ParameterExpression, check if it's a lambda parameter (entity) or query parameter (closure)
            // Lambda parameters have names like "e", "o", etc. and should not be evaluated
            // Query parameters have names like "id", "sequenceNumber", etc. from closures
            if (expression is ParameterExpression paramExpr)
            {
                // Lambda parameters (e, o, x, etc.) are part of the query structure - don't evaluate
                // But captured variables that become parameters should be evaluated
                // The problem: at this stage, we can't easily distinguish them
                // So we mark ConceptAs parameters as evaluatable - they should be constants anyway
                Console.WriteLine("[ConceptAsEvaluatableExpressionFilter] ParameterExpression: " + paramExpr.Name + ", DON'T evaluate (it's a lambda parameter)");
                return baseResult;
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

        return baseResult;
    }
}
