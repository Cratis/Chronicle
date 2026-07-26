// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ContractConnectedClient = Cratis.Chronicle.Contracts.Clients.ConnectedClient;
using KernelConnectedClient = Cratis.Chronicle.Concepts.Clients.ConnectedClient;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_getting_connected_clients;

/// <summary>
/// The per-silo lookups are independent, so they are issued together rather than one silo at a time - the cost of the
/// call is one round trip, not one per silo.
/// </summary>
public class across_all_silos : given.a_connection_service_with_three_silos
{
    readonly ConcurrentCallGate _gate = new(NumberOfSilos);
    IEnumerable<ContractConnectedClient> _result;

    void Establish()
    {
        for (var index = 0; index < NumberOfSilos; index++)
        {
            var silo = _silos[index];
            _connectedClientsPerSilo[index].GetAllConnectedClients().Returns(_ => GetClientsWhenAllSilosAreInFlight(silo));
        }
    }

    async Task Because() => _result = await _connectionService.GetConnectedClients();

    [Fact] void should_have_every_silo_in_flight_at_the_same_time() => _gate.AllCallsWereConcurrent.ShouldBeTrue();
    [Fact] void should_return_the_clients_from_every_silo() => _result.Count().ShouldEqual(NumberOfSilos);
    [Fact] void should_stamp_each_client_with_the_silo_it_is_connected_to() =>
        _result.Select(client => client.SiloAddress).ShouldContainOnly(_silos.Select(silo => silo.ToParsableString()));

    async Task<IEnumerable<KernelConnectedClient>> GetClientsWhenAllSilosAreInFlight(SiloAddress silo)
    {
        await _gate.Enter();
        return ClientOn(silo);
    }
}
