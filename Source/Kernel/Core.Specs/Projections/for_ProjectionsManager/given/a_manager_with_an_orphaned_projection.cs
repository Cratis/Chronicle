// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_manager_with_an_orphaned_projection : a_projections_manager_grain
{
    protected ProjectionDefinition _definition;
    protected IConnectedClients _localClients;
    protected IConnectedClients _remoteClients;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _definition = CreateDefinition("the-projection", "the-read-model");
        _state.Projections = [_definition];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default, (EventStoreNamespaceName)"second"]);
        var local = SiloAddress.Zero;
        var remote = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus> { [local] = SiloStatus.Active, [remote] = SiloStatus.Active });
        _silo.AddProbe(_ => management);
        _localClients = Substitute.For<IConnectedClients>();
        _remoteClients = Substitute.For<IConnectedClients>();
        _silo.AddProbe<IConnectedClients>(key => key.ToString() == remote.ToParsableString() ? _remoteClients : _localClients);
    }
}
