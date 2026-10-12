// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Reactors;

public partial class Reactors
{
    /// <inheritdoc/>
    public Task<IReactorHandler> Register(Type reactorType)
    {
        RegisteredObserverMetadata.Validate(reactorType, ExplicitArtifactKind.Reactor, _clientArtifactsProvider);
        if (_eventStore.Reducers.GetAllHandlers().Any(_ => _.ReducerType == reactorType))
        {
            throw new ConflictingArtifactKind(reactorType, nameof(ExplicitArtifactKind.Reactor), nameof(ExplicitArtifactKind.Reducer));
        }
        lock (_registerLock)
        {
            var registration = CreateRegistrationFor(reactorType);
            AddRegistration(registration);
            return Task.FromResult<IReactorHandler>(registration.Handler);
        }
    }

    /// <inheritdoc/>
    public IReactorHandler GetHandlerFor(Type reactorType) =>
        _handlers.Values.FirstOrDefault(_ => _.Handler.ReactorType == reactorType)?.Handler ?? _handlers[ResolveId(reactorType)].Handler;

    /// <inheritdoc/>
    public Task<ReactorState> GetStateFor(Type reactorType) => GetHandlerFor(reactorType).GetState();

    /// <inheritdoc/>
    public Task<JobId> Replay(Type reactorType) => Replay(GetHandlerFor(reactorType).Id);

    ReactorId ResolveId(Type type) => RegisteredObserverMetadata.ReactorIdFor(_clientArtifactsProvider, type);
}
