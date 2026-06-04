// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IMaterializedReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="storage">The storage.</param>
/// <param name="complianceHelper">The <see cref="IReadModelComplianceHelper"/> for decrypting PII fields.</param>
internal sealed class MaterializedReadModels(
    IGrainFactory grainFactory,
    IStorage storage,
    IReadModelComplianceHelper complianceHelper) : IMaterializedReadModels
{
    /// <inheritdoc/>
    public async Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModel, request.EventStore);
        var definition = await readModel.GetDefinition();
        var sinks = storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Sinks;
        var sink = await sinks.GetFor(definition);
        var skip = Math.Max(0, request.Page * request.PageSize);

        ReadModelContainerName? occurrence = null;
        if (!string.IsNullOrEmpty(request.Occurrence))
        {
            occurrence = request.Occurrence;
        }

        var (instances, totalCount) = await sink.GetInstances(
            occurrence,
            skip,
            request.PageSize);

        var schema = definition.GetSchemaForLatestGeneration();
        var releasedInstances = await complianceHelper.Release(
            request.EventStore,
            request.Namespace,
            schema,
            instances ?? []);

        var instancesAsJson = releasedInstances.Select(instance => JsonSerializer.Serialize(instance)).ToList();
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
            var definition = await readModel.GetDefinition();
            var sinks = storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Sinks;
            var sink = await sinks.GetFor(definition);
            var skip = Math.Max(0, request.Page * request.PageSize);

            ReadModelContainerName? occurrence = null;
            if (!string.IsNullOrEmpty(request.Occurrence))
            {
                occurrence = request.Occurrence;
            }

            var schema = definition.GetSchemaForLatestGeneration();
            return (sink, occurrence, skip, schema);
        })
        .SelectMany(state =>
            state.sink.ObserveInstances(state.occurrence, state.skip, request.PageSize)
                .SelectMany(async instances =>
                {
                    var releasedInstances = await complianceHelper.Release(
                        request.EventStore,
                        request.Namespace,
                        state.schema,
                        instances);

                    var instancesAsJson = releasedInstances.Select(instance => JsonSerializer.Serialize(instance)).ToList();
                    var (_, totalCount) = await state.sink.GetInstances(state.occurrence, state.skip, request.PageSize);

                    return new ObserveInstancesResponse
                    {
                        Instances = instancesAsJson,
                        TotalCount = (int)totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize
                    };
                }));
    }
}
