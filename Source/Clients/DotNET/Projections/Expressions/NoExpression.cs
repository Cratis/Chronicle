// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IEventValueExpression"/> for representing no value.
/// </summary>
public class NoExpression : IEventValueExpression
{
    /// <inheritdoc/>
    public PropertyExpression Build() => string.Empty;
}
