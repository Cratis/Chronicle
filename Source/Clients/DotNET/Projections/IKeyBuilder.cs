// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Projections;

/// <summary>
/// Defines a builder for keys.
/// </summary>
public interface IKeyBuilder
{
    /// <summary>
    /// Builds the expression.
    /// </summary>
    /// <returns>The expression built.</returns>
    PropertyExpression Build();
}
