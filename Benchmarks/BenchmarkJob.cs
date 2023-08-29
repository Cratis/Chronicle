// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Aksio.Cratis;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Cratis.Schemas;
using Aksio.Execution;
using Aksio.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Benchmarks;

public abstract class BenchmarkJob
{
    protected IGrainFactory GrainFactory { get; private set; } = null!;
    protected IExecutionContextManager? ExecutionContextManager { get; private set; }
    protected IEventSerializer? EventSerializer { get; private set; }
    protected ISchemaStore? SchemaStore { get; private set; }
    protected IJsonSchemaGenerator? SchemaGenerator { get; private set; }
    protected virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();
    protected IMongoDatabase? Database { get; private set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        SetExecutionContext();

        GrainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        ExecutionContextManager = GlobalVariables.ServiceProvider.GetRequiredService<IExecutionContextManager>();
        EventSerializer = GlobalVariables.ServiceProvider.GetRequiredService<IEventSerializer>();
        SchemaStore = GlobalVariables.ServiceProvider.GetRequiredService<ISchemaStore>();
        SchemaGenerator = GlobalVariables.ServiceProvider.GetRequiredService<IJsonSchemaGenerator>();

        var configuration = GlobalVariables.ServiceProvider.GetRequiredService<Storage>();
        var clientFactory = GlobalVariables.ServiceProvider.GetRequiredService<IMongoDBClientFactory>();

        var storageTypes = configuration.Microservices
                                .Get(GlobalVariables.MicroserviceId).Tenants
                                .Get(GlobalVariables.TenantId);
        var eventStoreForTenant = storageTypes.Get(WellKnownStorageTypes.EventStore);

        var url = new MongoUrl(eventStoreForTenant.ConnectionDetails.ToString());
        var client = clientFactory.Create(url);
        Database = client.GetDatabase(url.DatabaseName);

        foreach (var eventType in EventTypes)
        {
            var eventTypeAttribute = eventType.GetCustomAttribute<EventTypeAttribute>()!;
            SchemaStore.Register(eventTypeAttribute.Type, eventType.Name, SchemaGenerator.Generate(eventType));
        }

        Setup();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
    }

    protected virtual void Setup()
    {
    }

    protected void SetExecutionContext() => ExecutionContextManager?.Establish(GlobalVariables.TenantId, CorrelationId.New(), GlobalVariables.MicroserviceId);

    protected JsonObject SerializeEvent(object @event) => EventSerializer!.Serialize(@event).GetAwaiter().GetResult();
}
