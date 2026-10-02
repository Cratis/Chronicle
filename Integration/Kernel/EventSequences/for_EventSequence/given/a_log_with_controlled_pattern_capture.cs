// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Orleans.Storage;

using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventSequenceKey = Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class a_log_with_controlled_pattern_capture(PatternCaptureFixture fixture) : Specification<PatternCaptureFixture>(fixture)
{
    protected IEventStore _store;
    protected PatternCaptureControl _control;
    protected KernelEventSequenceKey _key;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<PatternCaptureControl>();
            services.AddSingleton<IPatternCapture>(provider => new ControlledPatternCapture(
                ActivatorUtilities.CreateInstance<PatternCapture>(provider), provider.GetRequiredService<PatternCaptureControl>()));
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.EventSequences, (provider, _) =>
                new FailingEventSequenceStateStorage(ActivatorUtilities.CreateInstance<EventSequencesStorageProvider>(provider), provider.GetRequiredService<PatternCaptureControl>()));
        });
    };

    async Task Establish()
    {
        _store = await ChronicleClient.GetEventStore($"patterns-{Guid.NewGuid():N}", "new-tenant");
        await _store.DiscoverAll();
        await _store.RegisterAll();
        (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _key = new(KernelEventSequenceId.Log, _store.Name.Value, _store.Namespace.Value);
        _control = Services.GetRequiredService<PatternCaptureControl>();
    }

    void Destroy() => _control?.SubscriptionReleased.TrySetResult();
}
