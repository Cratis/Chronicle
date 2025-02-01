// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypes : IEventTypes
{
    readonly Dictionary<EventType, Type> _typesByEventType = [];
    readonly Dictionary<EventType, JsonSchema> _schemasByEventType = [];
    readonly IEventStore _eventStore;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IChronicleServicesAccessor _servicesAccessor;

    /// <summary>
    /// /// Initializes a new instance of <see cref="EventTypes"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> the event types belong to.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas from types.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    public EventTypes(
        IEventStore eventStore,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IClientArtifactsProvider clientArtifacts)
    {
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _clientArtifacts = clientArtifacts;
        eventStore.Connection.Lifecycle.OnConnected += Register;
    }

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes => _typesByEventType.Values.ToImmutableList();

    /// <inheritdoc/>
    public Task Discover()
    {
        var eventTypes = _clientArtifacts.EventTypes.Select(_ => new
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

        foreach (var eventType in eventTypes)
        {
            _typesByEventType[eventType.EventType] = eventType.ClrType;
            _schemasByEventType[eventType.EventType] = _jsonSchemaGenerator.Generate(eventType.ClrType);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        var registrations = _typesByEventType.Select(_ => new EventTypeRegistration
        {
            Type = _.Key.ToContract(),
            Schema = _schemasByEventType[_.Key].ToJson()
        }).ToList();

        await _servicesAccessor.Services.EventTypes.Register(new()
        {
            EventStore = _eventStore.Name,
            Types = registrations
        });
    }

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _typesByEventType.Any(_ => _.Key.Id == eventTypeId);

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType) => _typesByEventType.Single(_ => _.Value == clrType).Key;

    /// <inheritdoc/>
    public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => _schemasByEventType.Single(_ => _.Key.Id == eventTypeId).Value;

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => _typesByEventType.Any(_ => _.Value == clrType);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _typesByEventType.Single(_ => _.Key.Id == eventTypeId).Value;
}
