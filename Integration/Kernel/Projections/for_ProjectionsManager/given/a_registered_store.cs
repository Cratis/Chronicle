// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Orleans.Storage;

using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using KernelNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using KernelObserverKey = Cratis.Chronicle.Concepts.Observation.ObserverKey;
using SignalingObserverStorage = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.PatternCaptureFixture.SignalingObserverStorage;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.given;

public class a_registered_store(NamespaceSubscriptionFixture fixture) : Specification<NamespaceSubscriptionFixture>(fixture)
{
    protected IGrainFactory _grains;
    protected KernelEventStoreName _storeName;
    protected readonly KernelNamespaceName _newTenant = "later-tenant";
    protected KernelObserverKey _observerKey;
    protected SignalingObserverStorage _observerStorage;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Observers:WatchdogInterval"] = "1" })
                .Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.ObserverState, (provider, _) =>
                ActivatorUtilities.CreateInstance<SignalingObserverStorage>(provider));
        });
    };

    async Task Establish()
    {
        var initialStore = await ChronicleClient.GetEventStore($"subscriptions-{Guid.NewGuid():N}", "initial-tenant");
        await initialStore.DiscoverAll();
        await initialStore.RegisterAll();
        (await initialStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _storeName = initialStore.Name.Value;
        _grains = Services.GetRequiredService<IGrainFactory>();
        _observerStorage = (SignalingObserverStorage)Services.GetRequiredKeyedService<IGrainStorage>(WellKnownGrainStorageProviders.ObserverState);
        _observerKey = new("$system.statistics.event-types", _storeName, _newTenant, KernelEventSequenceId.Log);

        var definitions = await _grains.GetGrain<Cratis.Chronicle.Projections.IProjectionsManager>(_storeName).GetProjectionDefinitions();
        definitions.Any(definition => definition.Identifier.Value == _observerKey.ObserverId.Value && definition.IsActive).ShouldBeTrue();
        var namespaces = await _grains.GetGrain<Namespaces.INamespaces>(_storeName).GetAll();
        namespaces.ShouldNotContain(_newTenant);
    }
}
