// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            return result.ReadModel ?? throw new InvalidOperationException($"Reducer returned null for read model type '{readModelType.Name}'");
        }

        throw new UnknownReadModel(readModelType);
    }
}
