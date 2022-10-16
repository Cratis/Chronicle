// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Exception that gets thrown when an event value expression is not supported.
/// </summary>
public class UnsupportedModelPropertyExpression : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedModelPropertyExpression"/> class.
    /// </summary>
    /// <param name="expression">The unsupported expression.</param>
    public UnsupportedModelPropertyExpression(string expression) : base($"Unknown model property expression '{expression}'")
    {
    }
}
