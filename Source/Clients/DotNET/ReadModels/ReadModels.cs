// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModelExplorer;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="projections">Projections to get read models from.</param>
/// <param name="reducers">Reducers to get read models from.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for resolving event types.</param>
/// <param name="schemaGenerator">Schema generator to use.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> for Chronicle configuration.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for JSON serialization.</param>
/// <param name="readModelWatcherManager"><see cref="IReadModelWatcherManager"/> for managing watchers.</param>
/// <param name="reducerObservers"><see cref="IReducerObservers"/> for managing reducer observers.</param>
/// <param name="materializedReadModels">The <see cref="IMaterializedReadModels"/> for materialized read model operations.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
public class ReadModels(
    IEventStore eventStore,
    INamingPolicy namingPolicy,
    IProjections projections,
    IReducers reducers,
    IEventTypes eventTypes,
    IJsonSchemaGenerator schemaGenerator,
    IOptions<ChronicleOptions> options,
    JsonSerializerOptions jsonSerializerOptions,
    IReadModelWatcherManager readModelWatcherManager,
    IReducerObservers reducerObservers,
    IMaterializedReadModels materializedReadModels,
    ILogger<ReadModels> logger) : IReadModels
{
    readonly IChronicleServicesAccessor _chronicleServicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
    readonly SinkTypeId _defaultSinkTypeId = options.Value.DefaultSinkTypeId;
    readonly ReadModelReleaser _releaser = new(
        eventStore,
        schemaGenerator,
        (eventStore.Connection as IChronicleServicesAccessor)!,
        jsonSerializerOptions,
        logger);

    /// <summary>
    /// Gets the <see cref="IMaterializedReadModels"/> for working with materialized read model instances.
    /// </summary>
    public IMaterializedReadModels Materialized => materializedReadModels;

    /// <inheritdoc/>
    public async Task Register()
    {
        var readModels = new List<IHaveReadModel>();

        readModels.AddRange(projections.GetAllHandlers());
        readModels.AddRange(reducers.GetAllHandlers());

        var readModelDefinitions = readModels.ConvertAll(readModel =>
        {
            var observerType = readModel switch
            {
                IProjectionHandler => ReadModelObserverType.Projection,
                IReducerHandler => ReadModelObserverType.Reducer,
                _ => ReadModelObserverType.Projection
            };

            var observerIdentifier = readModel switch
            {
                IProjectionHandler projectionHandler => projectionHandler.Id.Value,
                IReducerHandler reducerHandler => reducerHandler.Id.Value,
                _ => string.Empty
            };

            return new ReadModelDefinition
            {
                Type = new()
                {
                    Identifier = readModel.ReadModelType.GetReadModelIdentifier(),
                    Generation = ReadModelGeneration.First,
                },
                ContainerName = namingPolicy.GetReadModelName(readModel.ReadModelType),
                DisplayName = readModel.ReadModelType.Name,
                Sink = new()
                {
                    ConfigurationId = Guid.Empty,
                    TypeId = GetSinkTypeIdFor(readModel.ReadModelType)
                },
                Schema = schemaGenerator.Generate(readModel.ReadModelType).ToJson(),
                Indexes = GetIndexesForType(readModel.ReadModelType, string.Empty),
                ObserverType = observerType,
                ObserverIdentifier = observerIdentifier
            };
        });

        await _chronicleServicesAccessor.Services.ReadModels.RegisterMany(new RegisterManyRequest
        {
            EventStore = eventStore.Name,
            Owner = ReadModelOwner.Client,
            ReadModels = readModelDefinitions
        });
    }

    /// <inheritdoc/>
    public async Task Register<TReadModel>()
    {
        var observerType = ReadModelObserverType.Projection;
        var observerIdentifier = string.Empty;

        if (projections.HasFor<TReadModel>())
        {
            var handler = projections.GetAllHandlers().FirstOrDefault(h => h.ReadModelType == typeof(TReadModel));
            if (handler is IProjectionHandler projectionHandler)
            {
                observerType = ReadModelObserverType.Projection;
                observerIdentifier = projectionHandler.Id.Value;
            }
        }
        else if (reducers.HasFor<TReadModel>())
        {
            var handler = reducers.GetAllHandlers().FirstOrDefault(h => h.ReadModelType == typeof(TReadModel));
            if (handler is IReducerHandler reducerHandler)
            {
                observerType = ReadModelObserverType.Reducer;
                observerIdentifier = reducerHandler.Id.Value;
            }
        }

        var readModelDefinitions = new List<ReadModelDefinition>()
        {
            new()
            {
                Type = new()
                {
                    Identifier = typeof(TReadModel).GetReadModelIdentifier(),
                    Generation = ReadModelGeneration.First,
                },
                ContainerName = namingPolicy.GetReadModelName(typeof(TReadModel)),
                DisplayName = typeof(TReadModel).Name,
                Sink = new()
                {
                    ConfigurationId = Guid.Empty,
                    TypeId = GetSinkTypeIdFor(typeof(TReadModel))
                },
                Schema = schemaGenerator.Generate(typeof(TReadModel)).ToJson(),
                Indexes = GetIndexesForType(typeof(TReadModel), string.Empty),
                ObserverType = observerType,
                ObserverIdentifier = observerIdentifier
            }
        };
        await _chronicleServicesAccessor.Services.ReadModels.RegisterMany(new RegisterManyRequest
        {
            EventStore = eventStore.Name,
            Owner = ReadModelOwner.Client,
            ReadModels = readModelDefinitions
        });
    }

    /// <inheritdoc/>
    public async Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        var readModelType = typeof(TReadModel);
        var result = await GetInstanceById(readModelType, key, sessionId);
        var instance = (TReadModel)result;

        // Only an in-process reduce leaves PII encrypted — the Kernel releases whatever it serves from the
        // materialized store.
        if (IsReducedInProcess(readModelType))
        {
            return await Release(instance);
        }

        return instance;
    }

    /// <inheritdoc/>
    public async Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        // Validate that the read model is known by either projections or reducers
        if (!projections.HasFor(readModelType) && !reducers.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        if (IsReducedInProcess(readModelType))
        {
            // An unseeded (never-created) reducer-backed read model has no state yet. Return null — matching
            // the materialized path below that returns default! for a "null" document — rather than
            // throwing, so a nullable caller guard (state?.…) holds uniformly across both backings. A
            // throwing pre-check from a reactor on a passive reducer read model would otherwise freeze the
            // event-source partition permanently.
            var reducedInstance = await reducers.GetInstanceById(readModelType, key);
            return reducedInstance ?? default!;
        }

        var readModelIdentifier = readModelType.GetReadModelIdentifier();

        var request = new GetInstanceByKeyRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModelIdentifier = readModelIdentifier,
            EventSequenceId = GetEventSequenceIdFor(readModelType),
            ReadModelKey = key,
            SessionId = sessionId?.Value.ToString() ?? string.Empty
        };

        var response = await _chronicleServicesAccessor.Services.ReadModels.GetInstanceByKey(request);
        var readModelJson = response.ReadModel;

        // A "null" JSON value means the model was not found in the sink (e.g. removed or never created).
        if (readModelJson == "null")
        {
            return default!;
        }

        // Mirror the __lastHandledEventSequenceNumber that the sink writes so that
        // GetInstanceById results are consistent with sink-stored read models.
        if (response.LastHandledEventSequenceNumber != EventSequenceNumber.Unavailable)
        {
            var jsonNode = JsonNode.Parse(readModelJson);
            if (jsonNode is JsonObject jsonObj)
            {
                jsonObj["__lastHandledEventSequenceNumber"] =
                    JsonValue.Create(response.LastHandledEventSequenceNumber);
                readModelJson = jsonObj.ToJsonString(jsonSerializerOptions);
            }
        }

        var instance = JsonSerializer.Deserialize(readModelJson, readModelType, jsonSerializerOptions);
        return instance ?? throw new InvalidOperationException($"Read model returned null for type '{readModelType.Name}' with key '{key.Value}'");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null)
    {
        var readModelType = typeof(TReadModel);

        // Validate that the read model is known by projections or reducers
        if (!projections.HasFor(readModelType) && !reducers.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        // A bounded replay is a request to apply exactly that many events from the beginning, which the
        // materialized store cannot answer — reduce it in-process, as for a passive read model.
        if (reducers.HasFor(readModelType) && (readModelType.IsPassive() || eventCount is not null))
        {
            var reducerInstances = await reducers.GetInstances(readModelType, eventCount);
            return await Release(reducerInstances.Cast<TReadModel>());
        }

        var readModelIdentifier = readModelType.GetReadModelIdentifier();
        var eventCountValue = eventCount ?? EventCount.Unlimited;

        var request = new GetAllInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModelIdentifier = readModelIdentifier,
            EventSequenceId = GetEventSequenceIdFor(readModelType),
            EventCount = eventCountValue.Value
        };

        var response = await _chronicleServicesAccessor.Services.ReadModels.GetAllInstances(request);
        var instances = response.Instances.Select(json => JsonSerializer.Deserialize<TReadModel>(json, jsonSerializerOptions)!);

        return await Release(instances);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey)
    {
        // Check for projection first
        if (projections.HasFor<TReadModel>())
        {
            var readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();

            return await GetSnapshots<TReadModel>(readModelIdentifier, EventSequenceId.Log, readModelKey);
        }

        // Explicitly check reducers existence using HasReducerFor(Type) to satisfy specs
        if (reducers.HasReducerFor(typeof(TReadModel)))
        {
            // Route via reducers internal retrieval to use correct event sequence
            var concreteReducers = reducers as Reducers.Reducers;
            if (concreteReducers is not null)
            {
                var reducerSnapshots = await concreteReducers.GetSnapshotsById<TReadModel>(readModelKey);
                return await ReleaseSnapshotInstances(reducerSnapshots);
            }

            // Fallback to generic retrieval if not the concrete type
            var readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();
            var handler = reducers.GetHandlerForReadModelType(typeof(TReadModel));

            return await ReleaseSnapshotInstances(
                await GetSnapshots<TReadModel>(readModelIdentifier, handler.EventSequenceId, readModelKey));
        }

        throw new UnknownReadModel(typeof(TReadModel));
    }

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>()
    {
        if (reducers.HasFor<TReadModel>())
        {
            return reducerObservers.GetWatcher<TReadModel>().Observable
                .Select(changeset => new ReadModelChangeset<TReadModel>(
                    changeset.Namespace,
                    changeset.ModelKey,
                    changeset.ReadModel,
                    changeset.Removed,
                    changeset.Removed ? ReadModelChangeType.Removed : ReadModelChangeType.Modified,
                    EventContext.EmptyWithEventSourceId(changeset.ModelKey)));
        }

        if (!projections.HasFor<TReadModel>())
        {
            throw new UnknownReadModel(typeof(TReadModel));
        }

        return readModelWatcherManager.GetWatcher<TReadModel>().Observable;
    }

    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> GetWatcherFor<TReadModel>()
    {
        if (!projections.HasFor<TReadModel>())
        {
            throw new UnknownReadModel(typeof(TReadModel));
        }

        return readModelWatcherManager.GetWatcher<TReadModel>();
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey)
    {
        var readModelIdentifier = readModelType.GetReadModelIdentifier();

        var request = new DehydrateSessionRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModelIdentifier = readModelIdentifier,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey,
            SessionId = sessionId.Value.ToString()
        };

        await _chronicleServicesAccessor.Services.ReadModels.DehydrateSession(request);
    }

    /// <inheritdoc/>
    public Task<TReadModel> Release<TReadModel>(TReadModel instance) => _releaser.Release(instance);

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances) => _releaser.Release(instances);

    /// <summary>
    /// Reads the snapshots one read model instance passed through.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to read snapshots of.</typeparam>
    /// <param name="readModelIdentifier">The identifier of the read model.</param>
    /// <param name="eventSequenceId">The event sequence the read model observes.</param>
    /// <param name="readModelKey">The key of the instance.</param>
    /// <returns>The snapshots, oldest first.</returns>
    async Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshots<TReadModel>(
        ReadModelIdentifier readModelIdentifier,
        EventSequenceId eventSequenceId,
        ReadModelKey readModelKey)
    {
        var response = await _chronicleServicesAccessor.Services.ReadModelExplorer.AllSnapshotsForReadModel(
            new AllSnapshotsForReadModelRequest
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace,
                ReadModel = readModelIdentifier,
                ReadModelKey = readModelKey,
                EventSequenceId = eventSequenceId
            });

        return response.Data.Select(snapshot => new ReadModelSnapshot<TReadModel>(
            JsonSerializer.Deserialize<TReadModel>(snapshot.Instance, jsonSerializerOptions)!,
            snapshot.Events.ToClient(eventStore.Name, eventStore.Namespace, eventTypes, jsonSerializerOptions),
            snapshot.Occurred,
            snapshot.CorrelationId)).ToList();
    }

    /// <summary>
    /// Check whether a read model has to be reduced here rather than read from the Kernel's materialized store.
    /// </summary>
    /// <param name="readModelType">The read model type to check.</param>
    /// <returns>True when the read model is reducer-backed and passive, false otherwise.</returns>
    /// <remarks>
    /// A reducer-backed read model that is not passive is observed, so the Kernel already holds its state in a
    /// sink and serves it with PII released. A passive one has no observer and therefore no sink to read from,
    /// so its state only exists once this client has folded the events for it.
    /// </remarks>
    bool IsReducedInProcess(Type readModelType) => reducers.HasFor(readModelType) && readModelType.IsPassive();

    /// <summary>
    /// Resolve the <see cref="EventSequenceId"/> a read model is built from.
    /// </summary>
    /// <param name="readModelType">The read model type to resolve for.</param>
    /// <returns>The <see cref="EventSequenceId"/> to ask the Kernel for the read model on.</returns>
    EventSequenceId GetEventSequenceIdFor(Type readModelType) =>
        reducers.HasFor(readModelType)
            ? reducers.GetHandlerForReadModelType(readModelType).EventSequenceId
            : EventSequenceId.Log;

    async Task<IEnumerable<ReadModelSnapshot<TReadModel>>> ReleaseSnapshotInstances<TReadModel>(IEnumerable<ReadModelSnapshot<TReadModel>> snapshots)
    {
        var released = new List<ReadModelSnapshot<TReadModel>>();
        foreach (var snapshot in snapshots)
        {
            released.Add(snapshot with { Instance = await Release(snapshot.Instance) });
        }

        return released;
    }

    /// <summary>
    /// Resolves the <see cref="SinkTypeId"/> for a read model type.
    /// </summary>
    /// <param name="readModelType">The read model type to resolve the sink for.</param>
    /// <returns>The <see cref="SinkTypeId"/> to register the read model with.</returns>
    /// <remarks>
    /// Passive read models never have an observer writing to a materialized sink, so they register
    /// with <see cref="SinkTypeId.None"/>. This lets the kernel fall through to immediate
    /// projection when resolving the instance by key instead of reading an empty sink and returning null.
    /// </remarks>
    SinkTypeId GetSinkTypeIdFor(Type readModelType) =>
        readModelType.IsPassive() ? SinkTypeId.None : _defaultSinkTypeId;

    List<IndexDefinition> GetIndexesForType(Type type, string prefix)
    {
        var indexes = new List<IndexDefinition>();
        var visitedTypes = new HashSet<Type>();
        CollectIndexes(type, prefix, indexes, visitedTypes);
        return indexes;
    }

    void CollectIndexes(Type type, string prefix, List<IndexDefinition> indexes, HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(type))
        {
            return;
        }
        visitedTypes.Add(type);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyPath = string.IsNullOrEmpty(prefix)
                ? namingPolicy.GetPropertyName(property.Name)
                : $"{prefix}.{namingPolicy.GetPropertyName(property.Name)}";

            if (Attribute.IsDefined(property, typeof(IndexAttribute)))
            {
                indexes.Add(new IndexDefinition { PropertyPath = propertyPath });
            }

            var propertyType = property.PropertyType;

            // Check if it's a collection type (IList<T>, IEnumerable<T>, etc.)
            if (propertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                var elementType = propertyType.GetGenericArguments().FirstOrDefault();
                if (elementType?.IsPrimitive == false && elementType != typeof(string))
                {
                    CollectIndexes(elementType, propertyPath, indexes, visitedTypes);
                }
            }
            else if (!propertyType.IsPrimitive && propertyType != typeof(string) && !propertyType.IsValueType)
            {
                // Recurse into complex types
                CollectIndexes(propertyType, propertyPath, indexes, visitedTypes);
            }
        }
    }
}
