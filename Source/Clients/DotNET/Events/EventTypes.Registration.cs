// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Schemas;

using EventTypeRegistrationContract = Cratis.Chronicle.Contracts.Events.EventTypeRegistration;

namespace Cratis.Chronicle.Events;

public partial class EventTypes
{
    /// <summary>
    /// Gets the migrators prepared with this registry's owning-store binding and cache.
    /// </summary>
    internal IEventTypeMigrators PreparedMigrators => _storeMigrators ?? _eventTypeMigrators;

    IClientArtifactsProvider CurrentArtifacts => _clientArtifacts is StoreClientArtifactsProvider store ? store.Current : _clientArtifacts;

    /// <summary>
    /// Supplies ordinary constructor dependencies for the custom-provider explicit migration overlay.
    /// </summary>
    /// <param name="serviceProvider">The owning store's constructor dependency provider.</param>
    internal void UseMigrationServices(IServiceProvider serviceProvider) => _migrationServices = serviceProvider;

    /// <summary>
    /// Validates the local schema verdict without performing discovery or applying absent options recipes.
    /// </summary>
    /// <returns>The latest local schema verdict.</returns>
    internal Task ValidatePreparation() => _schemaVerdict;

    /// <summary>
    /// Builds the complete event payload before any store registration effects.
    /// </summary>
    /// <returns>The prepared wire payload.</returns>
    internal async Task<RegisterEventTypesRequest> PrepareRegistration()
    {
        await _schemaVerdict;
        _payloadFailure?.Throw();
        try
        {
            return BuildRegistrationPayload();
        }
        catch (Exception exception)
        {
            _payloadFailure = ExceptionDispatchInfo.Capture(exception);
            throw;
        }
    }

    /// <summary>
    /// Sends a payload prepared under the store's successful revision lease.
    /// </summary>
    /// <param name="payload">The complete event registration payload.</param>
    /// <returns>The wire registration operation.</returns>
    internal Task RegisterPrepared(RegisterEventTypesRequest payload) => _servicesAccessor.Services.EventTypes.RegisterEventTypes(payload).EnsureSuccess();

    /// <summary>
    /// Captures what discovery produced so another registry over an equal catalog can adopt it.
    /// </summary>
    /// <returns>The frozen lookups, which are safe to share, and independent copies of the schemas.</returns>
    internal EventTypesDiscovery CaptureDiscovery() => new(
        _descriptors,
        _typesByEventType,
        _schemasByEventType.ToFrozenDictionary(_ => _.Key, _ => CloneSchema(_.Value)));

    /// <summary>
    /// Adopts a discovery captured from a registry over an equal catalog instead of discovering again.
    /// </summary>
    /// <param name="discovery">The captured discovery.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArtifactPreparationReentered">Adoption reenters unfinished local preparation.</exception>
    internal Task AdoptDiscovery(EventTypesDiscovery discovery) => RunLocalPreparation(() => AdoptLocal(discovery));

    static JsonSchema CloneSchema(JsonSchema schema) => new(schema.Node.DeepClone().AsObject());

    void AdoptLocal(EventTypesDiscovery discovery)
    {
        _descriptors = discovery.Descriptors;
        _typesByEventType = discovery.TypesByEventType;
        _storeMigrators = StoreEventTypeMigrators.Bind(_eventTypeMigrators, CurrentArtifacts, this, _migrationServices, _namingPolicy);
        _schemasByEventType = discovery.Schemas.ToFrozenDictionary(_ => _.Key, _ => CloneSchema(_.Value));
    }

