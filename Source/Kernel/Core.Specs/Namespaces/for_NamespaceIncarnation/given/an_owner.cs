// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.BroadcastChannel;
using Orleans.TestKit;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation.given;

public class an_owner : for_NamespaceIncarnationCoordinator.given.a_coordinator
{
    protected static readonly EventStoreName _eventStore = "store";
    protected readonly TestKitSilo _silo = new();
    protected readonly List<NamespaceState> _listing = [];
    protected NamespaceIncarnation _owner;
    protected INamespaceRemovalSteps _steps;
    protected IEventSequence _system;
    protected INamespaceStorage _names;
    ServiceProvider _services;

    /// <summary>Gets the owner address before the one grain in this silo is activated.</summary>
    protected virtual EventStoreNamespaceName OwnerNamespace => _name;

    async Task Establish()
    {
        _lifecycle.Get(Arg.Any<EventStoreNamespaceName>()).Returns(_ => Task.FromResult<NamespaceLifecycle?>(_state));
        _lifecycle.TryWrite(Arg.Any<EventStoreNamespaceName>(), Arg.Any<long?>(), Arg.Any<NamespaceLifecycle>()).Returns(call =>
        {
            if (call.ArgAt<long?>(1) != _state?.Revision) return Task.FromResult(false);
            _state = call.ArgAt<NamespaceLifecycle>(2);
            _writes.Add(_state);
            return Task.FromResult(true);
        });
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        _names = Substitute.For<INamespaceStorage>();
        _names.GetAll().Returns(_ => Task.FromResult<IEnumerable<NamespaceState>>(_listing));
        _names.Create(Arg.Any<EventStoreNamespaceName>(), Arg.Any<DateTimeOffset>()).Returns(call =>
        {
            _listing.Add(new(call.Arg<EventStoreNamespaceName>(), call.Arg<DateTimeOffset>()));
            return Task.CompletedTask;
        });
        store.Namespaces.Returns(_names);
        storage.GetEventStore(_eventStore).Returns(store);
        var capability = Substitute.For<IHaveNamespaceIncarnations>();
        capability.NamespaceLifecycles.Returns(_lifecycle);
        capability.GetNamespaceMarker(Arg.Any<EventStoreNamespaceName>()).Returns(_marker);
        var access = Substitute.For<INamespaceIncarnations>();
        access.Enabled.Returns(true);
        access.GetStorage(_eventStore).Returns(capability);
        _steps = Substitute.For<INamespaceRemovalSteps>();
        _silo.AddService(storage);
        _silo.AddService(access);
        _silo.AddService(_steps);
        _silo.AddService<TimeProvider>(_clock);
        _silo.AddService(NullLogger<NamespaceIncarnation>.Instance);
        var channel = Substitute.For<IBroadcastChannelProvider>();
        channel.GetChannelWriter<NamespaceAdded>(Arg.Any<ChannelId>()).Returns(Substitute.For<IBroadcastChannelWriter<NamespaceAdded>>());
        _services = new ServiceCollection().AddKeyedSingleton(WellKnownBroadcastChannelNames.NamespaceAdded, channel).BuildServiceProvider();
        var client = Substitute.For<IClusterClient>();
        client.ServiceProvider.Returns(_services);
        _silo.AddService(client);
        _system = Substitute.For<IEventSequence>();
        _system.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
            .Returns(AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First));
        _silo.AddProbe<IEventSequence>(_ => _system);
        _owner = await _silo.CreateGrainAsync<NamespaceIncarnation>(new NamespaceIncarnationKey(_eventStore.Value, OwnerNamespace.Value).ToString());
    }

    void Destroy() => _services.Dispose();
}
