// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Specifications;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypesForSpecifications : IEventTypes
{
    readonly Dictionary<Type, EventType> _eventTypes = [];
    readonly Dictionary<EventTypeId, Type> _clrTypesByEventType = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypesForSpecifications"/> class.
    /// </summary>
    /// <param name="types"><see cref="IEnumerable{T}"/> of <see cref="Type"/> representing the event types.</param>
    public EventTypesForSpecifications(IEnumerable<Type>? types = null)
    {
        types ??= Enumerable.Empty<Type>();

        _eventTypes = types.ToDictionary(_ => _, _ => _.GetEventType());
        _clrTypesByEventType = _eventTypes.ToDictionary(_ => _.Value.Id, _ => _.Key);

        AllClrTypes = _clrTypesByEventType.Values.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes { get; }

    /// <inheritdoc/>
    public Task Discover() => throw new NotImplementedException();

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

    /// <inheritdoc/>
    public Task Register() => throw new NotImplementedException();

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
