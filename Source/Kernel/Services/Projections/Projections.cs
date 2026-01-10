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
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using ContractProjectionDefinitionParsingErrors = Cratis.Chronicle.Contracts.Projections.ProjectionDefinitionParsingErrors;
using ContractProjectionPreview = Cratis.Chronicle.Contracts.Projections.ProjectionPreview;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting ExpandoObjects.</param>
/// <param name="languageService"><see cref="ILanguageService"/> for handling projection definition language.</param>
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
    public async Task<IEnumerable<Contracts.Projections.ProjectionWithDsl>> GetAllDsls(GetAllDslsRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
        var dsls = await projectionsManager.GetProjectionDsls();
        return dsls.Select(p => new Contracts.Projections.ProjectionWithDsl
        {
            Identifier = p.Identifier,
            ReadModel = p.ReadModel,
            Dsl = p.Dsl
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task<OneOf<ContractProjectionPreview, ContractProjectionDefinitionParsingErrors>> PreviewFromDsl(PreviewProjectionRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var eventSequenceStorage = storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .GetEventSequence(request.EventSequenceId);

        var projectionId = ProjectionId.CreatePreviewId();
        var projectionKey = new ProjectionKey(projectionId, request.EventStore);
        var projection = grainFactory.GetGrain<IProjection>(projectionKey);

        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Dsl ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return await compileResult.Match(
            async definition =>
            {
                var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
                var readModelDefinition = allReadModels.First(r => r.GetSchemaForLatestGeneration().Title! == definition.ReadModel);
                definition = definition with { ReadModel = readModelDefinition.Identifier };

                await projection.SetDefinition(definition);

                var eventTypes = await projection.GetEventTypes();

                // Fetch a limited number of events and group them by correlation id
                const int defaultLimit = 1000;
                using var cursor = await eventSequenceStorage.GetEventsWithLimit(EventSequenceNumber.First, defaultLimit, eventTypes: eventTypes);
                var events = new List<AppendedEvent>();

                while (await cursor.MoveNext())
                {
                    events.AddRange(cursor.Current);
                }

                var result = await projection.Process(request.Namespace, events);
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
    public async Task SaveFromDsl(SaveProjectionRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Dsl ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        await compileResult.Match(
            async definition =>
            {
                var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
                var readModelDefinition = allReadModels.First(r => r.GetSchemaForLatestGeneration().Title! == definition.ReadModel);
                definition = definition with { ReadModel = readModelDefinition.Identifier, EventSequenceId = request.EventSequenceId };

                var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
                await projectionsManager.Register([definition]);
                return Task.CompletedTask;
            },
            errors => throw new InvalidOperationException($"Failed to save projection: {string.Join(", ", errors.Errors.Select(e => e.Message))}"));
    }

    /// <inheritdoc/>
    public async Task<OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>> GenerateDeclarativeCodeFromDsl(GenerateDeclarativeCodeRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Dsl ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return compileResult.Match(
            definition =>
            {
                var readModelDefinition = allReadModels.First(r => r.GetSchemaForLatestGeneration().Title! == definition.ReadModel);
                var code = languageService.GenerateDeclarativeCode(definition, readModelDefinition);

                return new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(new GeneratedCode { Code = code });
            },
            errors => new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(errors.ToContract()));
    }

    /// <inheritdoc/>
    public async Task<OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>> GenerateModelBoundCodeFromDsl(GenerateModelBoundCodeRequest request, CallContext context = default)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var allReadModels = await storage.GetEventStore(request.EventStore).ReadModels.GetAll();
        var eventTypeSchemas = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();

        var compileResult = languageService.Compile(
            request.Dsl ?? string.Empty,
            Concepts.Projections.ProjectionOwner.Server,
            allReadModels,
            eventTypeSchemas);

        return compileResult.Match(
            definition =>
            {
                var readModelDefinition = allReadModels.First(r => r.GetSchemaForLatestGeneration().Title! == definition.ReadModel);
                var code = languageService.GenerateModelBoundCode(definition, readModelDefinition);

                return new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(new GeneratedCode { Code = code });
            },
            errors => new OneOf<GeneratedCode, ContractProjectionDefinitionParsingErrors>(errors.ToContract()));
    }
}
