// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Represents a <see cref="IEventValueProviderExpressionResolver"/> for resolving value from <see cref="EventSourceId"/> of the <see cref="Event"/>.
    /// </summary>
    public class EventSourceIdExpressionResolver : IEventValueProviderExpressionResolver
    {
        /// <inheritdoc/>
        public bool CanResolve(string expression) => expression == "$eventSourceId";

        /// <inheritdoc/>
        public EventValueProvider Resolve(string expression) => EventValueProviders.FromEventSourceId();
    }
}
