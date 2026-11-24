// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// The exception that is thrown when a flat composite key expression is malformed or cannot be used as a key.
/// </summary>
/// <param name="expression">The complete expression.</param>
/// <param name="reason">Why the expression is refused.</param>
public class InvalidFlatCompositeKeyExpression(string expression, string reason) : Exception($"Invalid flat composite key expression '{expression}': {reason}")
{
    /// <summary>
    /// Gets the reason the expression is refused.
    /// </summary>
    public string Reason { get; } = reason;
}
