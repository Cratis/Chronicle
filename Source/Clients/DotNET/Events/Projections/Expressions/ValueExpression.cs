// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Represents a <see cref="IEventValueExpression"/> for setting a constant value.
/// </summary>
public class ValueExpression : IEventValueExpression
{
    readonly string _value;

    /// <summary>
    /// Represents a null value.
    /// </summary>
    public static ValueExpression Null = new("null");

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueExpression"/> class.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public ValueExpression(string value) => _value = value;

    /// <inheritdoc/>
    public string Build() => $"$value({_value})";
}
