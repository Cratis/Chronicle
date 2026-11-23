// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;

using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// A registered reactor, up to date, that an operator gives commands to through the kernel's services.
/// </summary>
/// <param name="fixture">The <see cref="OperatorCommandFixture"/>.</param>
public class a_reactor_given_operator_commands(OperatorCommandFixture fixture) : Specification<OperatorCommandFixture>(fixture)
{
    protected IEventStore _store;
    protected IReactorHandler _handler;

    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(QuarantineRecoveryEvent)];
    public override IEnumerable<Type> Reactors => [typeof(QuarantineRecoveryReactor)];

    public QuarantineRecoveryReactor Reactor => ChronicleFixture.Reactor;

    protected Contracts.IServices KernelServices => ((Contracts.IChronicleServicesAccessor)_store.Connection).Services;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(ChronicleFixture.Reactor);

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);

    async Task Establish()
    {
        Reactor.Reset();
        _store = await ChronicleClient.GetEventStore($"operator-{Guid.NewGuid():N}");
        await _store.DiscoverAll();
        await _store.RegisterAll();
        (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        _handler = _store.Reactors.GetHandlerFor<QuarantineRecoveryReactor>();
        await _handler.WaitTillActive(TimeSpan.FromSeconds(30));
    }

    protected async Task<IEnumerable<Job>> GetOperatorCommandJobs() =>
        (await _store.Jobs.GetJobs()).Where(job => job.Type.Value.Contains("ObserverOperatorCommand", StringComparison.Ordinal)).ToArray();

    protected async Task<IEnumerable<Job>> WaitForOperatorCommandJobs(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            var jobs = await GetOperatorCommandJobs();
            if (jobs.Any() || DateTimeOffset.UtcNow > deadline)
            {
                return jobs;
            }

            await Task.Delay(250);
        }
    }

    protected async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met in time.");
            }

            await Task.Delay(250);
        }
    }
}
