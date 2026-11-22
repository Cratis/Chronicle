// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_activating;

public class a_grain : Specification
{
    readonly TestKitSilo _silo = new();

    async Task Establish()
    {
        _silo.AddService(Substitute.For<ILogger<ConnectedClients>>());
        _silo.AddService(Substitute.For<IMeter<ConnectedClients>>());
        _silo.AddService<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions()));
        _silo.AddService(Substitute.For<IStorage>());
        _silo.AddService(Substitute.For<IClientDepartureNotifier>());
        await _silo.CreateGrainAsync<ConnectedClients>("placement");
    }

    [Fact] void should_revise_the_clients_on_a_timer_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
