// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Expressions;

/// <summary>
/// Defines a builder for event value expressions.
/// </summary>
public interface IEventValueExpression
{
    /// <summary>
    /// Builds the expression.
    /// </summary>
    /// <returns>The expression built.</returns>
    PropertyExpression Build();
}