    void DiscoverLocal()
    {
        var artifacts = CurrentArtifacts;
        var eventTypes = artifacts.EventTypes.Distinct().Select(type => artifacts is ExplicitArtifactCatalog catalog
            ? catalog.GetEventDescriptorFor(type, _eventStore.Name)
            : EventTypeDescriptor.FromDeclaration(type, _eventStore.Name)).ToArray();
        var duplicate = eventTypes
            .GroupBy(_ => _.EventType)
            .Where(_ => _.Count() > 1)
            .OrderBy(_ => _.Key.Id.Value)
            .ThenBy(_ => _.Key.Generation.Value)
            .FirstOrDefault();
        if (duplicate is not null)
        {
            throw new MultipleEventTypesWithSameIdFound(duplicate.Key, duplicate.Select(_ => _.ClrType));
        }

        _descriptors = eventTypes.ToFrozenDictionary(_ => _.ClrType);
        _typesByEventType = eventTypes.ToFrozenDictionary(_ => _.EventType, _ => _.ClrType);
        _storeMigrators = StoreEventTypeMigrators.Bind(_eventTypeMigrators, artifacts, this, _migrationServices, _namingPolicy);
        _schemasByEventType = eventTypes.ToFrozenDictionary(_ => _.EventType, _ => _jsonSchemaGenerator.Generate(_.ClrType, this));
    }

    RegisterEventTypesRequest BuildRegistrationPayload()
    {
        var precise = _eventStore.Connection is IKernelCapabilities capabilities &&
            capabilities.Capabilities.Contains(KernelCapabilities.PreciseEventTypeSchemas);
        _schemasByEventType = _typesByEventType.ToFrozenDictionary(entry => entry.Key, entry => precise
            ? _jsonSchemaGenerator.Generate(entry.Value, this)
            : _jsonSchemaGenerator.GenerateLegacyEventType(entry.Value, this));
        var registrations = new List<EventTypeRegistrationContract>();

        // Group all CLR types by event type ID so that all generations of the same event
        // type are registered as a single registration with multiple generation schemas.
        foreach (var group in _typesByEventType.GroupBy(_ => _.Key.Id))
        {
            var orderedGroup = group.OrderBy(_ => _.Key.Generation.Value).ToList();
            var latestEntry = orderedGroup[^1];
            var latestEventType = latestEntry.Key;
            var latestClrType = latestEntry.Value;
            var latestSchema = _schemasByEventType[latestEventType];
            var descriptor = _descriptors[latestClrType];
            var registration = new EventTypeRegistrationContract
            {
                Type = latestEventType.ToContract(),
                Schema = latestSchema.ToJson(),
                EventStore = descriptor.EventStore ?? string.Empty,
                Visibility = descriptor.RegistrationVisibility
            };

            // Pass 1: register the real schema for every CLR type that represents a generation in
            // this group before any migrator gets a chance to backfill a placeholder for it. This
            // must be a distinct pass rather than interleaved with migrator processing below,
            // because the underlying dictionary's enumeration order is not generation-ascending and
            // is randomized per process - interleaving let a migrator's placeholder win the race
            // against a lower generation's real schema depending on enumeration order.
            foreach (var (eventType, _) in orderedGroup)
            {
                var schema = _schemasByEventType[eventType];
                registration.Generations.Add(new EventTypeGenerationDefinition
                {
                    Generation = eventType.Generation,
                    Schema = schema.ToJson()
                });
            }

            // Pass 2: add migration definitions, backfilling an empty placeholder schema only for a
            // generation that has no CLR type at all in this group (a genuine gap, kept for backward
            // compatibility) - never for one whose real schema was already added in pass 1.
            foreach (var (_, clrType) in orderedGroup)
            {
                foreach (var migrator in (_storeMigrators ?? _eventTypeMigrators).GetMigratorsFor(clrType))
                {
                    var upcastBuilder = new EventMigrationBuilder(_namingPolicy);
                    migrator.Upcast(upcastBuilder);
                    var downcastBuilder = new EventMigrationBuilder(_namingPolicy);
                    migrator.Downcast(downcastBuilder);
                    registration.Migrations.Add(new EventTypeMigrationDefinition
                    {
                        FromGeneration = migrator.From,
                        ToGeneration = migrator.To,
                        UpcastJmesPath = upcastBuilder.ToJson().ToJsonString(),
                        DowncastJmesPath = downcastBuilder.ToJson().ToJsonString()
                    });
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

        return new()
        {
            EventStore = _eventStore.Name,
            Types = registrations,
            DisableValidation = !_enableEventTypeGenerationValidation
        };
    }
}
