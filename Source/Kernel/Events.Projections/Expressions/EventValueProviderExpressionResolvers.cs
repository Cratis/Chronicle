// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventValueProviderExpressionResolvers"/>.
    /// </summary>
    public class EventValueProviderExpressionResolvers : IEventValueProviderExpressionResolvers
    {
        readonly IEventValueProviderExpressionResolver[] _resolvers = new[]
        {
            new EventSourceIdExpressionResolver()
        };

        /// <inheritdoc/>
        public bool CanResolve(string expression) => _resolvers.Any(_ => _.CanResolve(expression));

        /// <inheritdoc/>
        public EventValueProvider Resolve(string expression)
        {
            var resolver = Array.Find(_resolvers, _ => _.CanResolve(expression));
            ThrowIfUnsupportedEventValueExpression(expression, resolver);
            return resolver!.Resolve(expression);
        }

        void ThrowIfUnsupportedEventValueExpression(string expression, IEventValueProviderExpressionResolver? resolver)
        {
            if (resolver == default)
            {
                throw new UnsupportedEventValueExpression(expression);
            }
        }
    }
}
