// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// A registered reactor of a kernel that releases a subscribed observer after two quiet seconds and wakes dormant ones every five.
/// </summary>
/// <param name="fixture">The fixture that hosts the in-process kernel.</param>
public class a_dormant_reactor(DormantObserverFixture fixture) : Specification<DormantObserverFixture>(fixture)
{
    protected const string ObserverGrainPrefix = "observer/";

    protected IEventStore _store;
    protected IReactorHandler _handler;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(DormantObserverEvent)];
    public override IEnumerable<Type> Reactors => [typeof(DormantObserverReactor)];

    protected DormantObserverReactor Reactor => ChronicleFixture.Reactor;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(ChronicleFixture.Reactor);

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Observers:WatchdogInterval"] = "1",
                    ["Observers:DormantAfter"] = "2",
                    ["Observers:DormantSafetyNet"] = "5",
                    ["Observers:DurableSubscriptions"] = "Release"
                })
                .Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
        });
    };

    async Task Establish()
    {
        Reactor.Reset();
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

    async Task<IEnumerable<string>> ResidentObserverGrains() =>
        (await Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics())
            .Select(statistics => statistics.GrainId.ToString())
            .Where(grain =>
                grain.Contains(_handler.Id.Value, StringComparison.OrdinalIgnoreCase) &&
                grain.Contains(_store.Name.Value, StringComparison.Ordinal) &&
                grain.StartsWith(ObserverGrainPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    protected async Task WaitForHandled(int count)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (Reactor.Handled.Count < count)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Only {Reactor.Handled.Count} of {count} events were handled");
            }

            await Task.Delay(100);
        }
    }
}
