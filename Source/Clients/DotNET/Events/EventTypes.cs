// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

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
        var eventTypes = clientArtifacts.EventTypes.Select(_ => new
        {
            ClrType = _,
            EventType = _.GetEventType()
        }).ToArray();

        var duplicateEventTypes = eventTypes.GroupBy(_ => _.EventType.Id).Where(_ => _.Count() > 1).ToArray();
        if (duplicateEventTypes.Length > 0)
        {
            var clrTypes = duplicateEventTypes.SelectMany(_ => _).Select(_ => _.ClrType).ToArray();
            throw new MultipleEventTypesWithSameIdFound(clrTypes);
        }

        _typesByEventType = eventTypes.ToDictionary(_ => _.EventType, _ => _.ClrType);
        All = eventTypes.Select(_ => _.EventType).ToImmutableList();

        AllClrTypes = _typesByEventType.Values.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<EventType> All { get; }

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes { get; }

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _typesByEventType.Any(_ => _.Key.Id == eventTypeId);

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType) => _typesByEventType.Single(_ => _.Value == clrType).Key;

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => _typesByEventType.Any(_ => _.Value == clrType);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _typesByEventType.Single(_ => _.Key.Id == eventTypeId).Value;
}
