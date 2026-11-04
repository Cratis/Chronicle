// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.given;

public class an_event_store_initialization : Specification
{
    protected static readonly EventStoreName _name = "the-store";
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IKernelProjections _projections;
    protected IEventSequence _systemSequence;
    protected bool _initialized;
    protected IGrainFactory _grains;
    protected IEventStoreInitializer _initializerReference;
    protected EventStoreInitialization _initialization;

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
        _projections = Substitute.For<IKernelProjections>();
        var eventTypes = Substitute.For<IEventTypes>();
        _systemSequence = Substitute.For<IEventSequence>();
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
            .Returns(AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First));
        silo.AddService(_storage);
        silo.AddService(_projections);
        silo.AddService(eventTypes);
        silo.AddProbe<IEventSequence>(_ => _systemSequence);
        var initializer = await silo.CreateGrainAsync<EventStoreInitializer>(_name.Value);
        _grains = Substitute.For<IGrainFactory>();
        _initializerReference = Substitute.For<IEventStoreInitializer>();
        _initializerReference.Ensure().Returns(_ => initializer.Ensure());
        _grains.GetGrain<IEventStoreInitializer>(_name.Value).Returns(_initializerReference);
        _initialization = new(_storage, _grains, NullLogger<EventStoreInitialization>.Instance);
    }
}
