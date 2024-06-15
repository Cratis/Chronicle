// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Expressions;

/// <summary>
/// Represents a <see cref="IKeyBuilder"/> that builds an event context property accessor expression.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventContextPropertyExpression"/> class.
/// </remarks>
/// <param name="propertyPath"><see cref="PropertyPath"/> for the property.</param>
public class EventContextPropertyExpression(PropertyPath propertyPath) : IEventValueExpression
{
    /// <inheritdoc/>
    public PropertyExpression Build() => $"$eventContext({propertyPath})";
}
