// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions;

/// <summary>
/// Represents a <see cref="IEventValueExpression"/> that builds an event content property accessor expression.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventContextPropertyExpression"/> class.
/// </remarks>
/// <param name="propertyPath"><see cref="PropertyPath"/> for the property.</param>
public class EventContentPropertyExpression(PropertyPath propertyPath) : IEventValueExpression
{
    /// <inheritdoc/>
    public PropertyExpression Build() => (string)propertyPath;
}
