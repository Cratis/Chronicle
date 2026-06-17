// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IMaterializedReadModels"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use.</param>
/// <param name="projections">Projections to get read models from.</param>
/// <param name="reducers">Reducers to get read models from.</param>
/// <param name="schemaGenerator">Schema generator to use.</param>
/// <param name="chronicleServicesAccessor">Accessor for Chronicle services.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for JSON serialization.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
public class MaterializedReadModels(
    IEventStore eventStore,
    IProjections projections,
    IReducers reducers,
    IJsonSchemaGenerator schemaGenerator,
    IChronicleServicesAccessor chronicleServicesAccessor,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<MaterializedReadModels> logger) : IMaterializedReadModels
{
    /// <inheritdoc/>
    public async Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(InstanceCountToSkip? skip = null, InstanceCount? take = null)
    {
        // Apply defaults if not provided
        skip ??= InstanceCountToSkip.Zero;
        take ??= InstanceCount.Default;

        var readModelType = typeof(TReadModel);

        // Validate that the read model is known by projections or reducers
        if (!projections.HasFor(readModelType) && !reducers.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        // Get the read model identifier
        var readModelIdentifier = readModelType.GetReadModelIdentifier();
        var pageSize = take.Value == InstanceCount.Unlimited.Value ? int.MaxValue : take.Value;
        var page = 0;

        // Calculate skip by converting to page-based pagination
        if (skip.Value > 0 && take.Value > 0 && take.Value != InstanceCount.Unlimited.Value)
        {
            page = skip.Value / take.Value;
        }

        var skipWithinPage = skip.Value % take.Value;

        var request = new GetInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModel = readModelIdentifier,
            Page = page,
            PageSize = pageSize
        };

        var response = await chronicleServicesAccessor.Services.MaterializedReadModels.GetInstances(request);
        var instances = response.Instances
            .Select(json => JsonSerializer.Deserialize<TReadModel>(json, jsonSerializerOptions)!);

        // If there's a remaining skip within the page, skip those instances
        var result = skipWithinPage > 0 ? instances.Skip(skipWithinPage) : instances;

        // Release (decrypt) the instances before returning
        return await ReleaseInstances(result);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<TReadModel>> ObserveInstances<TReadModel>(InstanceCountToSkip? skip = null, InstanceCount? take = null)
    {
        // Apply defaults if not provided
        skip ??= InstanceCountToSkip.Zero;
        take ??= InstanceCount.Default;

        var readModelType = typeof(TReadModel);

        // Validate that the read model is known by projections or reducers
        if (!projections.HasFor(readModelType) && !reducers.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        // Get the read model identifier
        var readModelIdentifier = readModelType.GetReadModelIdentifier();
        var pageSize = take.Value == InstanceCount.Unlimited.Value ? int.MaxValue : take.Value;
        var page = 0;

        // Calculate skip by converting to page-based pagination
        if (skip.Value > 0 && take.Value > 0 && take.Value != InstanceCount.Unlimited.Value)
        {
            page = skip.Value / take.Value;
        }

        var skipWithinPage = skip.Value % take.Value;

        var request = new ObserveInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModel = readModelIdentifier,
            Page = page,
            PageSize = pageSize
        };

        return chronicleServicesAccessor.Services.MaterializedReadModels.ObserveInstances(request)
            .SelectMany(async response =>
            {
                var instances = response.Instances
                    .Select(json => JsonSerializer.Deserialize<TReadModel>(json, jsonSerializerOptions)!);

                // If there's a remaining skip within the page, skip those instances
                var filteredInstances = skipWithinPage > 0 ? instances.Skip(skipWithinPage) : instances;

                // Release (decrypt) the instances before returning
                return await ReleaseInstances(filteredInstances);
            });
    }

    async Task<IEnumerable<TReadModel>> ReleaseInstances<TReadModel>(IEnumerable<TReadModel> instances)
    {
        var result = new List<TReadModel>();
        foreach (var instance in instances)
        {
            var released = await ReleaseInstance(instance);
            result.Add(released);
        }
        return result;
    }

    async Task<TReadModel> ReleaseInstance<TReadModel>(TReadModel instance)
    {
        var subject = ReadModelSubjectResolver.ResolveFrom(instance);
        if (subject is null)
        {
            return instance;
        }

        return await ReleaseWithSubject(subject, instance);
    }

    async Task<TReadModel> ReleaseWithSubject<TReadModel>(Subject subject, TReadModel instance)
    {
        var schema = schemaGenerator.Generate(typeof(TReadModel));
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

        var payload = JsonSerializer.Serialize(instance, jsonSerializerOptions);
        var request = new Contracts.Compliance.ReleaseRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            Subject = subject.Value,
            Schema = schema.ToJson(),
            Payload = payload
        };

        var response = await chronicleServicesAccessor.Services.Compliance.Release(request);
        if (response.HasError)
        {
            logger.FailedToRelease(typeof(TReadModel).Name, subject.Value, response.Error);
            return instance;
        }

        return JsonSerializer.Deserialize<TReadModel>(response.Payload, jsonSerializerOptions) ?? instance;
    }
}
