// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Defines a system for resolving an expression. It represents known expression resolvers in the system.
    /// </summary>
    public interface IPropertyMapperExpressionResolvers
    {
        /// <summary>
        /// Called to verify if the resolver can resolve the expression.
        /// </summary>
        /// <param name="targetProperty">The target property we're mapping to.</param>
        /// <param name="expression">Expression to resolve</param>
        /// <returns>True if it can resolve, false if not.</returns>
        bool CanResolve(Property targetProperty, string expression);

        /// <summary>
        /// Called to resolve the expression.
        /// </summary>
        /// <param name="targetProperty">The target property we're mapping to.</param>
        /// <param name="expression">Expression to resolve</param>
        /// <returns><see cref="PropertyMapper{Event}"/> it resolves to.</returns>
        PropertyMapper<Event> Resolve(Property targetProperty, string expression);
    }
}
