// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections.Expressions;

/// <summary>
/// Represents a <see cref="IEventValueExpression"/> that builds an event content property accessor expression.
/// </summary>
public class EventContentPropertyExpression : IEventValueExpression
{
    readonly PropertyPath _propertyPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventContextPropertyExpression"/> class.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> for the property.</param>
    public EventContentPropertyExpression(PropertyPath propertyPath) => _propertyPath = propertyPath;

    /// <inheritdoc/>
    public PropertyExpression Build() => (string)_propertyPath;
}
