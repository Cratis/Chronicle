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
using Cratis.Chronicle.Concepts.Sinks;
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
/// <param name="complianceHelper">The <see cref="IReadModelsCompliance"/> for decrypting PII fields.</param>
/// <param name="eventCompliance">The <see cref="IEventCompliance"/> for decrypting PII event content.</param>
/// <param name="jsonSerializerOptions">The JSON serializer options.</param>
internal sealed class ReadModels(
    IGrainFactory grainFactory,
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    IReducerMediator reducerMediator,
    IReadModelsCompliance complianceHelper,
    IEventCompliance eventCompliance,
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

        if (definition.ObserverType == Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            // When no session is active, try to read from the sink first (the stored projected value).
            // This correctly handles joins and events with custom key resolvers (UsingKey) because
            // the projection observer processes all events and stores the result in the sink.
            // ImmediateProjection only replays events by EventSourceId, which misses cross-source events.
            // Note: when ReadModelKey is unspecified ("*") we cannot look it up by key in the sink;
            // fall through to ImmediateProjection which replays all events and returns the last state.
            if (string.IsNullOrEmpty(request.SessionId) && request.ReadModelKey != ReadModelKey.Unspecified.Value)
            {
                var namespaceStorage = storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace);
                var sink = await namespaceStorage.Sinks.GetFor(definition);

                // For passive projections the sink never has data; fall through to immediate projection.
                if (sink.TypeId != SinkTypeId.None)
                {
                    var key = new Key(request.ReadModelKey, ArrayIndexers.NoIndexers);
                    var storedState = await sink.FindOrDefault(key);

                    if (storedState is not null)
                    {
                        var schema = definition.GetSchemaForLatestGeneration();
                        var released = await complianceHelper.Release(
                            request.EventStore,
                            request.Namespace,
                            schema,
                            storedState);
                        var jsonObject = expandoObjectConverter.ToJsonObject(released, schema);
                        var readModelJson = jsonObject.ToJsonString(jsonSerializerOptions);

                        var lastSeq = EventSequenceNumber.Unavailable;
                        var stateDict = (IDictionary<string, object?>)released;
                        if (stateDict.TryGetValue(WellKnownProperties.LastHandledEventSequenceNumber, out var seqObj) &&
                            seqObj is not null)
                        {
                            try { lastSeq = (EventSequenceNumber)Convert.ToUInt64(seqObj); }
                            catch { /* value not convertible to ulong; leave as Unavailable */ }
                        }

                        return new GetInstanceByKeyResponse
                        {
                            ReadModel = readModelJson,
                            ProjectedEventsCount = 0,
                            LastHandledEventSequenceNumber = lastSeq
                        };
                    }

                    // Document not found in the sink: either it was never created or it was removed.
                    // Return a null-model response; the projection observer is authoritative for active sinks.
                    return new GetInstanceByKeyResponse
                    {
                        ReadModel = "null",
                        ProjectedEventsCount = 0,
                        LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable
                    };
                }
            }

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

            var jsonObject = expandoObjectConverter.ToJsonObject(decrypted, schema);

            // Ensure __lastHandledEventSequenceNumber is included in the JSON output since
            // ToJsonObject may drop it if it is not mapped by the schema converter.
            var decryptedDict = (IDictionary<string, object?>)decrypted;
            if (decryptedDict.TryGetValue(WellKnownProperties.LastHandledEventSequenceNumber, out var seqObj) && seqObj is not null)
            {
                try { jsonObject[WellKnownProperties.LastHandledEventSequenceNumber] = JsonValue.Create(Convert.ToUInt64(seqObj)); }
                catch { /* leave sequence number absent if conversion fails */ }
            }

            readModels.Add(jsonObject.ToJsonString(jsonSerializerOptions));
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

                // Direct grain-to-observer notification — replaces Orleans MemoryStreams pub-sub
                // whose subscriber propagation lagged the first publish under load and silently
                // dropped early changesets. The notifier grain dispatches synchronously the moment
                // Subscribe returns, so no warmup or wait is needed between Subscribed and the
                // first appended event.
                var forwardingObserver = new ChangesetForwardingObserver(
                    observer,
                    request.EventStore,
                    schema,
                    complianceHelper,
                    jsonSerializerOptions);
                var observerReference = grainFactory.CreateObjectReference<IProjectionChangesetObserver>(forwardingObserver);
                var notifier = grainFactory.GetGrain<IProjectionChangesetNotifier>(definition.ObserverIdentifier);
                await notifier.Subscribe(observerReference);

                try
                {
                    // Notify the client that the changeset notifier subscription is now active.
                    // Direct grain dispatch means any changeset produced from this point on will
                    // reach the forwarding observer below.
                    observer.OnNext(new ReadModelChangeset { Subscribed = true });

                    await Task.Delay(Timeout.Infinite, context.CancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
                finally
                {
                    await notifier.Unsubscribe(observerReference).ConfigureAwait(false);
                    grainFactory.DeleteObjectReference<IProjectionChangesetObserver>(observerReference);
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

        var allEvents = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            allEvents.AddRange(cursor.Current);
        }
        cursor.Dispose();

        // Decrypt the stored events before projecting and returning them — both the snapshot read
        // model and the events it carries must be released so no PII leaves encrypted.
        var eventTypeSchemas = await storage.GetEventStore(eventStoreName).EventTypes.GetFor(allEvents.Select(_ => _.Context.EventType).Distinct());
        var releasedEvents = await eventCompliance.Release(allEvents, eventTypeSchemas.ToDictionary(_ => _.Type));

        var eventsByCorrelation = new Dictionary<Guid, List<AppendedEvent>>();
        foreach (var appendedEvent in releasedEvents)
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

            var result = await projection.ProcessForSingleReadModel(namespaceName, initialState, orderedEvents);
            var jsonObject = expandoObjectConverter.ToJsonObject(result, readModelDefinition.GetSchemaForLatestGeneration());
            var readModel = JsonSerializer.Serialize(jsonObject, jsonSerializerOptions);
            initialState = result;

            snapshots.Add(new ReadModelSnapshot
            {
                ReadModel = readModel,
                Events = orderedEvents.ToContract(jsonSerializerOptions),
                Occurred = firstOccurred,
                CorrelationId = correlationId
            });
        }

        return snapshots;
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

    async Task<JsonObject> ReleaseProjectedReadModel(JsonObject readModel, JsonSchema schema, string eventStore, string @namespace, string? subject)
    {
        if (!schema.HasComplianceMetadata())
        {
            return readModel;
        }

        // The read model is projected directly from stored (encrypted) events, so its PII fields still
        // hold the encrypted value keyed by the event source id. Stamp the subject so the compliance
        // manager can decrypt them, then strip it again so the internal marker never leaves the kernel.
        var resolvedSubject = !string.IsNullOrWhiteSpace(subject) && subject != ReadModelKey.Unspecified.Value
            ? subject
            : InferSubjectFromJson();
        if (string.IsNullOrWhiteSpace(resolvedSubject))
        {
            return readModel;
        }

        string? InferSubjectFromJson()
        {
            foreach (var property in new[] { WellKnownProperties.Subject, "_id", "id" })
            {
                if (readModel.TryGetPropertyValue(property, out var value) && value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var identifier))
                {
                    return identifier;
                }
            }

            return null;
        }

        readModel[WellKnownProperties.Subject] = resolvedSubject;
        var released = await complianceHelper.ReleaseJson(eventStore, @namespace, schema, readModel);
        released.Remove(WellKnownProperties.Subject);
        return released;
    }

    sealed class ChangesetForwardingObserver(
        IObserver<ReadModelChangeset> observer,
        string eventStore,
        JsonSchema schema,
        IReadModelsCompliance complianceHelper,
        JsonSerializerOptions jsonSerializerOptions) : IProjectionChangesetObserver
    {
        public async Task OnChangeset(Concepts.EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change)
        {
            var decrypted = await complianceHelper.ReleaseJson(
                eventStore,
                namespaceName,
                schema,
                readModel);

            observer.OnNext(new ReadModelChangeset
            {
                Namespace = namespaceName,
                ModelKey = readModelKey,
                ReadModel = decrypted.ToJsonString(jsonSerializerOptions),
                Removed = change.ChangeType == Concepts.ReadModels.ReadModelChangeType.Removed,
                ChangeType = ToContractChangeType(change.ChangeType),
                EventSequenceNumber = change.EventSequenceNumber.Value,
                Occurred = change.Occurred,
                CorrelationId = change.CorrelationId.Value
            });
        }

        static Contracts.ReadModels.ReadModelChangeType ToContractChangeType(Concepts.ReadModels.ReadModelChangeType changeType) => changeType switch
        {
            Concepts.ReadModels.ReadModelChangeType.Added => Contracts.ReadModels.ReadModelChangeType.Added,
            Concepts.ReadModels.ReadModelChangeType.Removed => Contracts.ReadModels.ReadModelChangeType.Removed,
            _ => Contracts.ReadModels.ReadModelChangeType.Modified
        };
    }

    record ConnectedReducerContext(ReducerId ReducerId, ConnectionId ConnectionId, IEnumerable<EventType> EventTypes);
}
