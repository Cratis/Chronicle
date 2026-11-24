// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Supervision;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_activating;

public class a_grain : Specification
{
    static readonly Configuration.Clients _defaults = new();

    readonly TestKitSilo _silo = new();
    ISiloSupervisor _supervisor;

    async Task Establish()
    {
        _supervisor = Substitute.For<ISiloSupervisor>();
        _silo.AddService(Substitute.For<ILogger<ConnectedClients>>());
        _silo.AddService(Substitute.For<IMeter<ConnectedClients>>());
        _silo.AddService<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions()));
        _silo.AddService(Substitute.For<IStorage>());
        _silo.AddService(Substitute.For<IClientDepartureNotifier>());
        _silo.AddService(_supervisor);
        await _silo.CreateGrainAsync<ConnectedClients>("placement");
    }

    [Fact] void should_register_the_revision_with_the_supervisor() => _supervisor.Received(1).Register(
        "connected-clients:placement",
        TimeSpan.FromSeconds(Math.Max(_defaults.ReviseIntervalSeconds, _defaults.StaleThresholdSeconds)),
        Arg.Any<Func<Task<TimeSpan?>>>(),
        Arg.Any<Func<Task>>());

    [Fact] void should_not_arm_a_timer_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
