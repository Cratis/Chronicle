// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions;

/// <summary>
/// Represents a <see cref="IEventValueExpression"/> for setting a constant value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ValueExpression"/> class.
/// </remarks>
/// <param name="value">The value to set.</param>
public class ValueExpression(string value) : IEventValueExpression
{
    /// <summary>
    /// Represents a null value.
    /// </summary>
    [Obsolete($"A clear is no longer a constant - use {nameof(NullExpression)}.{nameof(NullExpression.Instance)}, which builds the $null expression. This built $value(null), which wrote the literal string \"null\".")]
    public static readonly ValueExpression Null = new("null");

    /// <inheritdoc/>
    public PropertyExpression Build() => $"{WellKnownExpressions.Value}({value})";
}
