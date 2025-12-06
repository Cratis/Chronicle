// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
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
/// <param name="additionalReadModels">Additional read models to register.</param>
/// <param name="schemaGenerator">Schema generator to use.</param>
public class ReadModels(
    IEventStore eventStore,
    INamingPolicy namingPolicy,
    IProjections projections,
    IReducers reducers,
    IEnumerable<IHaveReadModel> additionalReadModels,
    IJsonSchemaGenerator schemaGenerator) : IReadModels
{
    readonly IChronicleServicesAccessor _chronicleServicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task Register()
    {
        var readModels = new List<IHaveReadModel>();

        readModels.AddRange(projections.GetAllHandlers());
        readModels.AddRange(reducers.GetAllHandlers());
        readModels.AddRange(additionalReadModels);

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

            if (property.GetCustomAttribute<IndexAttribute>() is not null)
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
