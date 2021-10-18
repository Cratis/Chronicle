// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Defines a resolver of string representations of <see cref="EventValueProvider"/>.
    /// </summary>
    public interface IEventValueProviderExpressionResolver
    {
        /// <summary>
        /// Called to verify if the resolver can resolve the expression.
        /// </summary>
        /// <param name="expression">Expression to resolve</param>
        /// <returns>True if it can resolve, false if not.</returns>
        bool CanResolve(string expression);

        /// <summary>
        /// Called to resolve the expression.
        /// </summary>
        /// <param name="expression">Expression to resolve</param>
        /// <returns><see cref="EventValueProvider"/> it resolves to.</returns>
        EventValueProvider Resolve(string expression);
    }
}
