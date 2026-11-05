// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Reducers;

public partial class Reducers
{
    ReducerCatalog Catalog => new(_clientArtifacts, _eventTypes, _reducerValidator);

    /// <inheritdoc/>
    public Task<IReducerHandler> Register(Type reducerType)
    {
        RegisteredObserverMetadata.Validate(reducerType, ExplicitArtifactKind.Reducer, _clientArtifacts);
        if (_eventStore.Reactors is Reactors.Reactors reactors && reactors.Handlers.Any(_ => _.ReactorType == reducerType))
        {
            throw new ConflictingArtifactKind(reducerType, nameof(ExplicitArtifactKind.Reactor), nameof(ExplicitArtifactKind.Reducer));
        }
        var descriptor = Catalog.GetForRegistration(reducerType);
        var modelType = descriptor.ReadModelType;
        lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
        {
            ReadModelBuilderOwnership.Validate(_eventStore, _clientArtifacts, Catalog, modelType, reducerType, descriptor.IsSelfReducing);
            var handler = CreateHandlerFor(reducerType, modelType);
            RegisterWhenDispatchable(handler);
            var byType = new Dictionary<Type, IReducerHandler>(_handlersByType) { { reducerType, handler } };
            var byModel = new Dictionary<Type, IReducerHandler>(_handlersByModelType) { { modelType, handler } };
            _handlersByType = byType.ToFrozenDictionary();
            _handlersByModelType = byModel.ToFrozenDictionary();
            return Task.FromResult<IReducerHandler>(handler);
        }
    }

    /// <inheritdoc/>
    public Task<ReducerState> GetStateFor(Type reducerType) => GetHandlerFor(reducerType).GetState();

    /// <inheritdoc/>
    public Task<JobId> Replay(Type reducerType) => Replay(GetHandlerFor(reducerType).Id);

    ReducerId ResolveId(Type type) => RegisteredObserverMetadata.ReducerIdFor(_clientArtifacts, type);

    Type ResolveReadModel(Type type) => Catalog.Get(type).ReadModelType;
}
