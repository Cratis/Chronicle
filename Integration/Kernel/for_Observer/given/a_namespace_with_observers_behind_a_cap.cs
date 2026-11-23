// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Options;

using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// Four reactors owned by a client application of their own, on a kernel that allows two catch-up jobs at once.
/// </summary>
/// <param name="fixture">The <see cref="CatchUpCapFixture"/>.</param>
public class a_namespace_with_observers_behind_a_cap(CatchUpCapFixture fixture) : Specification<CatchUpCapFixture>(fixture)
{
    /// <summary>
    /// The number of catch-up jobs the kernel allows at once.
    /// </summary>
    public const int Cap = 2;

    static readonly Type[] _reactorTypes = [typeof(CatchUpCapReactorOne), typeof(CatchUpCapReactorTwo), typeof(CatchUpCapReactorThree), typeof(CatchUpCapReactorFour)];

#pragma warning disable CA2213 // Destroy disposes this through the specification lifecycle.
    ServiceProvider? _ownerServices;
#pragma warning restore CA2213
    protected IEventStore _store;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(QuarantineRecoveryEvent)];
    public override IEnumerable<Type> Reactors => [];

    public CatchUpGate Gate => ChronicleFixture.Gate;

    protected string StoreName { get; } = $"cap-{Guid.NewGuid():N}";

    protected Contracts.IServices KernelServices => ((Contracts.IChronicleServicesAccessor)_store.Connection).Services;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(ChronicleFixture.Gate);

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Observers:MaxConcurrentCatchUpJobs"] = Cap.ToString() })
                .Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
        });
    };

    async Task Establish()
    {
        Gate.Reset();
        _store = await ChronicleClient.GetEventStore(StoreName);
    }

    /// <summary>
    /// Starts the client application owning the four reactors; any client started before it is gone.
    /// </summary>
    /// <param name="waitUntilActive">Whether to wait until every observer is active before returning.</param>
    /// <returns>A task that completes when the reactors are registered.</returns>
    protected async Task StartOwner(bool waitUntilActive)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Gate);
        services.AddSingleton(Services.GetRequiredService<IGrainFactory>());
        services.AddSingleton(Services.GetRequiredService<ILocalSiloDetails>());
        services.AddSingleton(Services.GetRequiredService<IConnectionOwnership>());
        services.AddSingleton(Services.GetRequiredService<IServices>());
        services.AddSingleton(Services.GetRequiredService<ILoggerFactory>());
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions
        {
            DefaultSinkTypeId = ChronicleClient.Options.DefaultSinkTypeId
        }));
        services.AddInProcessChronicleClient(new OwnerArtifacts(), StoreName);
        _ownerServices = services.BuildServiceProvider();
        var ownerStore = await _ownerServices.GetRequiredService<IChronicleClient>().GetEventStore(StoreName, _store.Namespace);
        await ownerStore.WaitForRegistration();

        if (waitUntilActive)
        {
            await WaitUntil(
                async () => (await GetObservers()) is { Count: 4 } observers && observers.All(observer => observer.RunningState == Contracts.Observation.ObserverRunningState.Active),
                TimeSpan.FromSeconds(60));
        }
    }

    protected void StopOwner()
    {
        _ownerServices?.Dispose();
        _ownerServices = null;
    }

    protected async Task<IReadOnlyList<Contracts.Observation.ObserverInformation>> GetObservers() =>
        (await KernelServices.Observers.GetObservers(new() { EventStore = StoreName, Namespace = _store.Namespace }))
            .Where(observer => _reactorTypes.Any(type => observer.Id.Contains(type.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

    protected async Task<int> CountLiveCatchUpJobs() =>
        (await _store.Jobs.GetJobs()).Count(job =>
            job.Type.Value.Contains("CatchUpObserver", StringComparison.Ordinal) &&
            !job.Type.Value.Contains("Partition", StringComparison.Ordinal) &&
            job.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps or JobStatus.Running);

    protected async Task WaitUntil(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!await condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"The condition was not met in time: {string.Join(", ", (await GetObservers()).Select(observer => $"{observer.Id}={observer.RunningState}"))}");
            }

            // Task.Delay: poll interval while waiting for the observers to reach the condition, bounded by the deadline.
            await Task.Delay(250);
        }
    }

    void Destroy() => _ownerServices?.Dispose();

    sealed class OwnerArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
    {
        public override IEnumerable<Type> EventTypes => [typeof(QuarantineRecoveryEvent)];
        public override IEnumerable<Type> Reactors => _reactorTypes;
    }

    sealed class NoAssemblies : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [];
        public IEnumerable<Type> DefinedTypes => [];

        public void Initialize()
        {
        }
    }
}
