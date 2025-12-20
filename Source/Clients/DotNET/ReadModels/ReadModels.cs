// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reactive.Linq;
using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
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
public class ReadModels(
    IEventStore eventStore,
    INamingPolicy namingPolicy,
    IProjections projections,
    IReducers reducers,
    IJsonSchemaGenerator schemaGenerator) : IReadModels
{
    readonly IChronicleServicesAccessor _chronicleServicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task Register()
    {
        var readModels = new List<IHaveReadModel>();

        readModels.AddRange(projections.GetAllHandlers());
        readModels.AddRange(reducers.GetAllHandlers());

        var readModelDefinitions = readModels.ConvertAll(readModel => new ReadModelDefinition
        {
            Identifier = readModel.ReadModelType.GetReadModelIdentifier(),
            Name = namingPolicy.GetReadModelName(readModel.ReadModelType),
            Generation = ReadModelGeneration.First,
            Schema = schemaGenerator.Generate(readModel.ReadModelType).ToJson(),
            Indexes = GetIndexesForType(readModel.ReadModelType, string.Empty)
        });

        await _chronicleServicesAccessor.Services.ReadModels.Register(new RegisterRequest
        {
            EventStore = eventStore.Name,
            Owner = ReadModelOwner.Client,
            ReadModels = readModelDefinitions
        });
    }

    /// <inheritdoc/>
    public async Task Register<TReadModel>()
    {
        var readModelDefinitions = new List<ReadModelDefinition>()
        {
            new()
            {
                Identifier = typeof(TReadModel).GetReadModelIdentifier(),
                Name = namingPolicy.GetReadModelName(typeof(TReadModel)),
                Schema = schemaGenerator.Generate(typeof(TReadModel)).ToJson(),
                Indexes = GetIndexesForType(typeof(TReadModel), string.Empty)
            }
        };
        await _chronicleServicesAccessor.Services.ReadModels.Register(new RegisterRequest
        {
            EventStore = eventStore.Name,
            Owner = ReadModelOwner.Client,
            ReadModels = readModelDefinitions
        });
    }

    /// <inheritdoc/>
    public async Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key)
    {
        var readModelType = typeof(TReadModel);
        var result = await GetInstanceById(readModelType, key);
        return (TReadModel)result;
    }

    /// <inheritdoc/>
    public async Task<object> GetInstanceById(Type readModelType, ReadModelKey key)
    {
        if (projections.HasFor(readModelType))
        {
            var result = await projections.GetInstanceById(readModelType, key);
            return result.ReadModel;
        }

        if (reducers.HasFor(readModelType))
        {
            var result = await reducers.GetInstanceById(readModelType, key);
            return result.ReadModel ?? throw new InvalidOperationException($"Reducer returned null for read model type '{readModelType.Name}' with key '{key.Value}'");
        }

        throw new UnknownReadModel(readModelType);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey)
    {
        if (projections.HasFor(typeof(TReadModel)))
        {
            var projectionSnapshots = await projections.GetSnapshotsById<TReadModel>(readModelKey);
            return projectionSnapshots.Select(snapshot => new ReadModelSnapshot<TReadModel>(
                snapshot.Instance,
                snapshot.Events,
                snapshot.Occurred,
                snapshot.CorrelationId));
        }

        if (reducers.HasReducerFor(typeof(TReadModel)))
        {
            var reducerSnapshots = await reducers.GetSnapshotsById<TReadModel>(readModelKey);
            return reducerSnapshots.Select(snapshot => new ReadModelSnapshot<TReadModel>(
                snapshot.Instance,
                snapshot.Events,
                snapshot.Occurred,
                snapshot.CorrelationId));
        }

        throw new UnknownReadModel(typeof(TReadModel));
    }

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>()
    {
        var hasProjection = projections.HasFor(typeof(TReadModel));
        var hasReducer = reducers.HasFor<TReadModel>();

        if (!hasProjection && !hasReducer)
        {
            throw new UnknownReadModel(typeof(TReadModel));
        }

        var observables = new List<IObservable<ReadModelChangeset<TReadModel>>>();

        if (hasProjection)
        {
            observables.Add(projections.Watch<TReadModel>()
                .Select(changeset => new ReadModelChangeset<TReadModel>(
                    changeset.Namespace,
                    changeset.ModelKey,
                    changeset.ReadModel,
                    changeset.Removed)));
        }

        if (hasReducer)
        {
            observables.Add(reducers.Watch<TReadModel>()
                .Select(changeset => new ReadModelChangeset<TReadModel>(
                    changeset.Namespace,
                    changeset.ModelKey,
                    changeset.ReadModel,
                    changeset.Removed)));
        }

        return observables.Count == 1 ? observables[0] : observables[0].Merge(observables[1]);
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
