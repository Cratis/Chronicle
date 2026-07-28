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
        var paging = CalculatePaging(skip, take);

        var request = new GetInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModel = readModelIdentifier,
            Page = paging.Page,
            PageSize = paging.PageSize
        };

        var response = await chronicleServicesAccessor.Services.MaterializedReadModels.GetInstances(request);
        var instances = response.Instances
            .Select(json => JsonSerializer.Deserialize<TReadModel>(json, jsonSerializerOptions)!)
            .Skip(paging.LocalSkip)
            .Take(paging.LocalTake);

        // Release (decrypt) the instances before returning
        return await ReleaseInstances(instances);
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
        var paging = CalculatePaging(skip, take);

        var request = new ObserveInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModel = readModelIdentifier,
            Page = paging.Page,
            PageSize = paging.PageSize
        };

        return chronicleServicesAccessor.Services.MaterializedReadModels.ObserveInstances(request)
            .SelectMany(async response =>
            {
                var instances = response.Instances
                    .Select(json => JsonSerializer.Deserialize<TReadModel>(json, jsonSerializerOptions)!)
                    .Skip(paging.LocalSkip)
                    .Take(paging.LocalTake);

                // Release (decrypt) the instances before returning
                return await ReleaseInstances(instances);
            });
    }

    /// <summary>
    /// Translates a requested <paramref name="skip"/>/<paramref name="take"/> window into the server's
    /// page-aligned paging contract plus the local slicing needed to return the exact window.
    /// </summary>
    /// <param name="skip">The number of instances to skip.</param>
    /// <param name="take">The number of instances to take.</param>
    /// <returns>The server page and page size to request, and the local skip and take to apply to the response.</returns>
    /// <remarks>
    /// The server only exposes page-aligned offsets (its effective offset is always <c>Page * PageSize</c>).
    /// When <paramref name="skip"/> is a multiple of <paramref name="take"/> the requested window is exactly one
    /// server page, so the page request is used directly. When it is not, the window <c>[skip, skip+take)</c>
    /// straddles two server pages; a covering range is fetched from the start and sliced locally so the full
    /// <paramref name="take"/> items are returned instead of only the tail of the first page.
    /// </remarks>
    static (int Page, int PageSize, int LocalSkip, int LocalTake) CalculatePaging(InstanceCountToSkip skip, InstanceCount take)
    {
        var skipCount = Math.Max(0, skip.Value);
        var takeCount = take.Value;

        // Unlimited take: fetch everything from the start and skip locally.
        if (takeCount == InstanceCount.Unlimited.Value)
        {
            return (0, int.MaxValue, skipCount, int.MaxValue);
        }

        // A take of zero (or less) requests no instances — fetch nothing (and never divide by zero below).
        if (takeCount <= 0)
        {
            return (0, 0, 0, 0);
        }

        // Page-aligned skip: the requested window is exactly one server page.
        if (skipCount % takeCount == 0)
        {
            return (skipCount / takeCount, takeCount, 0, takeCount);
        }

        // Non-aligned skip: the window straddles two server pages. Fetch a covering range from the
        // start (page size guarded against int overflow) and slice it locally to the requested window.
        var coveringPageSize = (int)Math.Min(int.MaxValue, (long)skipCount + takeCount);

        return (0, coveringPageSize, skipCount, takeCount);
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
