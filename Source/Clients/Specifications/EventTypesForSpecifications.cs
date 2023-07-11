// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypesForSpecifications : IEventTypes
{
    readonly Dictionary<Type, EventType> _eventTypes = new();
    readonly Dictionary<EventTypeId, Type> _clrTypesByEventType = new();

    /// <inheritdoc/>
    public IEnumerable<EventType> All => _eventTypes.Values;

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _clrTypesByEventType[eventTypeId];

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType)
    {
        EnsureEventType(clrType);
        return _eventTypes[clrType];
    }

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _clrTypesByEventType.ContainsKey(eventTypeId);

    /// <inheritdoc/>
    public bool HasFor(Type clrType)
    {
        EnsureEventType(clrType);
        return _eventTypes.ContainsKey(clrType);
    }

    void EnsureEventType(Type clrType)
    {
        if (!_eventTypes.ContainsKey(clrType))
        {
            var eventTypeAttribute = clrType.GetCustomAttribute<EventTypeAttribute>();
            if (eventTypeAttribute is not null)
            {
                _eventTypes[clrType] = eventTypeAttribute.Type;
                _clrTypesByEventType[eventTypeAttribute.Type.Id] = clrType;
            }
        }
    }
}
