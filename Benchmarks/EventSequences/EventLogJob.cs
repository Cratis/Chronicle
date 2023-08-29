// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Execution;
using Aksio.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Benchmarks.EventSequences;

public abstract class EventLogJob : BenchmarkJob
{
    protected IEventSequence? EventSequence { get; private set; }

    [IterationSetup]
    public void CleanEventStore()
    {
        ExecutionContextManager?.Establish(TenantId.Development, CorrelationId.New(), GlobalVariables.MicroserviceId);

        var configuration = GlobalVariables.ServiceProvider.GetRequiredService<Storage>();
        var clientFactory = GlobalVariables.ServiceProvider.GetRequiredService<IMongoDBClientFactory>();

        var storageTypes = configuration.Microservices
                                .Get(GlobalVariables.MicroserviceId).Tenants
                                .Get(TenantId.Development);
        var eventStoreForTenant = storageTypes.Get(WellKnownStorageTypes.EventStore);

        var url = new MongoUrl(eventStoreForTenant.ConnectionDetails.ToString());
        var client = clientFactory.Create(url);
        var database = client.GetDatabase(url.DatabaseName);
        database.DropCollection(CollectionNames.EventLog);
        database.DropCollection(CollectionNames.EventSequences);
    }

    protected override void Setup()
    {
        base.Setup();

        var grainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        EventSequence = grainFactory.GetGrain<IEventSequence>(EventSequenceId.Log, keyExtension: new MicroserviceAndTenant(GlobalVariables.MicroserviceId, TenantId.Development));

        SetExecutionContext();
    }

    protected async Task Perform(Func<IEventSequence, Task> action)
    {
        ExecutionContextManager?.Establish(TenantId.Development, CorrelationId.New(), GlobalVariables.MicroserviceId);
        await action(EventSequence!);
    }
}
