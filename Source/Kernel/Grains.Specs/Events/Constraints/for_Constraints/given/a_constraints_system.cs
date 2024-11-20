// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Orleans.BroadcastChannel;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Grains.Events.Constraints.given;

public class a_constraints_system : Specification
{
    protected TestKitSilo _silo;
    protected Constraints _constraints;

    protected IStorage<ConstraintsState> _stateStorage;
    protected IBroadcastChannelProvider _broadcastChannelProvider;
    protected IBroadcastChannelWriter<ConstraintsChanged> _broadcastChannelWriter;
    protected TestStorageStats _storageStats;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        var key = new ConstraintsKey("Something");
        _broadcastChannelProvider = Substitute.For<IBroadcastChannelProvider>();
        var serviceProvider = Substitute.For<IKeyedServiceProvider>();
        var clusterClient = Substitute.For<IClusterClient>();
        clusterClient.ServiceProvider.Returns(serviceProvider);
        _silo.AddService(clusterClient);
        serviceProvider.GetRequiredKeyedService(typeof(IBroadcastChannelProvider), WellKnownBroadcastChannelNames.ConstraintsChanged).Returns(_broadcastChannelProvider);
        _constraints = await _silo.CreateGrainAsync<Constraints>(key);
        _stateStorage = _silo.StorageManager.GetStorage<ConstraintsState>(typeof(Constraints).FullName);
        _stateStorage.State = new ConstraintsState();

        _storageStats = _silo.StorageStats<Constraints, ConstraintsState>()!;

        _broadcastChannelWriter = Substitute.For<IBroadcastChannelWriter<ConstraintsChanged>>();
        _broadcastChannelProvider.GetChannelWriter<ConstraintsChanged>(Arg.Any<ChannelId>()).Returns(_broadcastChannelWriter);
    }
}
