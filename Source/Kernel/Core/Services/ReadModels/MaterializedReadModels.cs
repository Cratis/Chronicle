// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IMaterializedReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="storage">The storage.</param>
/// <param name="expandoObjectConverter">The expando object converter.</param>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for decrypting PII fields.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
internal sealed class MaterializedReadModels(
    IGrainFactory grainFactory,
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    IJsonComplianceManager complianceManager,
    JsonSerializerOptions jsonSerializerOptions) : IMaterializedReadModels
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
        var releasedInstances = await ReadModelReleaseHelper.Release(
            complianceManager,
            request.EventStore,
            request.Namespace,
            schema,
            instances ?? [],
            expandoObjectConverter);

        var instancesAsJson = releasedInstances.ConvertAll(instance => JsonSerializer.Serialize(instance));
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
        return Observable.Create<ObserveInstancesResponse>(async observer =>
        {
            try
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

                return sink.ObserveInstances(occurrence, skip, request.PageSize)
                    .SelectMany(async instances =>
                    {
                        var releasedInstances = await ReadModelReleaseHelper.Release(
                            complianceManager,
                            request.EventStore,
                            request.Namespace,
                            schema,
                            instances,
                            expandoObjectConverter);

                        var instancesAsJson = releasedInstances.ToList().ConvertAll(instance => JsonSerializer.Serialize(instance));
                        var (_, totalCount) = await sink.GetInstances(occurrence, skip, request.PageSize);

                        return new ObserveInstancesResponse
                        {
                            Instances = instancesAsJson,
                            TotalCount = totalCount,
                            Page = request.Page,
                            PageSize = request.PageSize
                        };
                    })
                    .Subscribe(observer);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return System.Reactive.Disposables.Disposable.Empty;
            }
        });
    }
}
