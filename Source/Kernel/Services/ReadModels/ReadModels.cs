// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Storage;
using NJsonSchema;
using Orleans.Streams;
using ProtoBuf.Grpc;
using AppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using ProjectionChangeset = Cratis.Chronicle.Grains.Projections.ProjectionChangeset;
using ReadModelSnapshot = Cratis.Chronicle.Contracts.ReadModels.ReadModelSnapshot;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="clusterClient">The cluster client.</param>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="storage">The storage.</param>
/// <param name="expandoObjectConverter">The expando object converter.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
internal sealed class ReadModels(
    IClusterClient clusterClient,
    IGrainFactory grainFactory,
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions) : IReadModels
{
    /// <inheritdoc/>
    public async Task RegisterMany(RegisterManyRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var readModelDefinitions = request.ReadModels.Select(definition => definition.ToChronicle(request.Owner, request.Source)).ToArray();
        await readModelsManager.Register(readModelDefinitions);
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(RegisterSingleRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var readModelDefinition = request.ReadModel.ToChronicle(request.Owner, request.Source);
        await readModelsManager.RegisterSingle(readModelDefinition);
    }

    /// <inheritdoc/>
    public async Task UpdateDefinition(UpdateDefinitionRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var existingDefinitions = await readModelsManager.GetDefinitions();
        var existingDefinition = existingDefinitions.FirstOrDefault(d => d.Identifier == request.ReadModel.Type.Identifier) ??
            throw new InvalidOperationException($"Read model with identifier '{request.ReadModel.Type.Identifier}' not found.");

        var schema = await JsonSchema.FromJsonAsync(request.ReadModel.Schema);
        var indexes = request.ReadModel.Indexes
            .Select(i => new Concepts.ReadModels.IndexDefinition(i.PropertyPath))
            .ToArray();

        var updatedDefinition = new Concepts.ReadModels.ReadModelDefinition(
            existingDefinition.Identifier,
            request.ReadModel.ContainerName,
            existingDefinition.DisplayName,
            existingDefinition.Owner,
            existingDefinition.Source,
            existingDefinition.ObserverType,
            existingDefinition.ObserverIdentifier,
            existingDefinition.Sink,
            new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema>
            {
                { (Concepts.ReadModels.ReadModelGeneration)request.ReadModel.Type.Generation, schema }
            },
            indexes);

        await readModelsManager.UpdateDefinition(updatedDefinition);
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

        Concepts.ReadModels.ReadModelContainerName? occurrence = null;
        if (!string.IsNullOrEmpty(request.Occurrence))
        {
            occurrence = request.Occurrence;
        }

        var (instances, totalCount) = await sink.GetInstances(
            occurrence,
            skip,
            request.PageSize);

        var instancesAsJson = instances.Select(instance => JsonSerializer.Serialize(instance));
        return new()
        {
            Instances = instancesAsJson,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<GetSnapshotsByKeyResponse> GetSnapshotsByKey(GetSnapshotsByKeyRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModelIdentifier, request.EventStore);
        var definition = await readModel.GetDefinition();

        IList<ReadModelSnapshot> snapshots;

        if (definition.ObserverType == Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            var projectionSnapshots = await GetSnapshotsForProjection(
                definition.ObserverIdentifier,
                request.EventStore,
                request.Namespace,
                request.EventSequenceId,
                request.ReadModelKey);

            snapshots = projectionSnapshots.Select(s => new ReadModelSnapshot
            {
                ReadModel = s.ReadModel,
                Events = s.Events,
                Occurred = s.Occurred,
                CorrelationId = s.CorrelationId
            }).ToList();
        }
        else
        {
            // For reducers, snapshots are typically computed on the client side
            // Server-side reducers would need additional implementation here
            // For now, return empty snapshots as reducers typically run client-side
            snapshots = [];
        }

        return new GetSnapshotsByKeyResponse
        {
            Snapshots = snapshots
        };
    }

    /// <inheritdoc/>
    public async Task<GetInstanceByKeyResponse> GetInstanceByKey(GetInstanceByKeyRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModelIdentifier, request.EventStore);
        var definition = await readModel.GetDefinition();

        if (definition.ObserverType == Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            var projectionKey = !string.IsNullOrEmpty(request.SessionId)
                ? new ImmediateProjectionKey(
                    (ProjectionId)definition.ObserverIdentifier.Value,
                    request.EventStore,
                    request.Namespace,
                    request.EventSequenceId,
                    request.ReadModelKey,
                    (ProjectionSessionId)Guid.Parse(request.SessionId))
                : new ImmediateProjectionKey(
                    (ProjectionId)definition.ObserverIdentifier.Value,
                    request.EventStore,
                    request.Namespace,
                    request.EventSequenceId,
                    request.ReadModelKey);

            var projection = grainFactory.GetGrain<IImmediateProjection>(projectionKey);
            var result = await projection.GetModelInstance();

            return new GetInstanceByKeyResponse
            {
                ReadModel = result.ReadModel.ToJsonString(jsonSerializerOptions),
                ProjectedEventsCount = (ulong)result.ProjectedEventsCount,
                LastHandledEventSequenceNumber = result.LastHandledEventSequenceNumber
            };
        }

        throw new NotSupportedException("Server-side reducer instance retrieval is not yet supported. Reducers typically run client-side.");
    }

    /// <inheritdoc/>
    public async Task<GetAllInstancesResponse> GetAllInstances(GetAllInstancesRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModelIdentifier, request.EventStore);
        var definition = await readModel.GetDefinition();

        if (definition.ObserverType != Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            throw new NotSupportedException("GetAllInstances is only supported for projections (immediate projections).");
        }

        var eventSequenceStorage = storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .GetEventSequence(request.EventSequenceId);

        var readModelDefinition = await storage.GetEventStore(request.EventStore).ReadModels.Get(definition.Identifier);
        var projectionKey = new ProjectionKey((ProjectionId)definition.ObserverIdentifier.Value, request.EventStore);
        var projection = grainFactory.GetGrain<IProjection>(projectionKey);
        var eventTypes = await projection.GetEventTypes();

        // Get events from the beginning, optionally limited by event count
        var events = new List<AppendedEvent>();

        if (request.EventCount == ulong.MaxValue)
        {
            // Get all events
            var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: null, eventTypes: eventTypes);
            while (await cursor.MoveNext())
            {
                if (!cursor.Current.Any())
                {
                    break;
                }
                events.AddRange(cursor.Current);
            }
            cursor.Dispose();
        }
        else
        {
            // Get limited number of events
            using var cursor = await eventSequenceStorage.GetEventsWithLimit(EventSequenceNumber.First, (int)request.EventCount, eventTypes: eventTypes);
            while (await cursor.MoveNext())
            {
                events.AddRange(cursor.Current);
            }
        }

        // Process events to get all instances grouped by event source ID
        var result = await projection.Process(request.Namespace, events);
        var readModels = result.Select(r => expandoObjectConverter.ToJsonObject(r, readModelDefinition.GetSchemaForLatestGeneration()).ToString()).ToArray();

        return new GetAllInstancesResponse
        {
            Instances = readModels,
            ProcessedEventsCount = (ulong)events.Count
        };
    }

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset> Watch(WatchRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModelIdentifier, request.EventStore);

        return Observable.Create<ReadModelChangeset>(async observer =>
        {
            var definition = await WaitForReadModelDefinition(readModel, context.CancellationToken);

            if (definition.ObserverType == Concepts.ReadModels.ReadModelObserverType.Projection)
            {
                var streamProvider = clusterClient.GetStreamProvider(Grains.WellKnownStreamProviders.ProjectionChangesets);
                var streamId = StreamId.Create(new StreamIdentity(Guid.Empty, definition.ObserverIdentifier));

                var stream = streamProvider.GetStream<ProjectionChangeset>(streamId);

                var subscription = await stream.SubscribeAsync((changeset, _) =>
                {
                    observer.OnNext(new ReadModelChangeset
                    {
                        Namespace = changeset.Namespace,
                        ModelKey = changeset.ReadModelKey,
                        ReadModel = changeset.ReadModel.ToJsonString(jsonSerializerOptions),
                        Removed = false
                    });

                    return Task.CompletedTask;
                });

                context.CancellationToken.Register(() => subscription.UnsubscribeAsync().GetAwaiter().GetResult());
                await Task.Delay(Timeout.Infinite, context.CancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
            else
            {
                observer.OnError(new NotSupportedException("Server-side reducer watching is not yet supported. Reducers typically run client-side."));
            }
        });
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModelIdentifier, request.EventStore);
        var definition = await readModel.GetDefinition();

        if (definition.ObserverType == Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            var projectionKey = new ImmediateProjectionKey(
                (ProjectionId)definition.ObserverIdentifier.Value,
                request.EventStore,
                request.Namespace,
                request.EventSequenceId,
                request.ReadModelKey,
                (ProjectionSessionId)Guid.Parse(request.SessionId));

            var projection = grainFactory.GetGrain<IImmediateProjection>(projectionKey);
            await projection.Dehydrate();
        }
        else
        {
            throw new NotSupportedException("Server-side reducer session dehydration is not yet supported. Reducers typically run client-side.");
        }
    }

    async Task<Concepts.ReadModels.ReadModelDefinition> WaitForReadModelDefinition(IReadModel readModel, CancellationToken cancellationToken)
    {
        const int maxRetries = 50;
        const int delayMs = 100;

        for (var i = 0; i < maxRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var definition = await readModel.GetDefinition();
            if (!string.IsNullOrEmpty(definition.ObserverIdentifier))
            {
                return definition;
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        throw new InvalidOperationException($"Read model definition not registered within {maxRetries * delayMs}ms. Ensure the read model is registered before watching.");
    }

    async Task<IEnumerable<ReadModelSnapshot>> GetSnapshotsForProjection(
        string projectionId,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        string readModelKey)
    {
        var eventSequenceStorage = storage
            .GetEventStore(eventStoreName)
            .GetNamespace(namespaceName)
            .GetEventSequence(eventSequenceId);

        var projectionKey = new ProjectionKey(projectionId, eventStoreName);
        var projection = grainFactory.GetGrain<IProjection>(projectionKey);
        var definition = await projection.GetDefinition();
        var readModelDefinition = await storage.GetEventStore(eventStoreName).ReadModels.Get(definition.ReadModel);
        var eventTypes = await projection.GetEventTypes();
        var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, readModelKey, eventTypes: eventTypes);

        var eventsByCorrelation = new Dictionary<Guid, List<AppendedEvent>>();

        while (await cursor.MoveNext())
        {
            if (!cursor.Current.Any())
            {
                break;
            }

            foreach (var appendedEvent in cursor.Current)
            {
                var correlationId = appendedEvent.Context.CorrelationId;
                if (!eventsByCorrelation.ContainsKey(correlationId))
                {
                    eventsByCorrelation[correlationId] = [];
                }
                eventsByCorrelation[correlationId].Add(appendedEvent);
            }
        }

        var snapshots = new List<ReadModelSnapshot>();
        var initialState = new ExpandoObject();

        foreach (var (correlationId, events) in eventsByCorrelation)
        {
            var orderedEvents = events.OrderBy(e => e.Context.SequenceNumber).ToList();
            var firstOccurred = orderedEvents[0].Context.Occurred;

            var result = await projection.ProcessForSingleReadModel(namespaceName, initialState, orderedEvents);
            var jsonObject = expandoObjectConverter.ToJsonObject(result, readModelDefinition.GetSchemaForLatestGeneration());
            var readModel = JsonSerializer.Serialize(jsonObject, jsonSerializerOptions);
            initialState = result;

            snapshots.Add(new ReadModelSnapshot
            {
                ReadModel = readModel,
                Events = orderedEvents.ToContract(jsonSerializerOptions),
                Occurred = firstOccurred!,
                CorrelationId = correlationId
            });
        }

        cursor.Dispose();
        return snapshots;
    }
}
