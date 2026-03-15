// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using NJsonSchema;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypes"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
internal sealed class EventTypes(IStorage storage) : IEventTypes
{
    /// <inheritdoc/>
    public async Task Register(RegisterEventTypesRequest request)
    {
#if DEVELOPMENT
        var skipValidation = request.DisableValidation;
#else
        const bool skipValidation = false;
#endif
        if (!skipValidation)
        {
            foreach (var eventType in request.Types)
            {
                ValidateMigrationChain(eventType.Type.Id, eventType.Type.Generation, eventType.Migrations);
            }
        }

        foreach (var eventType in request.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            var owner = (Concepts.Events.EventTypeOwner)(int)eventType.Owner;
            var source = (Concepts.Events.EventTypeSource)(int)eventType.Source;

            if (eventType.Migrations.Count > 0 || eventType.Generations.Count > 1)
            {
                // Register using full definition with all generations and migrations
                var generations = new List<Concepts.Events.EventTypeGenerationDefinition>();
                foreach (var genDef in eventType.Generations)
                {
                    var genSchema = await JsonSchema.FromJsonAsync(genDef.Schema);
                    generations.Add(new Concepts.Events.EventTypeGenerationDefinition(genDef.Generation, genSchema));
                }

                if (generations.Count == 0)
                {
                    generations.Add(new Concepts.Events.EventTypeGenerationDefinition(eventType.Type.ToChronicle().Generation, schema));
                }

                var migrations = eventType.Migrations.Select(m =>
                {
                    var upcastJson = string.IsNullOrEmpty(m.UpcastJmesPath)
                        ? new JsonObject()
                        : JsonNode.Parse(m.UpcastJmesPath)?.AsObject() ?? new JsonObject();
                    var downcastJson = string.IsNullOrEmpty(m.DowncastJmesPath)
                        ? new JsonObject()
                        : JsonNode.Parse(m.DowncastJmesPath)?.AsObject() ?? new JsonObject();
                    return new Concepts.Events.EventTypeMigrationDefinition(
                        m.FromGeneration,
                        m.ToGeneration,
                        [],
                        upcastJson,
                        downcastJson);
                }).ToList();

                var definition = new EventTypeDefinition(
                    eventType.Type.ToChronicle().Id,
                    owner,
                    eventType.Type.Tombstone,
                    generations,
                    migrations);

                await storage.GetEventStore(request.EventStore).EventTypes.Register(definition);
            }
            else
            {
                await storage
                    .GetEventStore(request.EventStore).EventTypes
                    .Register(
                        eventType.Type.ToChronicle(),
                        schema,
                        owner,
                        source);
            }
        }
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(RegisterSingleEventTypeRequest request)
    {
        var schema = await JsonSchema.FromJsonAsync(request.Type.Schema);
        await storage
            .GetEventStore(request.EventStore).EventTypes
            .Register(
                request.Type.Type.ToChronicle(),
                schema,
                (Concepts.Events.EventTypeOwner)(int)request.Type.Owner,
                (Concepts.Events.EventTypeSource)(int)request.Type.Source);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Events.EventType>> GetAll(GetAllEventTypesRequest request)
    {
        var eventTypes = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => _.Type.ToContract());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeRegistration>> GetAllRegistrations(GetAllEventTypesRequest request)
    {
        var eventTypes = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => new EventTypeRegistration
        {
            Type = _.Type.ToContract(),
            Owner = (Contracts.Events.EventTypeOwner)(int)_.Owner,
            Source = (Contracts.Events.EventTypeSource)(int)_.Source,
            Schema = _.Schema.ToJson()
        });
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<EventTypeRegistration>> ObserveAllRegistrations(GetAllEventTypesRequest request, CallContext context = default)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        return eventStore.EventTypes
            .ObserveLatestForAllEventTypes()
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.Select(_ => new EventTypeRegistration
            {
                Type = _.Type.ToContract(),
                Owner = (Contracts.Events.EventTypeOwner)(int)_.Owner,
                Source = (Contracts.Events.EventTypeSource)(int)_.Source,
                Schema = _.Schema.ToJson()
            }).ToArray());
    }

    static void ValidateMigrationChain(string eventTypeId, uint currentGeneration, IList<Contracts.Events.EventTypeMigrationDefinition> migrations)
    {
        if (currentGeneration <= 1)
            return;

        if (migrations.Count == 0)
            throw new MissingEventTypeMigrators(eventTypeId, currentGeneration);

        if (!migrations.Any(m => m.FromGeneration == 1))
            throw new MissingFirstGenerationForEventType(eventTypeId, currentGeneration);

        for (uint from = 1; from < currentGeneration; from++)
        {
            if (!migrations.Any(m => m.FromGeneration == from))
                throw new MissingMigrationForEventTypeGeneration(eventTypeId, currentGeneration, from);
        }
    }
}
