// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

/// <summary>
/// Resolves literals stored in projection definitions to constant event values.
/// </summary>
public class LiteralExpressionResolver : IEventValueProviderExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => TryRead(expression, out _);

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        TryRead(expression, out var value);
        return _ => value!;
    }

    /// <summary>
    /// Reads the same quoted-text, boolean, and invariant-number forms classified by
    /// <see cref="DeclarationLanguage.CodeGeneration.ProjectionExpressions.ReadValue"/>.
    /// </summary>
    /// <param name="expression">The stored expression.</param>
    /// <param name="value">The parsed constant.</param>
    /// <returns>Whether the expression is a literal.</returns>
    internal static bool TryRead(string expression, out object? value)
    {
        if (expression.Length >= 2 && expression[0] == '"' && expression[^1] == '"')
        {
            value = expression[1..^1];
            return true;
        }

        if (bool.TryParse(expression, out var boolean))
        {
            value = boolean;
            return true;
        }

        if (double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = number;
            return true;
        }

        value = null;
        return false;
    }
}
