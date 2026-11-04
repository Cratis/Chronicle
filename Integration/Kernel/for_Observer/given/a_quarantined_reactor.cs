// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;

using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

public class a_quarantined_reactor(QuarantineRecoveryFixture fixture) : Specification<QuarantineRecoveryFixture>(fixture)
{
    protected IEventStore _store;
    protected IReactorHandler _handler;
    protected FailedPartition _failedPartition;
    protected ReactorState _quarantinedState;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(QuarantineRecoveryEvent)];
    public override IEnumerable<Type> Reactors => [typeof(QuarantineRecoveryReactor)];

    protected QuarantineRecoveryReactor Reactor => ChronicleFixture.Reactor;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(ChronicleFixture.Reactor);

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Observers:QuarantineOnFailedPartitionCount"] = "1" })
                .Build();
            services.PostConfigure<Configuration.ChronicleOptions>(options => configuration.Bind(options));
        });
    };

    async Task Establish()
    {
        Reactor.Reset();
        _store = await ChronicleClient.GetEventStore($"quarantine-{Guid.NewGuid():N}");
        await _store.DiscoverAll();
        await _store.RegisterAll();
        (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _handler = _store.Reactors.GetHandlerFor<QuarantineRecoveryReactor>();
        await _handler.WaitTillActive(TimeSpan.FromSeconds(30));
        Reactor.Mode = QuarantineRecoveryReactor.RecoveryMode.Fail;
        await _store.EventLog.Append("failing", new QuarantineRecoveryEvent("fail"));
        await Reactor.Failed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await _handler.WaitForState(ObserverRunningState.Quarantined, TimeSpan.FromSeconds(30));
        _failedPartition = (await _handler.WaitForThereToBeFailedPartitions(TimeSpan.FromSeconds(30))).Single();
        _quarantinedState = await _handler.GetState();
    }

    protected Task<ReactorPartitionRetryOutcome> RetryFailedPartition() =>
        _store.Reactors.RetryFailedPartitionFor<QuarantineRecoveryReactor>(_failedPartition.Partition);

    protected Task ClearObserverQuarantine() =>
        ((Contracts.IChronicleServicesAccessor)_store.Connection).Services.Observers.ClearObserverQuarantine(new()
        {
            EventStore = _store.Name,
            Namespace = _store.Namespace,
            ObserverId = _handler.Id,
            EventSequenceId = _handler.EventSequenceId
        });

    protected async Task<IEnumerable<Job>> GetRetryJobs() =>
        (await _store.Jobs.GetJobs()).Where(job => job.Type.Value.Contains("RetryFailedPartition", StringComparison.Ordinal)).ToArray();
}
