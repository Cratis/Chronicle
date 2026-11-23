// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// A registered reactor of a kernel that releases a subscribed observer after two quiet seconds and wakes dormant ones every five.
/// </summary>
/// <param name="fixture">The fixture that hosts the in-process kernel.</param>
/// <typeparam name="TFixture">The type of the fixture.</typeparam>
/// <remarks>
/// The in-process kernel is created once for every type a specification is a <see cref="Specification{T}"/> of, with the settings
/// of the first context that needed it. A variant of the kernel with other settings therefore has to be its own type, which is
/// why this is generic over the fixture instead of deriving from <see cref="Specification{T}"/> of the base fixture.
/// </remarks>
public class a_dormant_reactor<TFixture>(TFixture fixture) : Specification<TFixture>(fixture)
    where TFixture : DormantObserverFixture
{
    protected const string ObserverGrainPrefix = "observer/";

    protected IEventStore _store;
    protected IReactorHandler _handler;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(DormantObserverEvent), typeof(DormantAlphaEvent), typeof(DormantBetaEvent), typeof(DormantGammaEvent)];
    public override IEnumerable<Type> Reactors => [typeof(DormantObserverReactor), typeof(DormantAlphaReactor), typeof(DormantBetaReactor), typeof(DormantGammaReactor)];

    protected DormantObserverReactor Reactor => ChronicleFixture.Reactor;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(ChronicleFixture.Reactor);
        services.AddSingleton(ChronicleFixture.Alpha);
        services.AddSingleton(ChronicleFixture.Beta);
        services.AddSingleton(ChronicleFixture.Gamma);
    }

    /// <summary>
    /// Gives a derived context the chance to change the observer settings the kernel starts with. The kernel is started once
    /// per fixture, so a different setting needs a fixture of its own.
    /// </summary>
    /// <param name="settings">The settings, keyed by their configuration path.</param>
    protected virtual void ConfigureObserverSettings(Dictionary<string, string?> settings)
    {
    }

    /// <summary>
    /// Gives a derived context the chance to add services to the kernel silo.
    /// </summary>
    /// <param name="services">The silo's services.</param>
    protected virtual void ConfigureSiloServices(IServiceCollection services)
    {
    }

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var settings = new Dictionary<string, string?>
                {
                    ["Observers:WatchdogInterval"] = "1",
                    ["Observers:DormantAfter"] = "2",
                    ["Observers:DormantSafetyNet"] = "600",
                    ["Observers:DurableSubscriptions"] = "Release",

                    // The keeper of every client stream looks at its subscriptions every few seconds, so a keeper that woke
                    // a dormant observer would show within the time the observers are waited on.
                    ["ConnectedClients:SubscriptionCheckIntervalSeconds"] = "4",
                    ["ConnectedClients:SubscriptionSettleSeconds"] = "2"
                };
            ConfigureObserverSettings(settings);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
            ConfigureSiloServices(services);
        });
    };

    async Task Establish()
    {
        Reactor.Reset();
        ChronicleFixture.Alpha.Handled.Clear();
        ChronicleFixture.Beta.Handled.Clear();
        ChronicleFixture.Gamma.Handled.Clear();
        _store = await ChronicleClient.GetEventStore($"dormant-{Guid.NewGuid():N}");
        await _store.DiscoverAll();
        await _store.RegisterAll();
        (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _handler = _store.Reactors.GetHandlerFor<DormantObserverReactor>();
        await _handler.WaitTillActive(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Gets whether the reactor's observer holds an activation in the kernel.
    /// </summary>
    /// <returns>True when the observer is resident; otherwise false.</returns>
    protected async Task<bool> ObserverIsResident() => (await ResidentObserverGrains()).Any();

    /// <summary>
    /// Waits until the reactor's observer holds no activation, which is the dormant observer leaving.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the observer is not released in time.</exception>
    protected async Task WaitUntilReleased()
    {
        // A new observer registers its alert reminder, which is due one to two minutes later (Orleans reminders cannot be
        // shorter than a minute); the observer is not dormant before that tick has settled it.
        var deadline = DateTimeOffset.UtcNow.AddSeconds(240);
        while (await ObserverIsResident())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                var summary = Services.GetRequiredService<Releases.IClusterReleases>().Summarize();
                throw new TimeoutException($"The observer was not released: {string.Join(", ", await ResidentObserverGrains())}; floor reached {Releases.DormantObserverFloor.IsReached(Services.GetRequiredService<Releases.IClusterReleases>())}; releases {summary?.Releases.Count} unpublished {summary?.Unpublished} omitted {summary?.Omitted}");
            }

            await Task.Delay(500);
        }
    }

    Task<IEnumerable<string>> ResidentObserverGrains() => ResidentObserverGrains(_handler);

    /// <summary>
    /// Gets the activations of the observer of a reactor.
    /// </summary>
    /// <param name="handler">The <see cref="IReactorHandler"/> of the reactor.</param>
    /// <returns>The grain identities holding an activation.</returns>
    protected async Task<IEnumerable<string>> ResidentObserverGrains(IReactorHandler handler) =>
        (await Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics())
            .Select(statistics => statistics.GrainId.ToString())
            .Where(grain =>
                grain.Contains(handler.Id.Value, StringComparison.OrdinalIgnoreCase) &&
                grain.Contains(_store.Name.Value, StringComparison.Ordinal) &&
                grain.StartsWith(ObserverGrainPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    /// <summary>
    /// Waits until the observer of a reactor holds no activation.
    /// </summary>
    /// <param name="handler">The <see cref="IReactorHandler"/> of the reactor.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the observer is not released in time.</exception>
    protected async Task WaitUntilReleased(IReactorHandler handler)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(240);
        while ((await ResidentObserverGrains(handler)).Any())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                var observerState = await Services.GetRequiredService<Storage.IStorage>().GetEventStore(_store.Name.Value).GetNamespace(Concepts.EventStoreNamespaceName.Default).Observers.Get(handler.Id.Value);
                var recorded = (await Services.GetRequiredService<Storage.IStorage>().GetEventStore(_store.Name.Value).GetNamespace(Concepts.EventStoreNamespaceName.Default).Observers.GetDurableSubscriptions(Concepts.EventSequences.EventSequenceId.Log)).ContainsKey(handler.Id.Value);
                var dormantInQueue = await Services.GetRequiredService<IGrainFactory>().GetGrain<Cratis.Chronicle.EventSequences.IAppendedEventsQueues>(new Concepts.EventSequences.EventSequenceKey(Concepts.EventSequences.EventSequenceId.Log, _store.Name.Value, Concepts.EventStoreNamespaceName.Default)).IsDormant(new Concepts.Observation.ObserverKey(handler.Id.Value, _store.Name.Value, Concepts.EventStoreNamespaceName.Default, Concepts.EventSequences.EventSequenceId.Log));
                throw new TimeoutException($"The observer of {handler.Id} was not released: resident {string.Join(", ", await ResidentObserverGrains(handler))}; running state {observerState.RunningState}, replaying {observerState.IsReplaying}, catching up {observerState.CatchingUpPartitions.Count}, in flight {observerState.InFlightPartitions.Count}, next {observerState.NextEventSequenceNumber}; durable record {recorded}; dormant in queue {dormantInQueue}");
            }

            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Waits until none of a set of reactors has an observer activation at the same time.
    /// </summary>
    /// <param name="handlers">The <see cref="IReactorHandler"/> instances of the reactors.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when there is never a moment without activations.</exception>
    protected async Task WaitUntilNoneIsResident(IReactorHandler[] handlers)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(240);

        // An observer that has just been released can be woken once more by the reminder of its alerts, which ticks once a minute
        // or two after it subscribed and then removes itself. Quiet for several samples in a row is dormant for good.
        var quiet = 0;
        while (quiet < 8)
        {
            quiet = (await Task.WhenAll(handlers.Select(async handler => (await ResidentObserverGrains(handler)).Count()))).Sum() > 0 ? 0 : quiet + 1;
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("There was never a stretch when no observer had an activation");
            }

            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Waits until a reactor has handled a number of events.
    /// </summary>
    /// <param name="handled">The events the reactor has handled.</param>
    /// <param name="count">The number to wait for.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the events are not handled in time.</exception>
    protected static async Task WaitForHandled(System.Collections.Concurrent.ConcurrentQueue<EventSequenceNumber> handled, int count)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (handled.Count < count)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Only {handled.Count} of {count} events were handled");
            }

            await Task.Delay(100);
        }
    }

    protected async Task WaitForHandled(int count)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (Reactor.Handled.Count < count)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                var observers = Services.GetRequiredService<Storage.IStorage>().GetEventStore(_store.Name.Value).GetNamespace(Concepts.EventStoreNamespaceName.Default).Observers;
                var observerState = await observers.Get(_handler.Id.Value);
                var recorded = (await observers.GetDurableSubscriptions(Concepts.EventSequences.EventSequenceId.Log)).ContainsKey(_handler.Id.Value);
                var dormantInQueue = await Services.GetRequiredService<IGrainFactory>().GetGrain<Cratis.Chronicle.EventSequences.IAppendedEventsQueues>(new Concepts.EventSequences.EventSequenceKey(Concepts.EventSequences.EventSequenceId.Log, _store.Name.Value, Concepts.EventStoreNamespaceName.Default)).IsDormant(new Concepts.Observation.ObserverKey(_handler.Id.Value, _store.Name.Value, Concepts.EventStoreNamespaceName.Default, Concepts.EventSequences.EventSequenceId.Log));
                throw new TimeoutException($"Only {Reactor.Handled.Count} of {count} events were handled ({string.Join(',', Reactor.Handled)}): resident {string.Join(", ", await ResidentObserverGrains())}; running state {observerState.RunningState}, next {observerState.NextEventSequenceNumber}, last handled {observerState.LastHandledEventSequenceNumber}, in flight {observerState.InFlightPartitions.Count}, catching up {observerState.CatchingUpPartitions.Count}; durable record {recorded}; dormant in queue {dormantInQueue}");
            }

            await Task.Delay(100);
        }
    }
}
