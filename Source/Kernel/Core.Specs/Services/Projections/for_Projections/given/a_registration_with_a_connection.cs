// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Services.Projections.Definitions;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

public class a_registration_with_a_connection : all_dependencies
{
    protected readonly ConnectionId _connection = ConnectionId.New();
    protected IConnectedClients _localClients;
    protected IConnectedClients _remoteClients;
    protected IManagementGrain _management;
    protected Chronicle.Projections.IProjectionsManager _manager;
    protected RegisterRequest _request;

    void Establish()
    {
        var local = SiloAddress.Zero;
        var remote = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        _localClients = Substitute.For<IConnectedClients>();
        _remoteClients = Substitute.For<IConnectedClients>();
        _grainFactory.GetGrain<IConnectedClients>(local.ToParsableString()).Returns(_localClients);
        _grainFactory.GetGrain<IConnectedClients>(remote.ToParsableString()).Returns(_remoteClients);
        _management = Substitute.For<IManagementGrain>();
        _management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus> { [local] = SiloStatus.Active, [remote] = SiloStatus.Active });
        _grainFactory.GetGrain<IManagementGrain>(0).Returns(_management);
        _manager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        _grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(EventStore.Value).Returns(_manager);
        _request = new RegisterRequest
        {
            EventStore = EventStore.Value,
            ConnectionId = _connection.Value,
            Owner = Contracts.Projections.ProjectionOwner.Client,
            Projections = [_projectionDefinition.ToContract()]
        };
    }
}
