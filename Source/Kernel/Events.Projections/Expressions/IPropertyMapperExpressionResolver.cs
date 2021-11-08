// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Defines a resolver of expressions related to property mapping.
    /// </summary>
    public interface IPropertyMapperExpressionResolver
    {
        /// <summary>
        /// Called to check if the resolver can resolve the expression.
        /// </summary>
        /// <param name="targetProperty">The target property we're mapping to.</param>
        /// <param name="expression">Expression to check.</param>
        /// <returns>True if it can resolve, false if not.</returns>
        bool CanResolve(Property targetProperty, string expression);

        /// <summary>
        /// Called to resolve the expression.
        /// </summary>
        /// <param name="targetProperty">The target property we're mapping to.</param>
        /// <param name="expression">Expression to resolve</param>
        /// <returns><see cref="PropertyMapper"/> it resolves to.</returns>
        PropertyMapper Resolve(Property targetProperty, string expression);
    }
}
