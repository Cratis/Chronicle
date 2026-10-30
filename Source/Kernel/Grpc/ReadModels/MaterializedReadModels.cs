// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IMaterializedReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="readBindings">The bindings for resolving materialized read destinations.</param>
/// <param name="complianceHelper">The <see cref="IReadModelsCompliance"/> for decrypting compliance and security fields.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
internal sealed class MaterializedReadModels(
    IGrainFactory grainFactory,
    IReadModelReadBindings readBindings,
    IReadModelsCompliance complianceHelper,
    ILogger<MaterializedReadModels> logger) : IMaterializedReadModels
{
    /// <inheritdoc/>
    public async Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModel, request.EventStore);
        var definition = await readModel.GetKnownDefinition(request.ReadModel);
        var occurrence = string.IsNullOrEmpty(request.Occurrence) ? null : (ReadModelContainerName)request.Occurrence;
        var binding = await readBindings.Resolve(request.EventStore, request.Namespace, definition, occurrence);
        var skip = Math.Max(0, request.Page * request.PageSize);

        var (instances, totalCount) = await binding.GetInstances(skip, request.PageSize);

        var schema = definition.GetSchemaForLatestGeneration();
        var releasedInstances = await complianceHelper.Release(
            request.EventStore,
            request.Namespace,
            schema,
            instances);

        var instancesAsJson = releasedInstances.Select(instance => JsonSerializer.Serialize(instance.WithoutInitializationState(schema))).ToList();
        return new()
        {
            Instances = instancesAsJson,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <inheritdoc/>
    public IObservable<ObserveInstancesResponse> ObserveInstances(ObserveInstancesRequest request, CallContext context = default)
    {
        return Observable.FromAsync(async () =>
        {
            var readModel = grainFactory.GetReadModel(request.ReadModel, request.EventStore);
            var definition = await readModel.GetKnownDefinition(request.ReadModel);
            var occurrence = string.IsNullOrEmpty(request.Occurrence) ? null : (ReadModelContainerName)request.Occurrence;
            var binding = await readBindings.Resolve(request.EventStore, request.Namespace, definition, occurrence);
            var skip = Math.Max(0, request.Page * request.PageSize);

            var schema = definition.GetSchemaForLatestGeneration();
            return (binding, skip, schema);
        })

        // Pages are released and counted one at a time and in order, so a slower earlier page can never overtake a
        // later one. Only the latest page is kept waiting while one is being processed: under sustained writes the
        // sink emits faster than a slow client or release can drain, and every page queued would cost a release, a
        // count and a page read to deliver a snapshot that a newer one has already replaced.
        .SelectMany(state =>
            state.binding.ObserveInstances(state.skip, request.PageSize)
                .SelectLatestSequentially(
                    async instances =>
                    {
                        var releasedInstances = await complianceHelper.Release(
                            request.EventStore,
                            request.Namespace,
                            state.schema,
                            instances);

                        var instancesAsJson = releasedInstances.Select(instance => JsonSerializer.Serialize(instance.WithoutInitializationState(state.schema))).ToList();
                        var (_, totalCount) = await state.binding.GetInstances(state.skip, request.PageSize);

                        return new ObserveInstancesResponse
                        {
                            Instances = instancesAsJson,
                            TotalCount = (int)totalCount,
                            Page = request.Page,
                            PageSize = request.PageSize
                        };
                    },
                    logger));
    }
}
