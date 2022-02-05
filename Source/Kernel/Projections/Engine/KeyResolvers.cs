// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents utilities for creating <see cref="KeyResolvers"/> instances for providing values from <see cref="AppendedEvent">events</see>.
    /// </summary>
    public static class KeyResolvers
    {
        /// <summary>
        /// Create a <see cref="KeyResolver"/> that provides the event source id from an event.
        /// </summary>
        /// <returns>A new <see cref="KeyResolver"/>.</returns>
        public static readonly KeyResolver FromEventSourceId = (IProjectionEventProvider _, AppendedEvent @event) => EventValueProviders.FromEventSourceId(@event);

        /// <summary>
        /// Create a <see cref="KeyResolver"/> that provides a value from the event content.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <returns>A new <see cref="KeyResolver"/>.</returns>
        public static KeyResolver FromEventContent(PropertyPath sourceProperty) => (IProjectionEventProvider _, AppendedEvent @event) => EventValueProviders.FromEventContent(sourceProperty)(@event);
    }
}
