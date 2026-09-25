// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Reads the declaration language's value expressions into a form every language generator can render.
/// </summary>
/// <remarks>
/// The expressions arrive as strings - <c language="csharp">$eventSourceId</c>, <c language="csharp">$eventContext(occurred)</c>,
/// <c language="csharp">$value(42)</c>, <c language="csharp">+= amount</c>, a bare property path. What they mean is the same whichever
/// language the result is written in, so it is read once here and each generator only decides how to
/// spell it.
/// </remarks>
public static class ProjectionExpressions
{
    /// <summary>
    /// Reads one property mapping from the declaration language.
    /// </summary>
    /// <param name="property">The read model property path being mapped.</param>
    /// <param name="expression">The expression the declaration assigns to it.</param>
    /// <returns>The mapping, in language-neutral terms.</returns>
    public static ProjectionPropertyMapping ReadMapping(PropertyPath property, string expression)
    {
        var path = property.Path;
        var normalized = expression.Trim();

        if (normalized.StartsWith("+=", StringComparison.Ordinal))
        {
            return new(path, ProjectionOperation.Add, ReadValue(normalized[2..].Trim()));
        }

        if (normalized.StartsWith("-=", StringComparison.Ordinal))
        {
            return new(path, ProjectionOperation.Subtract, ReadValue(normalized[2..].Trim()));
        }

        if (normalized.Equals("increment", StringComparison.Ordinal) || normalized == WellKnownExpressions.Increment)
        {
            return new(path, ProjectionOperation.Increment, null);
        }

        if (normalized.Equals("decrement", StringComparison.Ordinal) || normalized == WellKnownExpressions.Decrement)
        {
            return new(path, ProjectionOperation.Decrement, null);
        }

        if (normalized.Equals("count", StringComparison.Ordinal) || normalized == WellKnownExpressions.Count)
        {
            return new(path, ProjectionOperation.Count, null);
        }

        if (normalized == WellKnownExpressions.Null)
        {
            return new(path, ProjectionOperation.Clear, null);
        }

        return new(path, ProjectionOperation.Set, ReadValue(expression));
    }

    /// <summary>
    /// Reads one value expression from the declaration language.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <returns>Where the value comes from.</returns>
    public static ProjectionValueSource ReadValue(string expression)
    {
        var normalized = expression.Trim();

        if (string.Equals(normalized, WellKnownExpressions.Null, StringComparison.Ordinal) ||
            string.Equals(normalized, "null", StringComparison.Ordinal))
        {
            return ProjectionValueSource.Nothing;
        }

        if (string.Equals(normalized, WellKnownExpressions.EventSourceId, StringComparison.Ordinal))
        {
            return ProjectionValueSource.EventSourceId;
        }

        if (TryReadCall(normalized, WellKnownExpressions.EventContext, out var contextProperty))
        {
            return new(ProjectionValueKind.EventContextProperty, contextProperty);
        }

        if (TryReadCall(normalized, WellKnownExpressions.Value, out var constant))
        {
            return ReadConstant(constant);
        }

        // Classify only stored literal forms, without trimming: whitespace is not part of a literal.
        if (LiteralExpressionResolver.TryRead(expression, out var literal))
        {
            return literal is string text
                ? new(ProjectionValueKind.Text, text)
                : ReadConstant(expression);
        }

        return new(ProjectionValueKind.EventProperty, normalized);
    }

    static bool IsBoolean(string value) => LiteralExpressionResolver.TryRead(value, out var literal) && literal is bool;

    static ProjectionValueSource ReadConstant(string value)
    {
        if (string.Equals(value, "null", StringComparison.Ordinal))
        {
            return ProjectionValueSource.Nothing;
        }

        if (IsBoolean(value))
        {
            return new(ProjectionValueKind.Literal, value.ToLowerInvariant());
        }

        if (LiteralExpressionResolver.TryRead(value, out var literal) && literal is long or decimal or double)
        {
            return new(ProjectionValueKind.Literal, value);
        }

        return new(ProjectionValueKind.Text, value);
    }

    static bool TryReadCall(string expression, string name, out string argument)
    {
        var prefix = $"{name}(";
        if (expression.StartsWith(prefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            argument = expression[prefix.Length..^1];
            return true;
        }

        argument = string.Empty;
        return false;
    }
}
