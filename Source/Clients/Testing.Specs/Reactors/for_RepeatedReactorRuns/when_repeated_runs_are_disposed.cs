// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_RepeatedReactorRuns;

public class when_repeated_runs_are_disposed : Specification
{
    readonly Ledger _ledger = new();
    Defaults _defaults;

    void Establish() => _defaults = new(new InMemoryClientArtifactsProvider
    {
        EventTypes = [typeof(Observed)],
        Reactors = [typeof(ScopedReactor)]
    });

    async Task Because()
    {
        await RunBatch(_defaults, _ledger);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact] void should_deliver_every_run_including_the_failing_runs() => _ledger.Deliveries.ShouldEqual(32);
    [Fact] void should_surface_each_handler_failure() => _ledger.Failures.ShouldEqual(16);
    [Fact] void should_dispose_every_activated_scope() => _ledger.Disposals.ShouldEqual(32);
    [Fact] void should_not_retain_reactor_instances() => _ledger.Reactors.Exists(reference => reference.IsAlive).ShouldBeFalse();
    [Fact] void should_not_retain_scoped_dependencies() => _ledger.Dependencies.Exists(reference => reference.IsAlive).ShouldBeFalse();
    [Fact] void should_not_retain_completed_scenarios() => _ledger.Scenarios.Exists(reference => reference.IsAlive).ShouldBeFalse();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task RunBatch(Defaults defaults, Ledger ledger)
    {
        for (var index = 0; index < 32; index++)
        {
            using var scenario = new ReactorScenario<ScopedReactor>(defaults);
            scenario.Services.AddSingleton(ledger);
            scenario.Services.AddScoped<ScopedDependency>();

            // Avoid ActivatorUtilities' extra service-checker lookup: DI queues compilation
            // that temporarily roots disposed providers, independently of scenario retention.
            scenario.Services.AddScoped<ScopedReactor>();
            ledger.Scenarios.Add(new(scenario));
            try
            {
                await scenario.Given.ForEventSource("same-source").Events(new Observed(index % 2 != 0));
            }
            catch (ExpectedFailure)
            {
                ledger.Failures++;
            }
        }
    }

    void Destroy() => (_defaults?.EventStore as IDisposable)?.Dispose();

    [EventType("catalog-disposed-reactor-observed")]
    public record Observed(bool Fail);

    public class Ledger
    {
        public int Deliveries { get; set; }
        public int Failures { get; set; }
        public int Disposals { get; set; }
        public List<WeakReference> Reactors { get; } = [];
        public List<WeakReference> Dependencies { get; } = [];
        public List<WeakReference> Scenarios { get; } = [];
    }

    public sealed class ScopedDependency : IDisposable
    {
        readonly Ledger _ledger;

        public ScopedDependency(Ledger ledger)
        {
            _ledger = ledger;
            ledger.Dependencies.Add(new(this));
        }

        public void Dispose() => _ledger.Disposals++;
    }

    public class ScopedReactor : IReactor
    {
        readonly Ledger _ledger;

        public ScopedReactor(ScopedDependency dependency, Ledger ledger)
        {
            _ledger = ledger;
            ledger.Reactors.Add(new(this));
            GC.KeepAlive(dependency);
        }

        public void Handle(Observed @event)
        {
            _ledger.Deliveries++;
            if (@event.Fail) throw new ExpectedFailure();
        }
    }

    public class ExpectedFailure : Exception;
}
