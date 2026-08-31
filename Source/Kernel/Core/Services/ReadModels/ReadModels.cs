// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;
using AppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using ReadModelSnapshot = Cratis.Chronicle.Contracts.ReadModels.ReadModelSnapshot;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="storage">The storage.</param>
/// <param name="expandoObjectConverter">The expando object converter.</param>
/// <param name="reducerMediator">The reducer mediator.</param>
/// <param name="changesetMediator">The <see cref="IProjectionChangesetMediator"/> for forwarding watched changesets to client streams.</param>
/// <param name="localSiloDetails">The <see cref="ILocalSiloDetails"/> for pinning the watch subscriber grain to this silo.</param>
/// <param name="complianceHelper">The <see cref="IReadModelsCompliance"/> for decrypting PII fields.</param>
/// <param name="eventCompliance">The <see cref="IEventCompliance"/> for decrypting PII event content.</param>
/// <param name="materializedReadModels">The <see cref="IMaterializedReadModelStore"/> for reading instances that are already materialized.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
internal sealed class ReadModels(
    IGrainFactory grainFactory,
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    IReducerMediator reducerMediator,
    IProjectionChangesetMediator changesetMediator,
    ILocalSiloDetails localSiloDetails,
    IReadModelsCompliance complianceHelper,
    IEventCompliance eventCompliance,
    IMaterializedReadModelStore materializedReadModels,
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
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { (ReadModelGeneration)request.ReadModel.Type.Generation, schema }
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

        // A materialized read model — projection or reducer alike — already has its state written to the sink by
        // its observer, so read it from there rather than re-projecting or round-tripping to a connected reducer
        // client. For a projection this is also the only path that reflects joins and custom key resolvers
        // (UsingKey), because ImmediateProjection replays by EventSourceId alone and misses cross-source events.
        // A session pins a projection to an in-flight state, and an unspecified key ("*") cannot be looked up by
        // key at all, so both of those keep replaying.
        if (materializedReadModels.IsMaterialized(definition) &&
            string.IsNullOrEmpty(request.SessionId) &&
            request.ReadModelKey != ReadModelKey.Unspecified.Value)
        {
            return await GetMaterializedInstanceByKey(request, definition);
        }

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

            if (!result.HasReadModel)
            {
                return new GetInstanceByKeyResponse
                {
                    ReadModel = "null",
                    ProjectedEventsCount = (ulong)result.ProjectedEventsCount,
                    LastHandledEventSequenceNumber = result.LastHandledEventSequenceNumber
                };
            }

            var immediateSchema = definition.GetSchemaForLatestGeneration();
            var releasedReadModel = await ReleaseProjectedReadModel(
                result.ReadModel,
                immediateSchema,
                request.EventStore,
                request.Namespace,
                request.ReadModelKey);

            return new GetInstanceByKeyResponse
            {
                ReadModel = releasedReadModel.ToJsonString(jsonSerializerOptions),
                ProjectedEventsCount = (ulong)result.ProjectedEventsCount,
                LastHandledEventSequenceNumber = result.LastHandledEventSequenceNumber
            };
        }

        var reducerContext = await GetConnectedReducerContext(definition, request.EventStore, request.Namespace, request.EventSequenceId);
        var reducerEvents = await GetEventsForReducer(
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            eventSourceId: request.ReadModelKey,
            eventTypes: reducerContext.EventTypes);

        var reduceResult = await ReduceWithConnectedClient(
            reducerContext.ReducerId,
            reducerContext.ConnectionId,
            request.EventStore,
            request.Namespace,
            request.ReadModelKey,
            reducerEvents,
            initialState: null);

        if (reduceResult.ReadModelState is null)
        {
            return new GetInstanceByKeyResponse
            {
                ReadModel = "{}",
                ProjectedEventsCount = (ulong)reducerEvents.Count,
                LastHandledEventSequenceNumber = reduceResult.ObserverResult.LastSuccessfulObservation
            };
        }

        var readModelSchema = (await storage.GetEventStore(request.EventStore).ReadModels.Get(definition.Identifier)).GetSchemaForLatestGeneration();
        var decrypted = await complianceHelper.Release(
            request.EventStore,
            request.Namespace,
            readModelSchema,
            reduceResult.ReadModelState);

        return new GetInstanceByKeyResponse
        {
            ReadModel = expandoObjectConverter.ToJsonObject(decrypted, readModelSchema).ToJsonString(jsonSerializerOptions),
            ProjectedEventsCount = (ulong)reducerEvents.Count,
            LastHandledEventSequenceNumber = reduceResult.ObserverResult.LastSuccessfulObservation
        };
    }

    /// <inheritdoc/>
    public async Task<GetAllInstancesResponse> GetAllInstances(GetAllInstancesRequest request, CallContext context = default)
    {
        var readModel = grainFactory.GetReadModel(request.ReadModelIdentifier, request.EventStore);
        var definition = await readModel.GetDefinition();

        // Every instance of a materialized read model is already in the sink, so read them from there. An
        // explicit event count is a request to re-apply exactly that many events from the beginning, which
        // only the replay path below can answer.
        if (materializedReadModels.IsMaterialized(definition) && request.EventCount == ulong.MaxValue)
        {
            return await GetAllMaterializedInstances(request, definition);
        }

        if (definition.ObserverType != Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            var reducerContext = await GetConnectedReducerContext(definition, request.EventStore, request.Namespace, request.EventSequenceId);
            var reducerEvents = await GetEventsForReducer(
                request.EventStore,
                request.Namespace,
                request.EventSequenceId,
                eventTypes: reducerContext.EventTypes,
                eventCount: request.EventCount);

            var reducerReadModelDefinition = await storage.GetEventStore(request.EventStore).ReadModels.Get(definition.Identifier);
            var readModelSchema = reducerReadModelDefinition.GetSchemaForLatestGeneration();
            var reducedReadModels = new List<string>();

            var orderedReducerEvents = reducerEvents.OrderBy(@event => @event.Context.SequenceNumber).ToList();
            foreach (var eventsForPartition in orderedReducerEvents
                         .GroupBy(@event => @event.Context.EventSourceId)
                         .Select(group => group.ToList()))
            {
                var eventSourceId = eventsForPartition[0].Context.EventSourceId;
                var reduceResult = await ReduceWithConnectedClient(
                    reducerContext.ReducerId,
                    reducerContext.ConnectionId,
                    request.EventStore,
                    request.Namespace,
                    eventSourceId,
                    eventsForPartition,
                    initialState: null);

                if (reduceResult.ReadModelState is null)
                {
                    continue;
                }

                var dictionary = (IDictionary<string, object?>)reduceResult.ReadModelState;
                var subject = GetOrInferSubject(dictionary);
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    dictionary[WellKnownProperties.Subject] = subject;
                }

                var decrypted = await complianceHelper.Release(
                    request.EventStore,
                    request.Namespace,
                    readModelSchema,
                    reduceResult.ReadModelState);

                reducedReadModels.Add(expandoObjectConverter.ToJsonObject(decrypted, readModelSchema).ToJsonString(jsonSerializerOptions));
            }

            return new GetAllInstancesResponse
            {
                Instances = reducedReadModels,
                ProcessedEventsCount = (ulong)reducerEvents.Count
            };
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
            using var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: null, eventTypes: eventTypes);
            while (await cursor.MoveNext())
            {
                if (!cursor.Current.Any())
                {
                    break;
                }
                events.AddRange(cursor.Current);
            }
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
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        var readModels = new List<string>();
        foreach (var instance in result)
        {
            var dictionary = (IDictionary<string, object?>)instance;
            var subject = GetOrInferSubject(dictionary);
            if (!string.IsNullOrWhiteSpace(subject))
            {
                dictionary[WellKnownProperties.Subject] = subject;
            }

            var decrypted = await complianceHelper.Release(
                request.EventStore,
                request.Namespace,
                schema,
                instance);

            readModels.Add(SerializeInstance(decrypted, schema));
        }

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
                var schema = definition.GetSchemaForLatestGeneration();
                var subscriptionId = Guid.NewGuid();

                // Register the forwarder that decrypts a changeset and pushes it onto this client's gRPC
                // stream into this silo's changeset mediator. The per-watch subscriber grain below is
                // pinned to this silo, so the notifier -> subscriber -> mediator -> stream path ends with
                // a strictly in-process hop. This mirrors the reducer/reactor delivery pattern and
                // replaces the previous CreateObjectReference grain-observer callback, whose one-way
                // dispatch from the notifier grain was silently dropped on slower backends.
                changesetMediator.Subscribe(subscriptionId, async (namespaceName, readModelKey, readModelInstance, change) =>
                {
                    // Decrypt through the shared release path so observable queries resolve the compliance
                    // subject exactly like one-shot queries (explicit __subject, else inferred from _id/id).
                    var decrypted = await ReleaseJsonForProjectedReadModel(request.EventStore, namespaceName, schema, readModelInstance);
                    observer.OnNext(new ReadModelChangeset
                    {
                        Namespace = namespaceName,
                        ModelKey = readModelKey,
                        ReadModel = decrypted.ToJsonString(jsonSerializerOptions),
                        Removed = change.ChangeType == Concepts.ReadModels.ReadModelChangeType.Removed,
                        ChangeType = change.ChangeType switch
                        {
                            Concepts.ReadModels.ReadModelChangeType.Added => Contracts.ReadModels.ReadModelChangeType.Added,
                            Concepts.ReadModels.ReadModelChangeType.Removed => Contracts.ReadModels.ReadModelChangeType.Removed,
                            _ => Contracts.ReadModels.ReadModelChangeType.Modified
                        },
                        EventSequenceNumber = change.EventSequenceNumber.Value,
                        Occurred = change.Occurred,
                        CorrelationId = change.CorrelationId.Value
                    });
                });

                // The subscriber grain carries this silo's address in its key so [ConnectedObserverPlacement]
                // pins it here; the subscription id travels in the event source id slot. The notifier reaches
                // it as an ordinary — reliably routed — grain reference rather than an object reference.
                var subscriberKey = new ObserverSubscriberKey(
                    new ObserverId(definition.ObserverIdentifier.Value),
                    request.EventStore,
                    request.Namespace,
                    request.EventSequenceId,
                    subscriptionId.ToString("N"),
                    localSiloDetails.SiloAddress.ToParsableString());
                var subscriber = grainFactory.GetGrain<IReadModelChangesetSubscriber>(subscriberKey);
                var notifier = grainFactory.GetGrain<IProjectionChangesetNotifier>(definition.ObserverIdentifier);
                await notifier.Subscribe(subscriber);

                try
                {
                    // Notify the client that the watch subscription is now active. Any changeset produced
                    // from this point on reaches the forwarder registered above.
                    observer.OnNext(new ReadModelChangeset { Subscribed = true });

                    await Task.Delay(Timeout.Infinite, context.CancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
                finally
                {
                    await notifier.Unsubscribe(subscriber).ConfigureAwait(false);
                    changesetMediator.Unsubscribe(subscriptionId);
                }
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

    static EventSequenceNumber GetLastHandledEventSequenceNumber(ExpandoObject instance)
    {
        var values = (IDictionary<string, object?>)instance;
        if (!values.TryGetValue(WellKnownProperties.LastHandledEventSequenceNumber, out var value) || value is null)
        {
            return EventSequenceNumber.Unavailable;
        }

        try
        {
            return (EventSequenceNumber)Convert.ToUInt64(value);
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            // The stored value is not a sequence number; report it as unavailable rather than failing the read.
            return EventSequenceNumber.Unavailable;
        }
    }

    async Task<GetInstanceByKeyResponse> GetMaterializedInstanceByKey(GetInstanceByKeyRequest request, Concepts.ReadModels.ReadModelDefinition definition)
    {
        var key = new Key(request.ReadModelKey, ArrayIndexers.NoIndexers);
        var instance = await materializedReadModels.FindByKey(request.EventStore, request.Namespace, definition, key);

        // Nothing in the sink means the instance was either never created or removed. The observer is
        // authoritative for a materialized read model, so answer with a null model rather than falling back to
        // a replay that would resurrect it.
        if (instance is null)
        {
            return new GetInstanceByKeyResponse
            {
                ReadModel = "null",
                ProjectedEventsCount = 0,
                LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable
            };
        }

        var jsonObject = expandoObjectConverter.ToJsonObject(instance, definition.GetSchemaForLatestGeneration());

        return new GetInstanceByKeyResponse
        {
            ReadModel = jsonObject.ToJsonString(jsonSerializerOptions),
            ProjectedEventsCount = 0,
            LastHandledEventSequenceNumber = GetLastHandledEventSequenceNumber(instance)
        };
    }

    async Task<GetAllInstancesResponse> GetAllMaterializedInstances(GetAllInstancesRequest request, Concepts.ReadModels.ReadModelDefinition definition)
    {
        var instances = await materializedReadModels.GetAllInstances(request.EventStore, request.Namespace, definition);
        var schema = definition.GetSchemaForLatestGeneration();

        return new GetAllInstancesResponse
        {
            Instances = instances.Select(instance => SerializeInstance(instance, schema)).ToList(),
            ProcessedEventsCount = 0
        };
    }

    string SerializeInstance(ExpandoObject instance, JsonSchema schema)
    {
        var jsonObject = expandoObjectConverter.ToJsonObject(instance, schema);

        // ToJsonObject drops the last handled event sequence number when the schema does not describe it, so
        // put it back — clients mirror it onto the instance to tell how far it has been brought up to date.
        var lastHandled = GetLastHandledEventSequenceNumber(instance);
        if (lastHandled != EventSequenceNumber.Unavailable)
        {
            jsonObject[WellKnownProperties.LastHandledEventSequenceNumber] = JsonValue.Create(lastHandled.Value);
        }

        return jsonObject.ToJsonString(jsonSerializerOptions);
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

    /// <summary>
    /// Reads the events that shaped one read model instance, decrypted and in order.
    /// </summary>
    /// <remarks>
    /// A read model's key is only the event source id when the projection does not say otherwise.
    /// Where it does - a projection keyed on an event property, say - narrowing the sequence by event
    /// source finds nothing, and the instance looks like it has no history at all. Narrowing stays
    /// the fast path for the projections it is actually right for; the rest read the sequence and let
    /// the projection say which events resolved to this key.
    /// </remarks>
    /// <param name="projectionId">The projection that produces the read model.</param>
    /// <param name="eventStoreName">The event store the read model lives in.</param>
    /// <param name="namespaceName">The namespace the read model lives in.</param>
    /// <param name="eventSequenceId">The event sequence to read from.</param>
    /// <param name="readModelKey">The key of the instance to read the history of.</param>
    /// <returns>The events behind the instance, with what is needed to project them.</returns>
    async Task<ProjectedReadModelHistory> GetHistoryForProjection(
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

        var keyIsEventSourceId = definition.From.Values.All(from => from.Key is null || string.IsNullOrEmpty(from.Key.Value));

        var cursor = keyIsEventSourceId
            ? await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, readModelKey, eventTypes: eventTypes)
            : await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: eventTypes);

        var allEvents = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            allEvents.AddRange(cursor.Current);
        }
        cursor.Dispose();

        if (!keyIsEventSourceId)
        {
            allEvents = (await projection.GetEventsForKey(namespaceName, readModelKey, allEvents)).ToList();
        }

        // Decrypt the stored events before projecting and returning them - both the projected read
        // model and the events it carries must be released so no PII leaves encrypted.
        var eventTypeSchemas = await storage.GetEventStore(eventStoreName).EventTypes.GetFor(allEvents.Select(_ => _.Context.EventType).Distinct());
        var releasedEvents = await eventCompliance.Release(allEvents, eventTypeSchemas.ToDictionary(_ => _.Type));

        return new(projection, readModelDefinition, [.. releasedEvents.OrderBy(_ => _.Context.SequenceNumber)]);
    }

    async Task<IEnumerable<ReadModelSnapshot>> GetSnapshotsForProjection(
        string projectionId,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        string readModelKey)
    {
        var history = await GetHistoryForProjection(projectionId, eventStoreName, namespaceName, eventSequenceId, readModelKey);

        var eventsByCorrelation = new Dictionary<Guid, List<AppendedEvent>>();
        foreach (var appendedEvent in history.Events)
        {
            var correlationId = appendedEvent.Context.CorrelationId;
            if (!eventsByCorrelation.TryGetValue(correlationId, out var eventsForCorrelation))
            {
                eventsForCorrelation = [];
                eventsByCorrelation[correlationId] = eventsForCorrelation;
            }
            eventsForCorrelation.Add(appendedEvent);
        }

        var snapshots = new List<ReadModelSnapshot>();
        var initialState = new ExpandoObject();

        foreach (var (correlationId, events) in eventsByCorrelation)
        {
            var orderedEvents = events.OrderBy(e => e.Context.SequenceNumber).ToList();
            var firstOccurred = orderedEvents[0].Context.Occurred;

            var result = await history.Projection.ProcessForSingleReadModel(namespaceName, initialState, orderedEvents);
            initialState = result;

            snapshots.Add(new ReadModelSnapshot
            {
                ReadModel = SerializeReadModel(result, history.ReadModelDefinition),
                Events = orderedEvents.ToContract(jsonSerializerOptions),
                Occurred = firstOccurred,
                CorrelationId = correlationId
            });
        }

        return snapshots;
    }

    string SerializeReadModel(ExpandoObject state, Concepts.ReadModels.ReadModelDefinition readModelDefinition)
    {
        var jsonObject = expandoObjectConverter.ToJsonObject(state, readModelDefinition.GetSchemaForLatestGeneration());
        return JsonSerializer.Serialize(jsonObject, jsonSerializerOptions);
    }

    async Task<ConnectedReducerContext> GetConnectedReducerContext(
        Concepts.ReadModels.ReadModelDefinition definition,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId)
    {
        var reducerId = (ReducerId)definition.ObserverIdentifier.Value;
        var observer = grainFactory.GetGrain<IObserver>(new ObserverKey(reducerId, eventStoreName, namespaceName, eventSequenceId));
        var subscription = await observer.GetSubscription();
        if (!subscription.IsSubscribed || subscription.Arguments is not ConnectedClient connectedClient)
        {
            throw new NotSupportedException($"Reducer '{reducerId}' is not connected. Reducer read model retrieval requires an active connected client.");
        }

        var eventTypes = await observer.GetEventTypes();
        return new ConnectedReducerContext(reducerId, connectedClient.ConnectionId, eventTypes);
    }

    async Task<List<AppendedEvent>> GetEventsForReducer(
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? eventTypes = default,
        ulong? eventCount = default)
    {
        var eventSequenceStorage = storage
            .GetEventStore(eventStoreName)
            .GetNamespace(namespaceName)
            .GetEventSequence(eventSequenceId);

        var events = new List<AppendedEvent>();
        if (eventCount is null or ulong.MaxValue)
        {
            using var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: eventSourceId, eventTypes: eventTypes);
            while (await cursor.MoveNext())
            {
                events.AddRange(cursor.Current);
            }
        }
        else
        {
            if (eventCount.Value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(eventCount), $"Event count '{eventCount.Value}' exceeds maximum supported value '{int.MaxValue}' for reducer retrieval.");
            }

            var limit = (int)eventCount.Value;
            using var cursor = await eventSequenceStorage.GetEventsWithLimit(EventSequenceNumber.First, limit, eventSourceId: eventSourceId, eventTypes: eventTypes);
            while (await cursor.MoveNext())
            {
                events.AddRange(cursor.Current);
            }
        }

        return events;
    }

    async Task<ReducerSubscriberResult> ReduceWithConnectedClient(
        ReducerId reducerId,
        ConnectionId connectionId,
        string eventStoreName,
        string namespaceName,
        Key partition,
        IEnumerable<AppendedEvent> events,
        ExpandoObject? initialState)
    {
        var tcs = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        reducerMediator.OnNext(
            reducerId,
            connectionId,
            eventStoreName,
            namespaceName,
            new ReduceOperation(partition, events, initialState),
            tcs);

        var reduceResult = await tcs.Task;
        if (reduceResult.ObserverResult.State != ObserverSubscriberState.Ok)
        {
            var exceptionMessage = string.Join(Environment.NewLine, reduceResult.ObserverResult.ExceptionMessages);
            throw new InvalidOperationException($"Failed to reduce read model. {exceptionMessage}".TrimEnd());
        }

        return reduceResult;
    }

    string? GetOrInferSubject(IDictionary<string, object?> instance)
    {
        if (instance.TryGetValue(WellKnownProperties.Subject, out var subject) && subject is not null)
        {
            return subject.ToString();
        }

        if (instance.TryGetValue("_id", out var identifier) && identifier is not null)
        {
            return identifier.ToString();
        }

        if (instance.TryGetValue("id", out identifier) && identifier is not null)
        {
            return identifier.ToString();
        }

        return null;
    }

    Task<JsonObject> ReleaseProjectedReadModel(JsonObject readModel, JsonSchema schema, string eventStore, string @namespace, string? subject) =>
        ReleaseJsonForProjectedReadModel(eventStore, @namespace, schema, readModel, subject);

    async Task<JsonObject> ReleaseJsonForProjectedReadModel(
        string eventStore,
        string @namespace,
        JsonSchema schema,
        JsonObject readModel,
        string? preferredSubject = null)
    {
        // A read model projected directly from stored (encrypted) events still holds its PII fields encrypted under
        // the compliance subject. The subject is resolved identically for one-shot and observable queries — an
        // explicit subject when the caller supplies one, otherwise inferred from the document (__subject -> _id ->
        // id) — stamped so the compliance manager can decrypt, then stripped again so the internal marker never
        // leaves the kernel. Sharing this between the one-shot query path and the observable (Watch) path keeps them
        // from diverging: observable queries used to skip the inference entirely and streamed a __subject-less
        // document back as ciphertext.
        //
        // The document handed in is never modified. On the observable path it belongs to the changeset the notifier
        // pushed, not to this call, so stamping bookkeeping onto it would leave an internal marker on an object
        // this method does not own.
        if (!schema.HasComplianceMetadata())
        {
            return readModel;
        }

        var resolvedSubject = !string.IsNullOrWhiteSpace(preferredSubject) && preferredSubject != ReadModelKey.Unspecified.Value
            ? preferredSubject
            : InferSubjectFromJson(readModel);
        if (string.IsNullOrWhiteSpace(resolvedSubject))
        {
            return readModel;
        }

        var stamped = (readModel.DeepClone() as JsonObject)!;
        stamped[WellKnownProperties.Subject] = resolvedSubject;

        // The strip stays even though the compliance manager releases onto a clone that never carried the marker:
        // an implementation that hands back the instance it was given returns the stamped document, and the marker
        // must never reach the client.
        var released = await complianceHelper.ReleaseJson(eventStore, @namespace, schema, stamped);
        released.Remove(WellKnownProperties.Subject);
        return released;

        static string? InferSubjectFromJson(JsonObject json)
        {
            foreach (var property in new[] { WellKnownProperties.Subject, "_id", "id" })
            {
                if (json.TryGetPropertyValue(property, out var value) && value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var identifier))
                {
                    return identifier;
                }
            }

            return null;
        }
    }

    record ConnectedReducerContext(ReducerId ReducerId, ConnectionId ConnectionId, IEnumerable<EventType> EventTypes);

    /// <summary>
    /// The events behind one read model instance, with what is needed to project them.
    /// </summary>
    /// <param name="Projection">The projection that produces the read model.</param>
    /// <param name="ReadModelDefinition">The read model's definition, for serializing against its schema.</param>
    /// <param name="Events">The events that shaped the instance, oldest first and decrypted.</param>
    record ProjectedReadModelHistory(
        IProjection Projection,
        Concepts.ReadModels.ReadModelDefinition ReadModelDefinition,
        IReadOnlyList<AppendedEvent> Events);
}
