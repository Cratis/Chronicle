// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypes : IEventTypes
{
    readonly IDictionary<EventType, Type> _typesByEventType;

    /// <summary>
    /// /// Initializes a new instance of <see cref="EventTypes"/>.
    /// </summary>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    public EventTypes(IClientArtifactsProvider clientArtifacts)
    {
        _typesByEventType = clientArtifacts.EventTypes
                        .ToDictionary(_ => _.GetEventType(), _ => _);

        All = _typesByEventType.Keys.ToArray();
    }

    /// <inheritdoc/>
    public IEnumerable<EventType> All { get; }

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _typesByEventType.Any(_ => _.Key.Id == eventTypeId);

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType) => _typesByEventType.Single(_ => _.Value == clrType).Key;

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => _typesByEventType.Any(_ => _.Value == clrType);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _typesByEventType.Single(_ => _.Key.Id == eventTypeId).Value;
}
