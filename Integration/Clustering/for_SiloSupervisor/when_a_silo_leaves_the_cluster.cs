// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Integration.Clustering.for_Clustering;
using Cratis.Chronicle.Reactors;
using KernelCore::Cratis.Chronicle.Supervision;
using context = Cratis.Chronicle.Integration.Clustering.for_SiloSupervisor.when_a_silo_leaves_the_cluster.context;

namespace Cratis.Chronicle.Integration.Clustering.for_SiloSupervisor;

/// <summary>
/// The silo hosting the observers dies while a client reactor is connected. Nothing in the surviving observer's own
/// activation notices, so the supervisor of the surviving silo hears the departure and opens its window of checks.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(SupervisorCollection.Name)]
public class when_a_silo_leaves_the_cluster(context _context)
    : IClassFixture<context>
{
    public class context(SupervisorFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public bool WindowWasOpenBefore { get; private set; }
        public bool WindowOpened { get; private set; }
        public TimeSpan TimeSinceLastTick { get; private set; }

        public async ValueTask InitializeAsync()
        {
            await fixture.ClientEventStore.Reactors.GetHandlerFor<ClusteredReactor>().WaitTillActive(_timeout);

            var supervisor = (SiloSupervisor)fixture.SiloServices.GetRequiredService<ISiloSupervisor>();
            WindowWasOpenBefore = supervisor.IsDepartureWindowOpen;

            await fixture.CrashSecondSilo();

            var deadline = DateTimeOffset.UtcNow + _timeout;
            while (!supervisor.IsDepartureWindowOpen && DateTimeOffset.UtcNow < deadline)
            {
                // Task.Delay: poll interval while waiting for the departure window to open, bounded by the deadline.
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            WindowOpened = supervisor.IsDepartureWindowOpen;
            TimeSinceLastTick = DateTimeOffset.UtcNow - supervisor.LastTick;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_not_have_the_window_open_before_the_departure() => _context.WindowWasOpenBefore.ShouldBeFalse();
    [Fact] void should_open_the_window_of_checks_on_the_surviving_silo() => _context.WindowOpened.ShouldBeTrue();
    [Fact] void should_keep_the_supervisor_of_the_surviving_silo_ticking() => (_context.TimeSinceLastTick < TimeSpan.FromSeconds(10)).ShouldBeTrue();
}
