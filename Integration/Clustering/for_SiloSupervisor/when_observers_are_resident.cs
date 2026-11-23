// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using KernelCore::Cratis.Chronicle.Supervision;
using context = Cratis.Chronicle.Integration.Clustering.for_SiloSupervisor.when_observers_are_resident.context;

namespace Cratis.Chronicle.Integration.Clustering.for_SiloSupervisor;

/// <summary>
/// Every resident observer is watched by the supervisor of its silo rather than by a grain timer of its own, so the
/// supervisors of the cluster together hold at least one registration for each observer activation and keep ticking.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class when_observers_are_resident(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public int ObserverGrains { get; private set; }
        public int Registrations { get; private set; }
        public IReadOnlyList<TimeSpan> TimeSinceLastTick { get; private set; } = [];

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.Reactors.GetHandlerFor<for_ClientObserverServing.ServedReactor>().WaitTillActive(_timeout);
            await eventStore.Reducers.GetHandlerFor<for_ClientObserverServing.ServedReducer>().WaitTillActive(_timeout);

            var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
            var statistics = await management.GetDetailedGrainStatistics();
            ObserverGrains = statistics.Count(_ => _.GrainType.StartsWith("Cratis.Chronicle.Observation.Observer,", StringComparison.Ordinal));

            // The loop ticks once a second; give each supervisor a moment so its last tick is a real one.
            // Task.Delay: the supervisor loop is time-driven and exposes no signal for a completed tick.
            await Task.Delay(TimeSpan.FromSeconds(3));
            var supervisors = new[] { fixture.SiloServices, fixture.ObserversSiloServices }
                .Select(services => services.GetRequiredService<ISiloSupervisor>())
                .Cast<SiloSupervisor>()
                .ToArray();
            Registrations = supervisors.Sum(supervisor => supervisor.Count);
            TimeSinceLastTick = [.. supervisors.Select(supervisor => DateTimeOffset.UtcNow - supervisor.LastTick)];
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_have_activated_the_observers() => _context.ObserverGrains.ShouldBeGreaterThan(0);
    [Fact] void should_hold_a_registration_for_every_observer() => (_context.Registrations >= _context.ObserverGrains).ShouldBeTrue();
    [Fact] void should_keep_every_supervisor_ticking() => _context.TimeSinceLastTick.All(elapsed => elapsed < TimeSpan.FromSeconds(10)).ShouldBeTrue();
}
