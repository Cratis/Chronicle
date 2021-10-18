// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Represents a <see cref="IEventValueProviderExpressionResolver"/> for resolving value from a property on the content of an <see cref="Event"/>.
    /// </summary>
    public class PropertyOnEventContentExpressionProvider : IEventValueProviderExpressionResolver
    {
        /// <inheritdoc/>
        public bool CanResolve(string expression) => !expression.StartsWith("$", StringComparison.InvariantCultureIgnoreCase);

        /// <inheritdoc/>
        public EventValueProvider Resolve(string expression) => EventValueProviders.FromEventContent(expression);
    }
}
