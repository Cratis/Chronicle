// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders only every-event operations supported by the client builder interfaces.
/// </summary>
static class DeclarativeEveryExpressions
{
    /// <summary>
    /// Renders an every-event mapping or an explicit limitation at that mapping's position.
    /// </summary>
    /// <param name="property">The mapping target.</param>
    /// <param name="expression">The stored mapping expression.</param>
    /// <param name="fromAll">Whether the builder subscribes to all event types.</param>
    /// <returns>The fluent operation or a limitation comment.</returns>
    internal static string Operation(string property, string expression, bool fromAll)
    {
        var normalized = DeclarativeExpressions.Normalize(expression);
        var parts = property.Split([$".{WellKnownExpressions.EventContext}."], StringSplitOptions.None);
        var counter = Counter(normalized);
        var contextOperation = parts.Length == 1 ? ContextOperation(property, normalized) : null;
        var operation = fromAll && counter is not null ? CounterOperation(parts, counter) : contextOperation;
        return operation ?? $"/* Not expressible with the declarative builder: every-mapping '{property}'. */";
    }

    static string? Counter(string expression) => expression switch
    {
        WellKnownExpressions.Count => "Count",
        WellKnownExpressions.Increment => "Increment",
        WellKnownExpressions.Decrement => "Decrement",
        _ => null
    };

    static string CounterOperation(string[] parts, string counter) => parts.Length == 1
        ? $".{counter}(m => m.{parts[0]})"
        : $".{counter}(m => m.{parts[0]}, c => c.{CSharpDefinitionSupport.ContextPath(parts[1])})";

    static string? ContextOperation(string property, string expression)
    {
        var source = ProjectionExpressions.ReadValue(expression);
        return source.Kind is ProjectionValueKind.EventSourceId or ProjectionValueKind.EventContextProperty
            ? $".Set(m => m.{property}).{DeclarativeExpressions.Set(expression)}"
            : null;
    }
}
