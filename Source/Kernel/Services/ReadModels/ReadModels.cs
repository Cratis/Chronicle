// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="storage">The storage.</param>
internal sealed class ReadModels(IGrainFactory grainFactory, IStorage storage) : IReadModels
{
    /// <inheritdoc/>
    public async Task RegisterMany(RegisterManyRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var readModelDefinitions = request.ReadModels.Select(definition => definition.ToChronicle(request.Owner)).ToArray();
        await readModelsManager.Register(readModelDefinitions);
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(RegisterSingleRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var readModelDefinition = request.ReadModel.ToChronicle(request.Owner);
        await readModelsManager.RegisterSingle(readModelDefinition);
    }

    /// <inheritdoc/>
    public async Task UpdateDefinition(UpdateDefinitionRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var readModelDefinition = request.ReadModel.ToChronicle(ReadModelOwner.None);
        await readModelsManager.UpdateDefinition(readModelDefinition);
    }

    /// <inheritdoc/>
    public async Task<GetDefinitionsResponse> GetDefinitions(GetDefinitionsRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var definitions = await readModelsManager.GetDefinitions();
        return new()
        {
            ReadModels = definitions.Select(_ => _.ToContract()).ToList()
        };
    }

    /// <inheritdoc/>
    public async Task<GetOccurrencesResponse> GetOccurrences(GetOccurrencesRequest request, CallContext context = default)
    {
        var readModelReplayManager = grainFactory.GetReadModelReplayManager(request.EventStore, request.Namespace, request.Type.Identifier);
        var occurrences = await readModelReplayManager.GetOccurrences();
        return new()
        {
            Occurrences = occurrences.Select(_ => _.ToContract()).ToList()
        };
    }

    /// <inheritdoc/>
    public async Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModel, request.EventStore);
        var definition = await readModel.GetDefinition();
        var sinks = storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Sinks;
        var sink = sinks.GetFor(definition);
        var skip = request.Page * request.PageSize;
        var (instances, totalCount) = await sink.GetInstances(
            string.IsNullOrEmpty(request.Occurrence) ? null! : request.Occurrence,
            skip,
            request.PageSize);

        var instancesAsJson = instances.Select(instance => System.Text.Json.JsonSerializer.Serialize(instance));
        return new()
        {
            Instances = instancesAsJson,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
