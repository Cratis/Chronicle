// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Collections;
using Aksio.MongoDB;
using Cratis;
using Cratis.Events;
using Cratis.Kernel.Configuration;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.EventTypes;
using Cratis.Schemas;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Benchmarks;

public abstract class BenchmarkJob
{
    protected IGrainFactory GrainFactory { get; private set; } = null!;
    protected IEventSerializer? EventSerializer { get; private set; }
    protected IStorage? Storage { get; private set; }
    protected IEventTypesStorage? EventTypesStorage { get; private set; }
    protected IJsonSchemaGenerator? SchemaGenerator { get; private set; }
    protected virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

    protected IMongoClient? MongoClient { get; private set; }
    protected IMongoDatabase? Database { get; private set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        GrainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        EventSerializer = GlobalVariables.ServiceProvider.GetRequiredService<IEventSerializer>();
        Storage = GlobalVariables.ServiceProvider.GetRequiredService<IStorage>();

        EventTypesStorage = Storage.GetEventStore((string)GlobalVariables.MicroserviceId).EventTypes;
        SchemaGenerator = GlobalVariables.ServiceProvider.GetRequiredService<IJsonSchemaGenerator>();

        var configuration = GlobalVariables.ServiceProvider.GetRequiredService<Storage>();
        var clientFactory = GlobalVariables.ServiceProvider.GetRequiredService<IMongoDBClientFactory>();

        var storageTypes = configuration.Microservices
                                .Get(GlobalVariables.MicroserviceId).Tenants
                                .Get(GlobalVariables.TenantId);
        var eventStoreForTenant = storageTypes.Get(WellKnownStorageTypes.EventStore);

        var url = new MongoUrl(eventStoreForTenant.ConnectionDetails.ToString());
        MongoClient = clientFactory.Create(url);
        Database = MongoClient.GetDatabase(url.DatabaseName);

        foreach (var eventType in EventTypes)
        {
            var eventTypeAttribute = eventType.GetCustomAttribute<EventTypeAttribute>()!;
            EventTypesStorage.Register(eventTypeAttribute.Type, eventType.Name, SchemaGenerator.Generate(eventType));
        }

        Setup();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (MongoClient is null) return;
        var databases = MongoClient.ListDatabases().ToList().Select(db => db["name"].AsString).ToArray();
        databases = databases.Where(_ => _ != "admin" && _ != "config" && _ != "local").ToArray();
        databases.ForEach(db => MongoClient.DropDatabase(db));
    }

    protected virtual void Setup()
    {
    }

    protected JsonObject SerializeEvent(object @event) => EventSerializer!.Serialize(@event).GetAwaiter().GetResult();
}
