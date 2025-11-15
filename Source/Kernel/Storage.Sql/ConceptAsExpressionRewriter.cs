// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Query expression visitor that rewrites ConceptAs property accesses and comparisons for EF Core query translation.
/// </summary>
/// <remarks>
/// This rewriter allows EF Core to translate queries that use ConceptAs types directly without
/// needing to manually extract the .Value property. It works by rewriting the expression tree
/// before EF Core processes it by:
/// - Detecting when a member access returns a ConceptAs type and converting it to the underlying primitive value type.
/// - Unwrapping explicit casts to ConceptAs types in binary comparisons (e.g., (ObserverId)stringValue == id) to keep comparisons at the primitive level.
/// - Preserving conversions in Select projections to maintain proper return types.
/// </remarks>
public class ConceptAsExpressionRewriter : ExpressionVisitor
{
    /// <summary>
    /// Rewrite an expression to handle ConceptAs types.
    /// </summary>
    /// <param name="expression">The expression to rewrite.</param>
    /// <returns>The rewritten expression.</returns>
    public static Expression Rewrite(Expression expression)
    {
        var rewriter = new ConceptAsExpressionRewriter();
        return rewriter.Visit(expression);
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        // Check if the member itself is a Concept type
        if (node.Type.IsConcept() && node.Expression is not null)
        {
            // Get the underlying value type for the Concept
            var valueType = node.Type.GetConceptValueType();

            // Create a conversion expression to extract the value
            return Expression.Convert(node, valueType);
        }

        return base.VisitMember(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Only handle conversions to Concept types in specific contexts
        // For now, we'll let the binary visitor handle comparisons
        // and keep conversions in projections (Select) unchanged

        // Visit the operand to handle any nested conversions
        var visitedOperand = Visit(node.Operand);

        if (visitedOperand != node.Operand)
        {
            return Expression.MakeUnary(node.NodeType, visitedOperand, node.Type, node.Method);
        }

        return base.VisitUnary(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Handle comparisons where ConceptAs types are compared directly
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Unwrap conversions to Concept types in comparisons
        if (left is UnaryExpression { NodeType: ExpressionType.Convert } leftConvert && leftConvert.Type.IsConcept())
        {
            left = leftConvert.Operand;
        }

        if (right is UnaryExpression { NodeType: ExpressionType.Convert } rightConvert && rightConvert.Type.IsConcept())
        {
            right = rightConvert.Operand;
        }

        // Now normalize both sides to their underlying value types if they are Concepts
        // This handles cases where one side is a Concept parameter/constant and the other is a primitive
        if (left.Type.IsConcept())
        {
            left = ExtractConceptValue(left);
        }

        if (right.Type.IsConcept())
        {
            right = ExtractConceptValue(right);
        }

        if (left != node.Left || right != node.Right)
        {
            // When we change the operand types, we need to clear the method parameter
            // because the original method (e.g., op_Equality for Concepts) won't match
            // the new operand types (underlying primitives)
            return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, null);
        }

        return base.VisitBinary(node);
    }

    /// <summary>
    /// Tries to extract the actual value from a Concept expression.
    /// For closure variables/constants, this evaluates and extracts the value.
    /// </summary>
    /// <param name="expression">The expression to extract the value from.</param>
    /// <returns>An expression representing the underlying value.</returns>
    static Expression ExtractConceptValue(Expression expression)
    {
        if (!expression.Type.IsConcept())
        {
            return expression;
        }

        var valueType = expression.Type.GetConceptValueType();

        // If this is a member access on a closure or constant, we can evaluate it
        // and create a constant with the underlying value
        if (expression is MemberExpression or ConstantExpression)
        {
            try
            {
                // Compile and evaluate the expression to get the actual Concept instance
                var lambda = Expression.Lambda(expression);
                var compiled = lambda.Compile();
                var conceptValue = compiled.DynamicInvoke();

                if (conceptValue != null)
                {
                    // Get the Value property from the Concept instance
                    var valueProperty = conceptValue.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var underlyingValue = valueProperty.GetValue(conceptValue);
                        return Expression.Constant(underlyingValue, valueType);
                    }
                }
            }
            catch
            {
                // If evaluation fails, fall back to property access
            }
        }

        // For other expressions (like query parameters), create a property access
        return Expression.Property(expression, "Value");
    }
}
