// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IKeyResolvers"/>.
/// </summary>
/// <param name="logger">The logger.</param>
public class KeyResolvers(ILogger<KeyResolvers> logger) : IKeyResolvers
{
    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides the event source id from an event.
    /// </summary>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver FromEventSourceId =>
        CreateKeyResolver(nameof(FromEventSourceId), (_, @event) =>
            Task.FromResult(new Key(EventValueProviders.EventSourceId(@event), ArrayIndexers.NoIndexers)));

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a value from the event content.
    /// </summary>
    /// <param name="eventValueProvider">The actual <see cref="ValueProvider{T}"/> for resolving key.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver FromEventValueProvider(ValueProvider<AppendedEvent> eventValueProvider) =>
        CreateKeyResolver(nameof(FromEventValueProvider), (_, @event) =>
        {
            var key = eventValueProvider(@event);
            return Task.FromResult(new Key(key, ArrayIndexers.NoIndexers))!;
        });

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value which is a composite of a set of <see cref="ValueProvider{T}"/>.
    /// </summary>
    /// <param name="propertiesWithKeyValueProviders">Target property paths in key and resolvers to use for resolving.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver Composite(IDictionary<PropertyPath, ValueProvider<AppendedEvent>> propertiesWithKeyValueProviders) =>
        CreateKeyResolver(nameof(Composite), (_, @event) =>
        {
            var key = new ExpandoObject();
            foreach (var keyValue in propertiesWithKeyValueProviders)
            {
                var actualTarget =
                    key.EnsurePath(keyValue.Key, ArrayIndexers.NoIndexers) as IDictionary<string, object>;
                actualTarget![keyValue.Key.LastSegment.Value] = keyValue.Value(@event);
            }

            return Task.FromResult(new Key(key, ArrayIndexers.NoIndexers));
        });

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value for a join relationship.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> the join is for.</param>
    /// <param name="keyResolver"><see cref="KeyResolver"/> for resolving the key from the event.</param>
    /// <param name="identifiedByProperty">The <see cref="PropertyPath"/> for the identified by property in the join relationship.</param>
    /// <returns><see cref="KeyResolver"/> that will be used to resolve.</returns>
    public KeyResolver ForJoin(IProjection projection, KeyResolver keyResolver, PropertyPath identifiedByProperty) =>
        CreateKeyResolver(nameof(ForJoin), async (eventSequenceStorage, @event) =>
        {
            var key = await keyResolver(eventSequenceStorage, @event);
            if (!projection.HasParent)
            {
                return key with { ArrayIndexers = ArrayIndexers.NoIndexers };
            }

            var arrayIndexers = new List<ArrayIndexer>
            {
                new(projection.ChildrenPropertyPath, identifiedByProperty, key.Value!)
            };

            return key with { ArrayIndexers = new ArrayIndexers(arrayIndexers) };
        });

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value hierarchically upwards in Child->Parent relationships.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to start at.</param>
    /// <param name="keyResolver"><see cref="KeyResolver"/> to use for resolving the key for the incoming event.</param>
    /// <param name="parentKeyResolver">The property that represents the parent key.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver FromParentHierarchy(IProjection projection, KeyResolver keyResolver, KeyResolver parentKeyResolver, PropertyPath identifiedByProperty) =>
        CreateKeyResolver(nameof(FromParentHierarchy), async (eventSequenceStorage, @event) =>
            {
                var parentKey = await parentKeyResolver(eventSequenceStorage, @event);
                if (!projection.HasParent)
                {
                    return parentKey with { ArrayIndexers = ArrayIndexers.NoIndexers };
                }

                var arrayIndexers = new List<ArrayIndexer>();

                var key = await keyResolver(eventSequenceStorage, @event);
                arrayIndexers.Add(new ArrayIndexer(projection.ChildrenPropertyPath, identifiedByProperty, key.Value));
                var parentProjection = projection.Parent!;
                var parentEventTypeIds = parentProjection.OwnEventTypes.Select(_ => _.Id).ToArray();
                if (parentEventTypeIds.Length == 0)
                {
                    return parentKey with { ArrayIndexers = new ArrayIndexers(arrayIndexers) };
                }

                AppendedEvent parentEvent;
                if (parentEventTypeIds.Any(id => id == @event.Metadata.Type.Id))
                {
                    parentEvent = @event;
                }
                else
                {
                    var optionalEvent = await eventSequenceStorage.TryGetLastInstanceOfAny(parentKey.Value.ToString()!, parentEventTypeIds);
                    parentEvent = optionalEvent.AsT0;
                }

                var eventType =
                    parentProjection.EventTypes.First(eventType => eventType.Id == parentEvent.Metadata.Type.Id);
                var keyResolverForEventType = parentProjection.GetKeyResolverFor(eventType);
                var resolvedParentKey = await keyResolverForEventType(eventSequenceStorage, parentEvent);
                parentKey = resolvedParentKey;
                arrayIndexers.AddRange(resolvedParentKey.ArrayIndexers.All);

                return parentKey with { ArrayIndexers = new ArrayIndexers(arrayIndexers) };
            });

    KeyResolver CreateKeyResolver(string keyResolverName, KeyResolver keyResolver) => async (eventSequenceStorage, @event) =>
    {
        try
        {
            logger.ResolvingKey(keyResolverName);
            return await keyResolver(eventSequenceStorage, @event);
        }
        catch (Exception ex)
        {
            logger.ErrorResolving(ex, keyResolverName);
            throw;
        }
    };
}
