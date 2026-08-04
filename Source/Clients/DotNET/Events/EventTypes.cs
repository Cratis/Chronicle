// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypes : IEventTypes
{
    readonly IEventStore _eventStore;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IEventTypeMigrators _eventTypeMigrators;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly bool _enableEventTypeGenerationValidation;
    FrozenDictionary<EventType, Type> _typesByEventType = FrozenDictionary<EventType, Type>.Empty;
    FrozenDictionary<EventType, JsonSchema> _schemasByEventType = FrozenDictionary<EventType, JsonSchema>.Empty;

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

        _typesByEventType = eventTypes.ToFrozenDictionary(_ => _.EventType, _ => _.ClrType);
        _schemasByEventType = eventTypes.ToFrozenDictionary(_ => _.EventType, _ => _jsonSchemaGenerator.Generate(_.ClrType));

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
            var latestClrType = latestEntry.Value;
            var latestSchema = _schemasByEventType[latestEventType];
            var eventStoreAttribute = latestClrType.GetCustomAttribute<EventStoreAttribute>();

            var registration = new EventTypeRegistration
            {
                Type = latestEventType.ToContract(),
                Schema = latestSchema.ToJson(),
                EventStore = eventStoreAttribute?.EventStore ?? string.Empty
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
    /// <exception cref="TypeIsNotAnEventType">Thrown when the type is not a registered event type.</exception>
    /// <remarks>
    /// Reporting the miss as <see cref="TypeIsNotAnEventType"/> rather than letting the lookup fall out as a LINQ
    /// exception matters most to the model-bound projection attributes: their generic type argument is
    /// unconstrained, so nothing at compile time relates it to an event type and this is the first place the
    /// mistake can be named. The fluent projection builder already answers the same condition the same way.
    /// </remarks>
    public EventType GetEventTypeFor(Type clrType)
    {
        foreach (var (eventType, type) in _typesByEventType)
        {
            if (type == clrType)
            {
                return eventType;
            }
        }

        throw new TypeIsNotAnEventType(clrType);
    }

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
