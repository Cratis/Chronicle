// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Defines a resolver of expressions for providing values from events.
    /// </summary>
    public interface IEventValueProviderExpressionResolver
    {
        /// <summary>
        /// Called to check if the resolver can resolve the expression.
        /// </summary>
        /// <param name="expression">Expression to check.</param>
        /// <returns>True if it can resolve, false if not.</returns>
        bool CanResolve(string expression);

        /// <summary>
        /// Called to resolve the expression.
        /// </summary>
        /// <param name="expression">Expression to resolve.</param>
        /// <returns><see cref="ValueProvider{Event}"/> it resolves to.</returns>
        ValueProvider<AppendedEvent> Resolve(string expression);
    }
}
