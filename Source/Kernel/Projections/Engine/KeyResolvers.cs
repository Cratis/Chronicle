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

        /// <summary>
        /// Create a <see cref="KeyResolver"/> that provides a key value hierarchically upwards in Child->Parent relationships.
        /// </summary>
        /// <param name="projection"><see cref="IProjection"/> to start at.</param>
        /// <param name="sourceProperty">The property that represents the parent key.</param>
        /// <returns>A new <see cref="KeyResolver"/>.</returns>
        public static KeyResolver FromParentHierarchy(IProjection projection, PropertyPath sourceProperty)
        {
            return (IProjectionEventProvider eventProvider, AppendedEvent @event) =>
            {
                var parentKey = EventValueProviders.FromEventContent(sourceProperty)(@event);
                var currentProjection = projection;
                while (currentProjection.HasParent)
                {
                    currentProjection = currentProjection.Parent;
                    if (currentProjection?.HasParent != true)
                    {
                        break;
                    }

                    var firstEvent = currentProjection.EventTypes.First()!;
                    var task = eventProvider.GetLastInstanceFor(firstEvent.Id, parentKey.ToString()!);
                    task.Wait();
                    var parentEvent = task.Result;
                    var keyResolver = currentProjection.GetKeyResolverFor(firstEvent);
                    parentKey = keyResolver(eventProvider, parentEvent);
                }

                return parentKey;
            };
        }
    }
}
