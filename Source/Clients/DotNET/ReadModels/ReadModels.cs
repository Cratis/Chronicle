// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="projections">Projections to get read models from.</param>
/// <param name="reducers">Reducers to get read models from.</param>
/// <param name="schemaGenerator">Schema generator to use.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for JSON serialization.</param>
/// <param name="readModelWatcherManager"><see cref="IReadModelWatcherManager"/> for managing watchers.</param>
/// <param name="reducerObservers"><see cref="IReducerObservers"/> for managing reducer observers.</param>
public class ReadModels(
    IEventStore eventStore,
    INamingPolicy namingPolicy,
    IProjections projections,
    IReducers reducers,
    IJsonSchemaGenerator schemaGenerator,
    JsonSerializerOptions jsonSerializerOptions,
    IReadModelWatcherManager readModelWatcherManager,
    IReducerObservers reducerObservers) : IReadModels
{
    readonly IChronicleServicesAccessor _chronicleServicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

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
                    TypeId = WellKnownSinkTypes.MongoDB
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
                    TypeId = WellKnownSinkTypes.MongoDB
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
        return (TReadModel)result;
    }

    /// <inheritdoc/>
    public async Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        // Validate that the read model is known by either projections or reducers
        if (!projections.HasFor(readModelType) && !reducers.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        var readModelIdentifier = readModelType.GetReadModelIdentifier();

        var request = new GetInstanceByKeyRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModelIdentifier = readModelIdentifier,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = key,
            SessionId = sessionId?.Value.ToString() ?? string.Empty
        };

        var response = await _chronicleServicesAccessor.Services.ReadModels.GetInstanceByKey(request);
        var instance = JsonSerializer.Deserialize(response.ReadModel, readModelType, jsonSerializerOptions);
        return instance ?? throw new InvalidOperationException($"Read model returned null for type '{readModelType.Name}' with key '{key.Value}'");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null)
    {
        var readModelType = typeof(TReadModel);

        // Validate that the read model is known by projections (immediate projections only)
        if (!projections.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        var readModelIdentifier = readModelType.GetReadModelIdentifier();
        var eventCountValue = eventCount ?? EventCount.Unlimited;

        var request = new GetAllInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ReadModelIdentifier = readModelIdentifier,
            EventSequenceId = EventSequenceId.Log,
            EventCount = eventCountValue.Value
        };

        var response = await _chronicleServicesAccessor.Services.ReadModels.GetAllInstances(request);

        return response.Instances.Select(json => JsonSerializer.Deserialize<TReadModel>(json, jsonSerializerOptions)!);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey)
    {
        // Check for projection first
        if (projections.HasFor<TReadModel>())
        {
            var readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();

            var request = new GetSnapshotsByKeyRequest
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace,
                ReadModelIdentifier = readModelIdentifier,
                EventSequenceId = EventSequenceId.Log,
                ReadModelKey = readModelKey
            };

            var response = await _chronicleServicesAccessor.Services.ReadModels.GetSnapshotsByKey(request);

            var snapshots = new List<ReadModelSnapshot<TReadModel>>();
            foreach (var snapshot in response.Snapshots)
            {
                var readModel = JsonSerializer.Deserialize<TReadModel>(snapshot.ReadModel, jsonSerializerOptions)!;
                var events = snapshot.Events.ToClient(jsonSerializerOptions);

                snapshots.Add(new ReadModelSnapshot<TReadModel>(
                    readModel,
                    events,
                    snapshot.Occurred,
                    snapshot.CorrelationId));
            }

            return snapshots;
        }

        // Explicitly check reducers existence using HasReducerFor(Type) to satisfy specs
        if (reducers.HasReducerFor(typeof(TReadModel)))
        {
            // Route via reducers internal retrieval to use correct event sequence
            var concreteReducers = reducers as Reducers.Reducers;
            if (concreteReducers is not null)
            {
                return await concreteReducers.GetSnapshotsById<TReadModel>(readModelKey);
            }

            // Fallback to generic retrieval if not the concrete type
            var readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();
            var handler = reducers.GetHandlerForReadModelType(typeof(TReadModel));

            var request = new GetSnapshotsByKeyRequest
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace,
                ReadModelIdentifier = readModelIdentifier,
                EventSequenceId = handler.EventSequenceId,
                ReadModelKey = readModelKey
            };

            var response = await _chronicleServicesAccessor.Services.ReadModels.GetSnapshotsByKey(request);

            var snapshots = new List<ReadModelSnapshot<TReadModel>>();
            foreach (var snapshot in response.Snapshots)
            {
                var readModel = JsonSerializer.Deserialize<TReadModel>(snapshot.ReadModel, jsonSerializerOptions)!;
                var events = snapshot.Events.ToClient(jsonSerializerOptions);

                snapshots.Add(new ReadModelSnapshot<TReadModel>(
                    readModel,
                    events,
                    snapshot.Occurred,
                    snapshot.CorrelationId));
            }

            return snapshots;
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
                    changeset.Removed));
        }

        if (!projections.HasFor<TReadModel>())
        {
            throw new UnknownReadModel(typeof(TReadModel));
        }

        return readModelWatcherManager.GetWatcher<TReadModel>().Observable;
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
