// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Aksio.Cratis;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Cratis.Schemas;
using Aksio.Execution;
using Aksio.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Benchmarks.EventSequences;

public abstract class EventLogJob
{
    protected abstract IEnumerable<Type> EventTypes { get; }

    protected IEventSequence? EventSequence { get; private set; }
    protected IExecutionContextManager? ExecutionContextManager { get; private set; }
    protected IEventSerializer? EventSerializer { get; private set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var grainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        EventSequence = grainFactory.GetGrain<IEventSequence>(EventSequenceId.Log, keyExtension: new MicroserviceAndTenant(GlobalVariables.MicroserviceId, TenantId.Development));
        ExecutionContextManager = GlobalVariables.ServiceProvider.GetRequiredService<IExecutionContextManager>();
        EventSerializer = GlobalVariables.ServiceProvider.GetRequiredService<IEventSerializer>();

        ExecutionContextManager?.Establish(TenantId.Development, CorrelationId.New(), GlobalVariables.MicroserviceId);

        var schemaStore = GlobalVariables.ServiceProvider.GetRequiredService<ISchemaStore>();
        var schemaGenerator = GlobalVariables.ServiceProvider.GetRequiredService<IJsonSchemaGenerator>();

        foreach (var eventType in EventTypes)
        {
            var eventTypeAttribute = eventType.GetCustomAttribute<EventTypeAttribute>()!;
            schemaStore.Register(eventTypeAttribute.Type, eventType.Name, schemaGenerator.Generate(eventType));
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
    }

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

    protected async Task Perform(Func<IEventSequence, Task> action)
    {
        ExecutionContextManager?.Establish(TenantId.Development, CorrelationId.New(), GlobalVariables.MicroserviceId);
        await action(EventSequence!);
    }

    protected Task<JsonObject> SerializeEvent(object @event) => EventSerializer!.Serialize(@event);
}
