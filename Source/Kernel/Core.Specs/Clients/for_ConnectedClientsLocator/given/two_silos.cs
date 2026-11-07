// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClientsLocator.given;

public class two_silos : Specification
{
    protected static readonly ConnectionId _connection = new("the-client");
    protected IGrainFactory _grainFactory;
    protected SiloAddress _first;
    protected SiloAddress _second;
    protected IConnectedClients _firstClients;
    protected IConnectedClients _secondClients;

    void Establish()
    {
        _first = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _second = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        _firstClients = Substitute.For<IConnectedClients>();
        _secondClients = Substitute.For<IConnectedClients>();
        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus> { [_first] = SiloStatus.Active, [_second] = SiloStatus.Active });
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IManagementGrain>(0, Arg.Any<string>()).Returns(management);
        _grainFactory.GetGrain<IConnectedClients>(_first.ToParsableString(), Arg.Any<string>()).Returns(_firstClients);
        _grainFactory.GetGrain<IConnectedClients>(_second.ToParsableString(), Arg.Any<string>()).Returns(_secondClients);
    }
}
