// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Orleans.Storage;

using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventSequenceKey = Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class a_log_with_a_failing_snapshot(PatternCaptureFixture fixture) : Specification<PatternCaptureFixture>(fixture)
{
    protected IEventStore _store;
    protected FailingEventSequenceStateStorage _snapshotStorage;
    protected KernelEventSequenceKey _key;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Observers:WatchdogInterval"] = "5" })
                .Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.EventSequences, (provider, _) =>
                new FailingEventSequenceStateStorage(ActivatorUtilities.CreateInstance<EventSequencesStorageProvider>(provider)));
        });
    };

    async Task Establish()
    {
        _store = await ChronicleClient.GetEventStore($"snapshot-{Guid.NewGuid():N}", "new-tenant");
        await _store.DiscoverAll();
        await _store.RegisterAll();
        (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _key = new(KernelEventSequenceId.Log, _store.Name.Value, _store.Namespace.Value);
        _snapshotStorage = (FailingEventSequenceStateStorage)Services.GetRequiredKeyedService<IGrainStorage>(WellKnownGrainStorageProviders.EventSequences);
        _snapshotStorage.Target = _key;
    }
}
