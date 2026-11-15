// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for assigning a value only when the target has no value yet.
/// </summary>
/// <remarks>
/// The expression is "$setOnce(&lt;event value expression&gt;)", where the inner expression is any event value expression -
/// including ones holding parentheses themselves, such as "$eventContext(occurred)" or "$causedBy(subject)".
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving the inner expression.</param>
public class SetOnceExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IReadModelPropertyExpressionResolver
{
    const string Prefix = $"{WellKnownExpressions.SetOnce}(";

    /// <summary>
    /// Try to get the inner event value expression of a set once expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="innerExpression">The inner expression when it is a set once expression.</param>
    /// <returns>True if it is a set once expression, false if not.</returns>
    public static bool TryGetInnerExpression(string expression, out string innerExpression)
    {
        if (expression.StartsWith(Prefix, StringComparison.Ordinal) && expression.EndsWith(')') && expression.Length > Prefix.Length + 1)
        {
            innerExpression = expression[Prefix.Length..^1].Trim();
            return innerExpression.Length > 0;
        }

        innerExpression = string.Empty;
        return false;
    }

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) =>
        TryGetInnerExpression(expression, out var innerExpression) && eventValueProviderExpressionResolvers.CanResolve(innerExpression);

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        TryGetInnerExpression(expression, out var innerExpression);
        return PropertyMappers.SetOnceFromEventValueProvider(targetProperty, eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, innerExpression));
    }
}
