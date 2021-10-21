// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Represents an implementation of <see cref="IPropertyMapperExpressionResolvers"/>.
    /// </summary>
    public class EventValueProviderExpressionResolvers : IPropertyMapperExpressionResolvers
    {
        readonly IPropertyMapperExpressionResolver[] _resolvers = new[]
        {
            new EventSourceIdExpressionResolver()
        };

        /// <inheritdoc/>
        public bool CanResolve(string targetPath, string expression) => _resolvers.Any(_ => _.CanResolve(targetPath, expression));

        /// <inheritdoc/>
        public PropertyMapper Resolve(string targetPath, string expression)
        {
            var resolver = Array.Find(_resolvers, _ => _.CanResolve(targetPath, expression));
            ThrowIfUnsupportedEventValueExpression(expression, resolver);
            return resolver!.Resolve(targetPath, expression);
        }

        void ThrowIfUnsupportedEventValueExpression(string expression, IPropertyMapperExpressionResolver? resolver)
        {
            if (resolver == default)
            {
                throw new UnsupportedPropertyMapperExpression(expression);
            }
        }
    }
}
