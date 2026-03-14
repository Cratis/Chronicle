// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents a property expression key produced by an <see cref="IEventMigrationPropertyBuilder"/> operation.
/// </summary>
/// <param name="Value">Actual value.</param>
public record PropertyExpression(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a not-set <see cref="PropertyExpression"/>.
    /// </summary>
    public static readonly PropertyExpression NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PropertyExpression"/>.
    /// </summary>
    /// <param name="expression"><see cref="string"/> to convert from.</param>
    public static implicit operator PropertyExpression(string expression) => new(expression);

    /// <summary>
    /// Implicitly convert from <see cref="PropertyExpression"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="expression"><see cref="PropertyExpression"/> to convert from.</param>
    public static implicit operator string(PropertyExpression expression) => expression.Value;
}
