// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Properties;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions.Keys;

/// <summary>
/// Defines a system that knows about all <see cref="IKeyExpressionResolver"/>.
/// </summary>
public interface IKeyExpressionResolvers
{
    /// <summary>
    /// Called to check if there are any resolver that can resolve the expression.
    /// </summary>
    /// <param name="expression">Expression to check.</param>
    /// <returns>True if it can resolve, false if not.</returns>
    bool CanResolve(string expression);

    /// <summary>
    /// Called to resolve the expression.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to resolve key for.</param>
    /// <param name="expression">Expression to resolve.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <returns><see cref="KeyResolver"/> it resolves to.</returns>
    KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty);
}
