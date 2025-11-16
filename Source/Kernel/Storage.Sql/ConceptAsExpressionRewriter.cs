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
    protected override Expression VisitConstant(ConstantExpression node)
    {
        // If we have a constant of ConceptAs type, unwrap it immediately
        if (node.Type.IsConcept() && node.Value != null)
        {
            var valueProperty = node.Value.GetType().GetProperty("Value");
            if (valueProperty != null)
            {
                var underlyingValue = valueProperty.GetValue(node.Value);
                var valueType = node.Type.GetConceptValueType();
                System.Diagnostics.Debug.WriteLine($"Unwrapping ConceptAs constant to: {underlyingValue}");
                return Expression.Constant(underlyingValue, valueType);
            }
        }

        return base.VisitConstant(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        // Check if the member itself is a Concept type BEFORE visiting children
        // This is important because we need to handle the entire concept expression as one unit
        if (node.Type.IsConcept())
        {
            // First check if this is accessing a closure variable (captured variable in lambda)
            // Pattern: closure_instance.id where id is ConceptAs<T>
            if (node.Expression is ConstantExpression)
            {
                // This is accessing a field/property on a closure - evaluate it to a constant
                return ExtractConceptValue(node);
            }

            // Otherwise, it's an entity property - append .Value to access the underlying value
            // Pattern: entity.Id where Id is ConceptAs<T> stored in DB as primitive
            return Expression.Property(node, "Value");
        }

        // For non-Concept members, visit the expression that this member belongs to
        var visitedExpression = Visit(node.Expression);

        // Rebuild the member access if the expression changed
        if (visitedExpression != node.Expression)
        {
            return Expression.MakeMemberAccess(visitedExpression, node.Member);
        }

        return base.VisitMember(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Don't unwrap conversions here - let VisitBinary handle them in comparison context
        // If we unwrap here, we'll break projections (Select) that expect ConceptAs return types

        // Visit the operand to handle any nested expressions
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

        // Unwrap conversions TO ConceptAs types in comparisons
        // This handles: (ObserverId)observer.Id == id
        // We want: observer.Id == (primitive)id
        if (left is UnaryExpression { NodeType: ExpressionType.Convert } leftConvert)
        {
            // If converting FROM primitive TO ConceptAs, unwrap the conversion
            var leftValueType = leftConvert.Type.GetConceptValueType();
            if (leftConvert.Operand.Type == leftValueType)
            {
                left = leftConvert.Operand;
            }
        }

        if (right is UnaryExpression { NodeType: ExpressionType.Convert } rightConvert)
        {
            // If converting FROM primitive TO ConceptAs, unwrap the conversion
            var rightValueType = rightConvert.Type.GetConceptValueType();
            if (rightConvert.Operand.Type == rightValueType)
            {
                right = rightConvert.Operand;
            }
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
    /// For entity properties, this creates a property access to the Value property.
    /// For already-parameterized expressions (__id_0), creates .Value access on the parameter.
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

        // Special handling for ParameterExpression - these are EF Core's compiled lambda parameters
        // Since the evaluatable filter didn't work to prevent parameterization, we need to handle these
        // We can't just convert the parameter - we need to replace it with a new parameter of the primitive type
        if (expression is ParameterExpression paramExpr)
        {
            // Create a new parameter with the same name but primitive type
            // This tells EF Core to treat this parameter as the underlying type in SQL
            return Expression.Parameter(valueType, paramExpr.Name);
        }

        // Special handling for member access on closure variables
        // This is the most common case: accessing a captured variable in a lambda
        if (expression is MemberExpression memberExpr &&
            memberExpr.Expression is ConstantExpression constantExpr)
        {
            // This is a field/property access on a constant (closure class)
            // We can directly get the value without compiling
            try
            {
                var member = memberExpr.Member;
                var closureInstance = constantExpr.Value;

                if (closureInstance != null)
                {
                    // Get the ConceptAs instance from the closure
                    var conceptValue = member switch
                    {
                        System.Reflection.FieldInfo field => field.GetValue(closureInstance),
                        System.Reflection.PropertyInfo prop => prop.GetValue(closureInstance),
                        _ => null
                    };

                    if (conceptValue != null)
                    {
                        // Extract the underlying primitive value from the ConceptAs instance
                        var valueProperty = conceptValue.GetType().GetProperty("Value");
                        if (valueProperty != null)
                        {
                            var underlyingValue = valueProperty.GetValue(conceptValue);
                            if (underlyingValue != null)
                            {
                                return Expression.Constant(underlyingValue, valueType);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to extract from closure: {ex.Message}");
            }
        }

        // Try to evaluate the expression first (for other cases)
        // This will only work if the expression doesn't reference query parameters
        try
        {
            // Compile and evaluate the expression to get the actual Concept instance
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            var conceptValue = compiled.DynamicInvoke();

            if (conceptValue != null)
            {
                // Extract the underlying primitive value
                var valueProperty = conceptValue.GetType().GetProperty("Value");
                if (valueProperty != null)
                {
                    var underlyingValue = valueProperty.GetValue(conceptValue);
                    if (underlyingValue != null)
                    {
                        // Successfully evaluated - return as constant
                        System.Diagnostics.Debug.WriteLine($"Successfully evaluated ConceptAs to constant: {underlyingValue}");
                        return Expression.Constant(underlyingValue, valueType);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Evaluation failed - this is expected for entity properties that reference query parameters
            System.Diagnostics.Debug.WriteLine($"Failed to evaluate ConceptAs (expected for entity properties): {ex.GetType().Name}");
        }

        // If we couldn't evaluate (entity property), create a property access to the Value property
        System.Diagnostics.Debug.WriteLine("Creating property access to .Value for entity property");
        return Expression.Property(expression, "Value");
    }
}
