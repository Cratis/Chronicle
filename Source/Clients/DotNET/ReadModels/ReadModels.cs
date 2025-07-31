// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use.</param>
/// <param name="projections">Projections to use.</param>
/// <param name="reducers">Reducers to use.</param>
/// <param name="modelNameResolver">Model name resolver to use.</param>
/// <param name="schemaGenerator">Schema generator to use.</param>
public class ReadModels(
    IEventStore eventStore,
    IProjections projections,
    IReducers reducers,
    IModelNameResolver modelNameResolver,
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
            Name = readModel.ReadModelName,
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
                Name = modelNameResolver.GetNameFor(typeof(TReadModel)),
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
}
