// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Options;

using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// A reactor owned by a client application of its own that has delivered an event and is then gone, leaving the observer without a client.
/// </summary>
/// <param name="fixture">The <see cref="OperatorCommandFixture"/>.</param>
public class a_reactor_whose_client_is_gone(OperatorCommandFixture fixture) : Specification<OperatorCommandFixture>(fixture)
{
#pragma warning disable CA2213 // Destroy disposes this through the specification lifecycle.
    ServiceProvider? _ownerServices;
#pragma warning restore CA2213
    protected IEventStore _store;
    protected string _observerId;
    protected string _eventSequenceId;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(QuarantineRecoveryEvent)];
    public override IEnumerable<Type> Reactors => [];

    public QuarantineRecoveryReactor Reactor => ChronicleFixture.Reactor;

    protected Contracts.IServices KernelServices => ((Contracts.IChronicleServicesAccessor)_store.Connection).Services;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(ChronicleFixture.Reactor);

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);

    async Task Establish()
    {
        Reactor.Reset();
        var storeName = $"gone-{Guid.NewGuid():N}";
        _store = await ChronicleClient.GetEventStore(storeName);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Reactor);
        services.AddSingleton(Services.GetRequiredService<IGrainFactory>());
        services.AddSingleton(Services.GetRequiredService<ILocalSiloDetails>());
        services.AddSingleton(Services.GetRequiredService<IConnectionOwnership>());
        services.AddSingleton(Services.GetRequiredService<IServices>());
        services.AddSingleton(Services.GetRequiredService<ILoggerFactory>());
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions
        {
            DefaultSinkTypeId = ChronicleClient.Options.DefaultSinkTypeId
        }));
        services.AddInProcessChronicleClient(new OwnerArtifacts(), storeName);
        _ownerServices = services.BuildServiceProvider();
        var ownerStore = await _ownerServices.GetRequiredService<IChronicleClient>().GetEventStore(storeName, _store.Namespace);
        await ownerStore.WaitForRegistration();
        var handler = ownerStore.Reactors.GetHandlerFor<QuarantineRecoveryReactor>();
        await handler.WaitTillSubscribed();
        await handler.WaitTillActive();
        _observerId = handler.Id.Value;
        _eventSequenceId = handler.EventSequenceId.Value;
    }

    protected void DisposeOwner() => _ownerServices!.Dispose();

    protected async Task WaitForTheObserverToBeDisconnected(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            var information = await KernelServices.Observers.GetObserverInformation(new()
            {
                EventStore = _store.Name,
                Namespace = _store.Namespace,
                ObserverId = _observerId,
                EventSequenceId = _eventSequenceId
            });
            if (information.RunningState == Contracts.Observation.ObserverRunningState.Disconnected)
            {
                return;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"The observer did not become disconnected; it is {information.RunningState}.");
            }

            await Task.Delay(250);
        }
    }

    protected async Task<IEnumerable<Job>> GetCatchUpJobs() =>
        (await _store.Jobs.GetJobs()).Where(job => job.Type.Value.Contains("CatchUpObserver", StringComparison.Ordinal)).ToArray();

    void Destroy() => _ownerServices?.Dispose();

    sealed class OwnerArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
    {
        public override IEnumerable<Type> EventTypes => [typeof(QuarantineRecoveryEvent)];
        public override IEnumerable<Type> Reactors => [typeof(QuarantineRecoveryReactor)];
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
