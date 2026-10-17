// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Storage;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.given;

public class an_ensure_command : Specification
{
    protected static readonly EventStoreName _name = "the-store";
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IEventTypes _eventTypes;
    protected IReactors _reactors;
    protected IKernelProjections _projections;
    protected INamespaces _namespaces;
    protected IEventSequence _systemSequence;
    protected bool _initialized;
    protected EventStoreInitializer _initializer;
    protected IEventStoreInitializer _initializerReference;
    IGrainFactory _grains;
    EnsureEventStore _command;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(_name).Returns(_store);
        _store.IsInitialized().Returns(_ => _initialized);
        _store.MarkInitialized().Returns(_ =>
        {
            _initialized = true;
            return Task.CompletedTask;
        });
        _eventTypes = Substitute.For<IEventTypes>();
        _reactors = Substitute.For<IReactors>();
        _projections = Substitute.For<IKernelProjections>();
        _namespaces = Substitute.For<INamespaces>();
        _systemSequence = Substitute.For<IEventSequence>();
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
            .Returns(AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First));
        silo.AddService(_storage);
        silo.AddService(_projections);
        silo.AddProbe<IEventSequence>(_ => _systemSequence);
        _initializer = await silo.CreateGrainAsync<EventStoreInitializer>(_name.Value);
        _grains = Substitute.For<IGrainFactory>();
        _grains.GetGrain<INamespaces>(_name.Value).Returns(_namespaces);
        _initializerReference = Substitute.For<IEventStoreInitializer>();
        _initializerReference.Ensure().Returns(_ => _initializer.Ensure());
        _grains.GetGrain<IEventStoreInitializer>(_name.Value).Returns(_initializerReference);
        _command = new(_name);
    }

    protected Task Ensure() => _command.Handle(_grains, _storage, _eventTypes, _reactors);
}
