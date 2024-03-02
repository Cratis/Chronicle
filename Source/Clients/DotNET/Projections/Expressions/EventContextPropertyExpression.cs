// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections.Expressions;

/// <summary>
/// Represents a <see cref="IKeyBuilder"/> that builds an event context property accessor expression.
/// </summary>
public class EventContextPropertyExpression : IEventValueExpression
{
    readonly PropertyPath _propertyPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventContextPropertyExpression"/> class.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> for the property.</param>
    public EventContextPropertyExpression(PropertyPath propertyPath) => _propertyPath = propertyPath;

    /// <inheritdoc/>
    public PropertyExpression Build() => $"$eventContext({_propertyPath})";
}
