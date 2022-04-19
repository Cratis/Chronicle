// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents utilities for creating <see cref="KeyResolvers"/> instances for providing values from <see cref="AppendedEvent">events</see>.
/// </summary>
public static class KeyResolvers
{
    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides the event source id from an event.
    /// </summary>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public static readonly KeyResolver FromEventSourceId = (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) => Task.FromResult(new Key(EventValueProviders.FromEventSourceId(@event), ArrayIndexers.NoIndexers))!;

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a value from the event content.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to start at.</param>
    /// <param name="keyProperty">Property that holds the key on the event.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public static KeyResolver FromEventContent(IProjection projection, PropertyPath keyProperty, PropertyPath identifiedByProperty)
    {
        return (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) =>
        {
            var key = EventValueProviders.FromEventContent(keyProperty)(@event);
            if (!projection.HasParent)
            {
                return Task.FromResult(new Key(key, ArrayIndexers.NoIndexers))!;
            }

            var arrayIndexers = new List<ArrayIndexer>();
            var parentKey = EventValueProviders.FromEventSourceId(@event);
            arrayIndexers.Add(new(projection.ChildrenPropertyPath, identifiedByProperty, key));
            return Task.FromResult(new Key(parentKey, new ArrayIndexers(arrayIndexers)))!;
        };
    }

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value hierarchically upwards in Child->Parent relationships.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to start at.</param>
    /// <param name="parentKeyProperty">The property that represents the parent key.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public static KeyResolver FromParentHierarchy(IProjection projection, PropertyPath parentKeyProperty, PropertyPath identifiedByProperty)
    {
        return async (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) =>
        {
            var arrayIndexers = new List<ArrayIndexer>();
            var parentKey = EventValueProviders.FromEventContent(parentKeyProperty)(@event);
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
                    arrayIndexers.Add(new(currentProjection.ChildrenPropertyPath, parentKeyProperty, parentKey));
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
