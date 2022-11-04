// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Dynamic;
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
    public static readonly KeyResolver FromEventSourceId = (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) => Task.FromResult(new Key(EventValueProviders.EventSourceId(@event), ArrayIndexers.NoIndexers))!;

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a value from the event content.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to start at.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <param name="eventValueProvider">The actual <see cref="ValueProvider{T}"/> for resolving key.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public static KeyResolver FromEventValueProvider(IProjection projection, PropertyPath identifiedByProperty, ValueProvider<AppendedEvent> eventValueProvider)
    {
        return (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) =>
        {
            var key = eventValueProvider(@event);
            if (!projection.HasParent)
            {
                return Task.FromResult(new Key(key, ArrayIndexers.NoIndexers))!;
            }

            var arrayIndexers = new List<ArrayIndexer>();
            var parentKey = EventValueProviders.EventSourceId(@event);
            arrayIndexers.Add(new(projection.ChildrenPropertyPath, identifiedByProperty, key));
            return Task.FromResult(new Key(parentKey, new ArrayIndexers(arrayIndexers)))!;
        };
    }

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value which is a composite of a set of <see cref="ValueProvider{T}"/>.
    /// </summary>
    /// <param name="propertiesWithKeyValueProviders">Target property paths in key and resolvers to use for resolving.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public static KeyResolver Composite(IDictionary<PropertyPath, ValueProvider<AppendedEvent>> propertiesWithKeyValueProviders)
    {
        return (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) =>
        {
            var key = new ExpandoObject();
            foreach (var keyValue in propertiesWithKeyValueProviders)
            {
                var actualTarget = key.EnsurePath(keyValue.Key, ArrayIndexers.NoIndexers) as IDictionary<string, object>;
                actualTarget[keyValue.Key.LastSegment.Value] = keyValue.Value(@event);
            }
            return Task.FromResult(new Key(key, ArrayIndexers.NoIndexers));
        };
    }

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value hierarchically upwards in Child->Parent relationships.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to start at.</param>
    /// <param name="thisLevelKeyResolver">see cref=KeyResolver"/> to use for resolving the key for the incoming event.</param>
    /// <param name="parentKeyProperty">The property that represents the parent key.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public static KeyResolver FromParentHierarchy(IProjection projection, KeyResolver thisLevelKeyResolver, PropertyPath parentKeyProperty, PropertyPath identifiedByProperty)
    {
        return async (IEventSequenceStorageProvider eventProvider, AppendedEvent @event) =>
        {
            var arrayIndexers = new List<ArrayIndexer>();
            var parentKey = EventValueProviders.EventContent(parentKeyProperty)(@event);
            var currentProjection = projection;
            if (currentProjection.HasParent)
            {
                arrayIndexers.Add(new(projection.ChildrenPropertyPath, identifiedByProperty, thisLevelKeyResolver(eventProvider, @event)));
                currentProjection = currentProjection.Parent!;
                var parentEvent = await eventProvider.GetLastInstanceOfAny(EventSequenceId.Log, parentKey.ToString()!, currentProjection.EventTypes.Select(_ => _.Id));
                var eventType = currentProjection.EventTypes.First(_ => _.Id == parentEvent.Metadata.Type.Id);
                var keyResolver = currentProjection.GetKeyResolverFor(eventType);
                var resolvedParentKey = await keyResolver(eventProvider, parentEvent);
                parentKey = resolvedParentKey.Value;
                arrayIndexers.AddRange(resolvedParentKey.ArrayIndexers.All);
            }

            return new(parentKey, new ArrayIndexers(arrayIndexers));
        };
    }
}
