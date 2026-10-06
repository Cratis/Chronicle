// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.BroadcastChannel;
using Orleans.TestKit;

namespace Cratis.Chronicle.Namespaces.for_Namespaces.given;

public class a_namespaces_grain : Specification
{
    protected TestKitSilo _silo = new();
    protected Namespaces _namespaces;
    protected IEventSequence _systemSequence;
    protected IBroadcastChannelWriter<NamespaceAdded> _writer;
    protected NamespacesState _state;
    protected EventStoreName _eventStore = "some-store";
    protected EventStoreNamespaceName _namespace = "future";
    ServiceProvider _services;

    async Task Establish()
    {
        _writer = Substitute.For<IBroadcastChannelWriter<NamespaceAdded>>();
        var channelProvider = Substitute.For<IBroadcastChannelProvider>();
        channelProvider.GetChannelWriter<NamespaceAdded>(Arg.Any<ChannelId>()).Returns(_writer);
        _services = new ServiceCollection()
            .AddKeyedSingleton(WellKnownBroadcastChannelNames.NamespaceAdded, channelProvider)
            .BuildServiceProvider();
        var client = Substitute.For<IClusterClient>();
        client.ServiceProvider.Returns(_services);
        _silo.AddService(client);
        _silo.AddService(NullLogger<Namespaces>.Instance);

        _systemSequence = Substitute.For<IEventSequence>();
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
            .Returns(AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First));
        _silo.AddProbe<IEventSequence>(key => key.ToString() == new EventSequenceKey(EventSequenceId.System, EventStoreName.System, EventStoreNamespaceName.Default).ToString()
            ? _systemSequence
            : null!);
        var storage = _silo.StorageManager.GetStorage<NamespacesState>(typeof(Namespaces).FullName!);
        _state = new NamespacesState();
        storage.State = _state;
        _namespaces = await _silo.CreateGrainAsync<Namespaces>(_eventStore.Value);
    }

    void Destroy() => _services.Dispose();
}
