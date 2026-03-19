// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.Migrations;
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
    readonly IEventTypeMigrators _eventTypeMigrators;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly bool _enableEventTypeGenerationValidation;

    /// <summary>
    /// Initializes a new instance of <see cref="EventTypes"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> the event types belong to.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas from types.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="eventTypeMigrators"><see cref="IEventTypeMigrators"/> for discovering event type migrators.</param>
    /// <param name="enableEventTypeGenerationValidation">Whether to enable event type generation chain validation on the server. Defaults to <see langword="false"/>.</param>
    public EventTypes(
        IEventStore eventStore,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IClientArtifactsProvider clientArtifacts,
        IEventTypeMigrators eventTypeMigrators,
        bool enableEventTypeGenerationValidation = false)
    {
        _eventStore = eventStore;
        _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _clientArtifacts = clientArtifacts;
        _eventTypeMigrators = eventTypeMigrators;
        _enableEventTypeGenerationValidation = enableEventTypeGenerationValidation;
    }

    /// <inheritdoc/>
    public IImmutableList<Type> AllClrTypes => _typesByEventType.Values.ToImmutableList();

    /// <inheritdoc/>
    public IImmutableList<EventType> All => _typesByEventType.Keys.ToImmutableList();

    /// <inheritdoc/>
    public Task Discover()
    {
        var eventTypes = _clientArtifacts.EventTypes.Select(_ => new
        {
            ClrType = _,
            EventType = _.GetEventType()
        }).ToArray();

        var duplicateEventTypes = eventTypes.GroupBy(_ => _.EventType).Where(_ => _.Count() > 1).ToArray();
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
        var registrations = new List<EventTypeRegistration>();

        // Group all CLR types by event type ID so that all generations of the same event
        // type are registered as a single registration with multiple generation schemas.
        foreach (var group in _typesByEventType.GroupBy(_ => _.Key.Id))
        {
            var latestEntry = group.OrderByDescending(_ => _.Key.Generation.Value).First();
            var latestEventType = latestEntry.Key;
            var latestSchema = _schemasByEventType[latestEventType];

            var registration = new EventTypeRegistration
            {
                Type = latestEventType.ToContract(),
                Schema = latestSchema.ToJson()
            };

            foreach (var (eventType, clrType) in group)
            {
                var schema = _schemasByEventType[eventType];
                var migrators = _eventTypeMigrators.GetMigratorsFor(clrType).ToList();

                // Add generation definition for this CLR type
                if (!registration.Generations.Any(g => g.Generation == eventType.Generation.Value))
                {
                    registration.Generations.Add(new EventTypeGenerationDefinition
                    {
                        Generation = eventType.Generation,
                        Schema = schema.ToJson()
                    });
                }

                // Add migration definitions from discovered migrators
                foreach (var migrator in migrators)
                {
                    var upcastBuilder = new EventMigrationBuilder();
                    migrator.Upcast(upcastBuilder);

                    var downcastBuilder = new EventMigrationBuilder();
                    migrator.Downcast(downcastBuilder);

                    registration.Migrations.Add(new EventTypeMigrationDefinition
                    {
                        FromGeneration = migrator.From,
                        ToGeneration = migrator.To,
                        UpcastJmesPath = upcastBuilder.ToJson().ToJsonString(),
                        DowncastJmesPath = downcastBuilder.ToJson().ToJsonString()
                    });

                    // Ensure both from and to generation schemas are registered so the kernel
                    // can store all generations. If a generation schema is not explicitly known
                    // (e.g. a previous generation schema), use an empty schema.
                    if (!registration.Generations.Any(g => g.Generation == migrator.From.Value))
                    {
                        registration.Generations.Add(new EventTypeGenerationDefinition
                        {
                            Generation = migrator.From,
                            Schema = "{}"
                        });
                    }

                    if (!registration.Generations.Any(g => g.Generation == migrator.To.Value))
                    {
                        registration.Generations.Add(new EventTypeGenerationDefinition
                        {
                            Generation = migrator.To,
                            Schema = "{}"
                        });
                    }
                }
            }

            registrations.Add(registration);
        }

        await _servicesAccessor.Services.EventTypes.Register(new()
        {
            EventStore = _eventStore.Name,
            Types = registrations,
            DisableValidation = !_enableEventTypeGenerationValidation
        });
    }

    /// <inheritdoc/>
    public bool HasFor(EventTypeId eventTypeId) => _typesByEventType.Any(_ => _.Key.Id == eventTypeId);

    /// <inheritdoc/>
    public EventType GetEventTypeFor(Type clrType) => _typesByEventType.Single(_ => _.Value == clrType).Key;

    /// <inheritdoc/>
    public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => _schemasByEventType
        .Where(_ => _.Key.Id == eventTypeId)
        .OrderByDescending(_ => _.Key.Generation.Value)
        .First().Value;

    /// <inheritdoc/>
    public bool HasFor(Type clrType) => _typesByEventType.Any(_ => _.Value == clrType);

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId) => _typesByEventType
        .Where(_ => _.Key.Id == eventTypeId)
        .OrderByDescending(_ => _.Key.Generation.Value)
        .First().Value;

    /// <inheritdoc/>
    public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => _typesByEventType
        .Where(_ => _.Key.Id == eventTypeId && _.Key.Generation == generation)
        .Select(_ => _.Value)
        .FirstOrDefault() ?? GetClrTypeFor(eventTypeId);
}
