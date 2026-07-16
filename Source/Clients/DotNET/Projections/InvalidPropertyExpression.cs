// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a projection builder accessor is not a supported member-access expression.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidPropertyExpression"/> class.
/// </remarks>
/// <param name="target">A description of what the expression was mapping (the projection, target property, or key).</param>
/// <param name="expression">The offending <see cref="Expression"/>.</param>
public class InvalidPropertyExpression(string target, Expression expression)
    : Exception($"The expression `{expression}` used for {target} is not a supported member-access expression. " +
                "Projection builder accessors extract a property path at definition time and are never executed at runtime, " +
                "so only a direct member-access accessor rooted in the lambda parameter is supported (for example `e => e.Property` or `e => e.Parent.Child`). " +
                "Method calls, string interpolation, arithmetic, conditionals, constants, or expressions that ignore the parameter cannot be mapped — " +
                "record the derived value as a fact on the event, or use a reducer.");
