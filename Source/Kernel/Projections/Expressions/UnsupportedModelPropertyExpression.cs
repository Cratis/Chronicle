// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Expressions;

/// <summary>
/// Exception that gets thrown when an event value expression is not supported.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnsupportedModelPropertyExpression"/> class.
/// </remarks>
/// <param name="expression">The unsupported expression.</param>
public class UnsupportedModelPropertyExpression(string expression) : Exception($"Unknown model property expression '{expression}'")
{
}
