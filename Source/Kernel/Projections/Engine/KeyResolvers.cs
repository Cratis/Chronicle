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
        /// <param name="projection"><see cref="IProjection"/> to start at.</param>
        /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
        /// <param name="childrenKey">Key that identifies the child from the event.</param>
        /// <returns>A new <see cref="KeyResolver"/>.</returns>
        public static KeyResolver FromEventSourceId(IProjection projection, PropertyPath identifiedByProperty, PropertyPath? childrenKey = default)
        {
            return (IProjectionEventProvider eventProvider, AppendedEvent @event) =>
            {
                if (childrenKey is null)
                {
                    return Task.FromResult(new Key(EventValueProviders.FromEventSourceId(@event), ArrayIndexers.NoIndexers))!;
                }

                var key = EventValueProviders.FromEventSourceId(@event);
                var childKey = EventValueProviders.FromEventContent(childrenKey)(@event);
                var arrayIndexers = new List<ArrayIndexer>
                {
                    new(projection.ChildrenPropertyPath, identifiedByProperty, childKey)
                };
                return Task.FromResult(new Key(key, new ArrayIndexers(arrayIndexers)))!;
            };
        }

        /// <summary>
        /// Create a <see cref="KeyResolver"/> that provides a value from the event content.
        /// </summary>
        /// <param name="sourceProperty">Source property.</param>
        /// <returns>A new <see cref="KeyResolver"/>.</returns>
        public static KeyResolver FromEventContent(PropertyPath sourceProperty) => (IProjectionEventProvider _, AppendedEvent @event) => Task.FromResult(new Key(EventValueProviders.FromEventContent(sourceProperty)(@event), ArrayIndexers.NoIndexers));

        /// <summary>
        /// Create a <see cref="KeyResolver"/> that provides a key value hierarchically upwards in Child->Parent relationships.
        /// </summary>
        /// <param name="projection"><see cref="IProjection"/> to start at.</param>
        /// <param name="sourceProperty">The property that represents the parent key.</param>
        /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
        /// <returns>A new <see cref="KeyResolver"/>.</returns>
        public static KeyResolver FromParentHierarchy(IProjection projection, PropertyPath sourceProperty, PropertyPath identifiedByProperty)
        {
            return async (IProjectionEventProvider eventProvider, AppendedEvent @event) =>
            {
                var arrayIndexers = new List<ArrayIndexer>();
                var parentKey = EventValueProviders.FromEventContent(sourceProperty)(@event);
                var currentProjection = projection;
                while (currentProjection.HasParent)
                {
                    currentProjection = currentProjection.Parent;
                    if (currentProjection?.HasParent != true)
                    {
                        break;
                    }

                    if (!currentProjection.ChildrenPropertyPath.IsRoot)
                    {
                        arrayIndexers.Add(new(currentProjection.ChildrenPropertyPath, sourceProperty, parentKey));
                    }
                    var firstEvent = currentProjection.EventTypes.First()!;
                    var parentEvent = await eventProvider.GetLastInstanceFor(firstEvent.Id, parentKey.ToString()!);
                    var keyResolver = currentProjection.GetKeyResolverFor(firstEvent);
                    var resolvedParentKey = await keyResolver(eventProvider, parentEvent);
                    parentKey = resolvedParentKey.Value;
                }

                arrayIndexers.Add(new(projection.ChildrenPropertyPath, identifiedByProperty, EventValueProviders.FromEventSourceId(@event)));

                return new(parentKey, new ArrayIndexers(arrayIndexers));
            };
        }
    }
}
