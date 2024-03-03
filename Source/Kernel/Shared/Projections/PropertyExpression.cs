// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Projections;

/// <summary>
/// Represents the definition of a key.
/// </summary>
/// <param name="Expression">The key expression.</param>
public record PropertyExpression(string Expression) : ConceptAs<string>(Expression)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PropertyExpression"/>.
    /// </summary>
    /// <param name="expression">Expression string.</param>
    public static implicit operator PropertyExpression(string expression) => new(expression);
}
