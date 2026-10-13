// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels;

public partial class ReadModels
{
    /// <summary>
    /// Builds all read-model definitions before any store registration effects.
    /// </summary>
    /// <returns>The complete read-model registration payload.</returns>
    internal RegisterManyRequest PrepareRegistration()
    {
        var readModels = new List<IHaveReadModel>();
        readModels.AddRange(projections.GetAllHandlers());
        readModels.AddRange(reducers.GetAllHandlers());
        var definitions = readModels.ConvertAll(readModel =>
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
            if (eventTypes.IsEventTarget(readModel.ReadModelType))
            {
                return CreateEventTargetDefinition(readModel.ReadModelType, observerType, observerIdentifier);
            }

            return new ReadModelDefinition
            {
                Type = new()
                {
                    Identifier = readModel.ReadModelType.GetReadModelIdentifier(),
                    Generation = ReadModelGeneration.First,
                },
                ContainerName = GetContainerNameFor(readModel.ReadModelType),
                DisplayName = readModel.ReadModelType.Name,
                Sink = new()
                {
                    ConfigurationId = Guid.Empty,
                    TypeId = GetSinkTypeIdFor(readModel.ReadModelType)
                },
                Schema = schemaGenerator.GenerateForReadModel(readModel.ReadModelType).ToJson(),
                Indexes = GetIndexesForType(readModel.ReadModelType, string.Empty),
                ObserverType = observerType,
                ObserverIdentifier = observerIdentifier
            };
        });

        return new()
        {
            EventStore = eventStore.Name,
            Owner = ReadModelOwner.Client,
            ReadModels = definitions
        };
    }

    /// <summary>
    /// Sends a complete payload already built under the successful store preparation lease.
    /// </summary>
    /// <param name="payload">The complete read-model registration payload.</param>
    /// <returns>The wire registration operation.</returns>
    internal Task RegisterPrepared(RegisterManyRequest payload) => _chronicleServicesAccessor.Services.ReadModels.RegisterMany(payload);
}
