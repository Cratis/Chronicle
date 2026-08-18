// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KernelConnectedClient = Cratis.Chronicle.Concepts.Clients.ConnectedClient;
using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

/// <summary>
/// A <see cref="KernelConnectionService"/> talking to a three-silo cluster, where each silo has its own
/// <see cref="IConnectedClients"/> grain.
/// </summary>
public class a_connection_service_with_three_silos : Specification
{
    protected const int NumberOfSilos = 3;

    protected IConnectionService _connectionService;
    protected IGrainFactory _grainFactory;
    protected IManagementGrain _management;
    protected SiloAddress[] _silos;
    protected IConnectedClients[] _connectedClientsPerSilo;

    void Establish()
    {
        _silos = [.. Enumerable.Range(1, NumberOfSilos).Select(index => SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11110 + index), index))];
        _connectedClientsPerSilo = [.. _silos.Select(_ => Substitute.For<IConnectedClients>())];

        _management = Substitute.For<IManagementGrain>();
        _management.GetHosts(true).Returns(_silos.ToDictionary(silo => silo, _ => SiloStatus.Active));

        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string>()).Returns(_management);
        for (var index = 0; index < NumberOfSilos; index++)
        {
            _grainFactory.GetGrain<IConnectedClients>(_silos[index].ToParsableString(), Arg.Any<string>()).Returns(_connectedClientsPerSilo[index]);
        }

        _connectionService = new KernelConnectionService(
            _grainFactory,
            Substitute.For<ILocalSiloDetails>(),
            NullLogger<KernelConnectionService>.Instance,
            Options.Create(new ChronicleOptions()));
    }

    protected static IEnumerable<KernelConnectedClient> ClientOn(SiloAddress silo) =>
        [new KernelConnectedClient { ConnectionId = new ConnectionId(silo.ToParsableString()) }];
}
