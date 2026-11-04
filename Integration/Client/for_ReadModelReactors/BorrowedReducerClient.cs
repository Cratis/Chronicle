// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Sinks;
using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public sealed class BorrowedReducerClient : IDisposable
{
    readonly ChronicleClient _client;

    public BorrowedReducerClient(IEventStore owner, SinkTypeId? defaultSinkTypeId)
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ReducerWatchedEvent)]);
        artifacts.Reducers.Returns([typeof(InactiveWatchedReducer)]);

        // The borrowed client registers the same read model as the owner. With a different default sink it
        // would move the read model to a sink the kernel's storage cannot serve, stalling the owner's reducer.
        var options = new ChronicleOptions();
        if (defaultSinkTypeId is not null)
        {
            options.DefaultSinkTypeId = defaultSinkTypeId;
        }
        _client = new(new Connection(owner.Connection), options, artifactsProvider: artifacts);
    }

    public async Task<IEventStore> Open(IEventStore owner)
    {
        var store = await _client.GetEventStore(owner.Name, owner.Namespace);
        await store.RegisterAll();
        return store;
    }

    public void Dispose()
    {
        _client.EvictEventStores();
        _client.Dispose();
    }

    sealed class Connection(IChronicleConnection connection) : IChronicleConnection, IChronicleServicesAccessor
    {
        public IConnectionLifecycle Lifecycle => connection.Lifecycle;
        public IServices Services => ((IChronicleServicesAccessor)connection).Services;
        public Task Connect() => connection.Connect();

        public void Dispose()
        {
            // Only the fixture owns the underlying transport.
        }
    }
}
