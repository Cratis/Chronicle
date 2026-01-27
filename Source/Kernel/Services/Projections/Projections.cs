// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.DefinitionLanguage;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Services.Projections.Definitions;
using Cratis.Chronicle.Services.ReadModels;
using Cratis.Chronicle.Storage;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema;
using ProtoBuf.Grpc;
using ContractProjectionDefinitionParsingErrors = Cratis.Chronicle.Contracts.Projections.ProjectionDeclarationParsingErrors;
using ContractProjectionPreview = Cratis.Chronicle.Contracts.Projections.ProjectionPreview;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;
using WellKnownSinkTypes = Cratis.Chronicle.Concepts.Sinks.WellKnownSinkTypes;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting ExpandoObjects.</param>
/// <param name="languageService"><see cref="ILanguageService"/> for handling projection declaration language.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for accessing services.</param>
internal sealed class Projections(
    IGrainFactory grainFactory,
    IExpandoObjectConverter expandoObjectConverter,
    ILanguageService languageService,
    IServiceProvider serviceProvider) : IProjections
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
        var projections = request.Projections.Select(_ => _.ToChronicle((Concepts.Projections.ProjectionOwner)(int)request.Owner)).ToArray();

        _ = Task.Run(() => projectionsManager.Register(projections));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAllDefinitions(GetAllDefinitionsRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
        var definitions = await projectionsManager.GetProjectionDefinitions();
        return definitions.Select(p => p.ToContract()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Projections.ProjectionWithDeclaration>> GetAllDeclarations(GetAllDeclarationsRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
        var definitions = await projectionsManager.GetProjectionDeclarations();
        return definitions.Select(p => new Contracts.Projections.ProjectionWithDeclaration
        {
            Identifier = p.Identifier,
            ReadModel = p.ReadModel,
            Declaration = p.Declaration
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task<OneOf<ContractProjectionPreview, ContractProjectionDefinitionParsingErrors>> Preview(PreviewProjectionRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var eventSequenceStorage = storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .GetEventSequence(request.EventSequenceId);

        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();

        // If a draft read model is provided, create a temporary read model definition for preview
        Concepts.ReadModels.ReadModelDefinition? draftDefinition = null;
        if (request.DraftReadModel is not null)
        {
            draftDefinition = CreateDraftReadModelDefinition(request.DraftReadModel);
            allReadModels = allReadModels.Append(draftDefinition).ToList();
        }

        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Declaration ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return await compileResult.Match(
            async definition =>
            {
                // Find the read model definition - need to handle potential empty schemas gracefully
                var readModelDefinition = allReadModels.FirstOrDefault(r =>
                {
                    if (r.Schemas.Count == 0) return false;
                    var schema = r.GetSchemaForLatestGeneration();
                    return schema.Title == definition.ReadModel;
                });

                if (readModelDefinition is null)
                {
                    return new OneOf<ContractProjectionPreview, ContractProjectionDefinitionParsingErrors>(
                        new ContractProjectionDefinitionParsingErrors
                        {
                            Errors = [new ProjectionDeclarationSyntaxError
                            {
                                Line = 1,
                                Column = 1,
                                Message = $"Read model '{definition.ReadModel}' not found"
                            }]
                        });
                }

                definition = definition with { ReadModel = readModelDefinition.Identifier };

                var projectionId = ProjectionId.CreatePreviewId();
                var projectionKey = new ProjectionKey(projectionId, request.EventStore);
                var projection = grainFactory.GetGrain<Grains.Projections.IProjection>(projectionKey);
                await projection.SetDefinition(definition);

                // Get event types - use preview method if we have a draft read model
                IEnumerable<Concepts.Events.EventType> eventTypes;
                if (draftDefinition is not null && readModelDefinition.Identifier == draftDefinition.Identifier)
                {
                    eventTypes = await projection.GetEventTypesForPreview(readModelDefinition);
                }
                else
                {
                    eventTypes = await projection.GetEventTypes();
                }

                // Fetch a limited number of events and group them by correlation id
                const int defaultLimit = 1000;
                using var cursor = await eventSequenceStorage.GetEventsWithLimit(EventSequenceNumber.First, defaultLimit, eventTypes: eventTypes);
                var events = new List<AppendedEvent>();

                while (await cursor.MoveNext())
                {
                    events.AddRange(cursor.Current);
                }

                // Process events - use preview method if we have a draft read model
                IEnumerable<System.Dynamic.ExpandoObject> result;
                if (draftDefinition is not null && readModelDefinition.Identifier == draftDefinition.Identifier)
                {
                    result = await projection.ProcessForPreview(request.Namespace, events, readModelDefinition);
                }
                else
                {
                    result = await projection.Process(request.Namespace, events);
                }

                var readModels = result.Select(r => expandoObjectConverter.ToJsonObject(r, readModelDefinition.GetSchemaForLatestGeneration()).ToString()).ToArray();

                return new OneOf<ContractProjectionPreview, ContractProjectionDefinitionParsingErrors>(new ContractProjectionPreview
                {
                    ReadModelEntries = readModels,
                    ReadModel = readModelDefinition.ToContract()
                });
            },
            errors => Task.FromResult(new OneOf<ContractProjectionPreview, ContractProjectionDefinitionParsingErrors>(errors.ToContract())));
    }

    /// <inheritdoc/>
    public async Task<SaveProjectionResult> Save(SaveProjectionRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();

        // If a draft read model is provided, save it first
        if (request.DraftReadModel is not null)
        {
            var readModelsService = serviceProvider.GetRequiredService<IReadModelsService>();
            var identifier = Guid.NewGuid().ToString();
            var schema = string.IsNullOrEmpty(request.DraftReadModel.Schema)
                ? new JsonSchema { Type = JsonObjectType.Object }.ToJson()
                : request.DraftReadModel.Schema;

            await readModelsService.RegisterSingle(new()
            {
                EventStore = request.EventStore,
                Owner = Contracts.ReadModels.ReadModelOwner.Client,
                Source = Contracts.ReadModels.ReadModelSource.User,
                ReadModel = new()
                {
                    Type = new()
                    {
                        Identifier = identifier,
                        Generation = 1,
                    },
                    Name = request.DraftReadModel.Name.Pluralize(),
                    DisplayName = request.DraftReadModel.Name,
                    Sink = new()
                    {
                        TypeId = WellKnownSinkTypes.MongoDB.Value,
                        ConfigurationId = Guid.Empty
                    },
                    Schema = schema,
                    Indexes = []
                }
            });

            // Refresh the read models list to include the newly created one
            allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        }

        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Declaration ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return await compileResult.Match(
            async definition =>
            {
                var readModelDefinition = allReadModels.FirstOrDefault(r =>
                {
                    if (r.Schemas.Count == 0) return false;
                    var schema = r.GetSchemaForLatestGeneration();
                    return schema.Title == definition.ReadModel;
                });

                if (readModelDefinition is null)
                {
                    return new SaveProjectionResult
                    {
                        Errors = [new ProjectionDeclarationSyntaxError
                        {
                            Line = 1,
                            Column = 1,
                            Message = $"Read model '{definition.ReadModel}' not found"
                        }]
                    };
                }

                definition = definition with { ReadModel = readModelDefinition.Identifier, EventSequenceId = request.EventSequenceId };

                var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
                await projectionsManager.Register([definition]);
                return new SaveProjectionResult();
            },
            errors => Task.FromResult(new SaveProjectionResult { Errors = errors.ToContract().Errors }));
    }

    /// <inheritdoc/>
    public async Task<OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>> GenerateDeclarativeCode(GenerateDeclarativeCodeRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Declaration ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return compileResult.Match(
            definition =>
            {
                var readModelDefinition = allReadModels.FirstOrDefault(r =>
                {
                    if (r.Schemas.Count == 0) return false;
                    var schema = r.GetSchemaForLatestGeneration();
                    return schema.Title == definition.ReadModel;
                });

                if (readModelDefinition is null)
                {
                    return new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(
                        new ContractProjectionDefinitionParsingErrors
                        {
                            Errors = [new ProjectionDeclarationSyntaxError
                            {
                                Line = 1,
                                Column = 1,
                                Message = $"Read model '{definition.ReadModel}' not found"
                            }]
                        });
                }

                var code = languageService.GenerateDeclarativeCode(definition, readModelDefinition);

                return new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(new GeneratedCode { Code = code });
            },
            errors => new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(errors.ToContract()));
    }

    /// <inheritdoc/>
    public async Task<OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>> GenerateModelBoundCode(GenerateModelBoundCodeRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Declaration ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return compileResult.Match(
            definition =>
            {
                var readModelDefinition = allReadModels.FirstOrDefault(r =>
                {
                    if (r.Schemas.Count == 0) return false;
                    var schema = r.GetSchemaForLatestGeneration();
                    return schema.Title == definition.ReadModel;
                });

                if (readModelDefinition is null)
                {
                    return new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(
                        new ContractProjectionDefinitionParsingErrors
                        {
                            Errors = [new ProjectionDeclarationSyntaxError
                            {
                                Line = 1,
                                Column = 1,
                                Message = $"Read model '{definition.ReadModel}' not found"
                            }]
                        });
                }

                var code = languageService.GenerateModelBoundCode(definition, readModelDefinition);

                return new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(new GeneratedCode { Code = code });
            },
            errors => new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(errors.ToContract()));
    }

    static Concepts.ReadModels.ReadModelDefinition CreateDraftReadModelDefinition(DraftReadModelDefinition draft)
    {
        var identifier = $"draft-{Guid.NewGuid()}";
        JsonSchema schema;

        if (string.IsNullOrEmpty(draft.Schema))
        {
            schema = new JsonSchema { Type = JsonObjectType.Object, Title = draft.Name };
        }
        else
        {
            try
            {
                schema = JsonSchema.FromJsonAsync(draft.Schema).GetAwaiter().GetResult();
            }
            catch
            {
                // Fallback to basic schema if parsing fails
                schema = new JsonSchema { Type = JsonObjectType.Object, Title = draft.Name };
            }
        }

        if (string.IsNullOrEmpty(schema.Title))
        {
            schema.Title = draft.Name;
        }

        var schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema>
        {
            { Concepts.ReadModels.ReadModelGeneration.First, schema }
        };

        return new Concepts.ReadModels.ReadModelDefinition(
            identifier,
            draft.Name.Pluralize(),
            draft.Name,
            Concepts.ReadModels.ReadModelOwner.Server,
            Concepts.ReadModels.ReadModelSource.User,
            Concepts.ReadModels.ReadModelObserverType.Projection,
            Concepts.ReadModels.ReadModelObserverIdentifier.Unspecified,
            new Concepts.Sinks.SinkDefinition(Concepts.Sinks.SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            schemas,
            []);
    }
}
