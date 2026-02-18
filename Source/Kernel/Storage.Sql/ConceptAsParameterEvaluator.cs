// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Expression visitor that evaluates ConceptAs closure variables to constants BEFORE EF Core parameterizes them.
/// </summary>
public class ConceptAsParameterEvaluator : ExpressionVisitor
{
    /// <summary>
    /// Evaluate ConceptAs expressions to constants in the expression tree.
    /// </summary>
    /// <param name="expression">The expression to process.</param>
    /// <returns>The expression with ConceptAs values evaluated to constants.</returns>
    public static Expression Evaluate(Expression expression)
    {
        var evaluator = new ConceptAsParameterEvaluator();
        return evaluator.Visit(expression);
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Unwrap any Convert expressions wrapping ConceptAs types
        // This is critical because after parameter evaluation, we have primitives on one side
        // and possibly Convert(property, ConceptAs) on the other
        left = UnwrapConvertToConceptAs(left);
        right = UnwrapConvertToConceptAs(right);

        // If either operand changed, rebuild without the method
        // This is important because the original method might expect ConceptAs types
        if (left != node.Left || right != node.Right)
        {
            // Clear the method to let Expression.MakeBinary choose the appropriate operator
            return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, null);
        }

        return base.VisitBinary(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        // Check if this is a ConceptAs member access on a closure parameter
        // Pattern: __p_0.id where __p_0 is the closure parameter and id is ConceptAs<T>
        if (node.Type.IsConcept() && node.Expression is ParameterExpression closureParam)
        {
            // Try to find the constant expression for this closure from the cache
            var closureKey = closureParam.Type.FullName ?? closureParam.Type.Name;
            var closureConstant = ClosureConstantCache.Get(closureKey);

            if (closureConstant != null)
            {
                // Get the closure instance value
                var closureInstance = closureConstant.Value;
                if (closureInstance != null)
                {
                    try
                    {
                        // Get the ConceptAs value from the closure instance
                        var conceptValue = node.Member switch
                        {
                            System.Reflection.FieldInfo field => field.GetValue(closureInstance),
                            System.Reflection.PropertyInfo prop => prop.GetValue(closureInstance),
                            _ => null
                        };

                        if (conceptValue != null)
                        {
                            // Extract the primitive value from the ConceptAs instance
                            var valueProperty = conceptValue.GetType().GetProperty("Value");
                            if (valueProperty != null)
                            {
                                var primitiveValue = valueProperty.GetValue(conceptValue);
                                if (primitiveValue != null)
                                {
                                    var primitiveType = node.Type.GetConceptValueType();

                                    // Return a constant expression with the primitive value
                                    return Expression.Constant(primitiveValue, primitiveType);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Failed to extract value - fall through to base implementation
                    }
                }
            }
        }

        // Check if this is a ConceptAs member access on a closure (constant expression)
        // Pattern: closure.conceptField where conceptField is ConceptAs<T>
        if (node.Type.IsConcept() && node.Expression is ConstantExpression constantExpr)
        {
            try
            {
                // Extract the ConceptAs instance from the closure
                var closureInstance = constantExpr.Value;
                if (closureInstance != null)
                {
                    var conceptValue = node.Member switch
                    {
                        System.Reflection.FieldInfo field => field.GetValue(closureInstance),
                        System.Reflection.PropertyInfo prop => prop.GetValue(closureInstance),
                        _ => null
                    };

                    if (conceptValue != null)
                    {
                        // Extract the primitive value from the ConceptAs instance
                        var valueProperty = conceptValue.GetType().GetProperty("Value");
                        if (valueProperty != null)
                        {
                            var primitiveValue = valueProperty.GetValue(conceptValue);
                            if (primitiveValue != null)
                            {
                                var primitiveType = node.Type.GetConceptValueType();

                                // Return a constant expression with the primitive value
                                // This prevents EF Core from creating a ConceptAs parameter
                                return Expression.Constant(primitiveValue, primitiveType);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Failed to evaluate - fall through to base implementation
            }
        }

        // Visit the expression part of the member access (e.g., the object being accessed)
        var visitedExpression = Visit(node.Expression);

        // Special case: If user explicitly accessed .Value on a ConceptAs (e.g., recommendationId.Value)
        // and we've already evaluated the ConceptAs to a constant primitive, the visitedExpression
        // will now be a ConstantExpression containing the primitive. If the current member is "Value",
        // we should just return that constant since accessing .Value on a primitive would fail.
        if (node.Member.Name == "Value" &&
            visitedExpression is ConstantExpression constant &&
            !constant.Type.IsConcept())
        {
            // The expression is already the primitive value, return it as-is
            return visitedExpression;
        }

        // If the expression changed, rebuild the member access
        if (visitedExpression != node.Expression)
        {
            return Expression.MakeMemberAccess(visitedExpression, node.Member);
        }

        return base.VisitMember(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Handle Convert expressions - if we're converting FROM primitive TO ConceptAs,
        // and the operand is now a constant (because we evaluated it), we can skip the convert
        var visitedOperand = Visit(node.Operand);

        if (node.NodeType == ExpressionType.Convert &&
            node.Type.IsConcept() &&
            visitedOperand is ConstantExpression)
        {
            // The operand is now a constant primitive value, don't convert it back to ConceptAs
            return visitedOperand;
        }

        if (visitedOperand != node.Operand)
        {
            // Clear the method if operand changed - the original method might expect different types
            return Expression.MakeUnary(node.NodeType, visitedOperand, node.Type, null);
        }

        return base.VisitUnary(node);
    }

    static Expression UnwrapConvertToConceptAs(Expression expression)
    {
        // Check for Convert expressions that convert to ConceptAs types
        while (expression is UnaryExpression unary &&
               unary.NodeType == ExpressionType.Convert &&
               unary.Type.IsConcept())
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
