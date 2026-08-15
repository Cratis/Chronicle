// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions;

/// <summary>
/// Represents a <see cref="IEventValueExpression"/> for clearing a member back to no value.
/// </summary>
/// <remarks>
/// A clear is its own expression rather than a <see cref="ValueExpression"/> carrying the text of the keyword. The
/// kernel captures a constant's operand as text, so routing a clear through <c>$value(...)</c> wrote those literal
/// four characters into the member instead of clearing it.
/// </remarks>
public class NullExpression : IEventValueExpression
{
    /// <summary>
    /// Gets the single <see cref="NullExpression"/> instance - it carries no state.
    /// </summary>
    public static readonly NullExpression Instance = new();

    /// <inheritdoc/>
    public PropertyExpression Build() => WellKnownExpressions.Null;
}
